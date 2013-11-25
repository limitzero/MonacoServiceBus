using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Timers;
using Monaco.Bus.Internals;
using Monaco.Bus.Internals.Collections;
using Monaco.Bus.Services.Timeout.Messages.Commands;
using Monaco.Bus.Services.Timeout.Messages.Events;
using Monaco.Extensibility.Logging;
using Monaco.Extensibility.Storage.Timeouts;

namespace Monaco.Bus.Services.Timeout
{
	/// <summary>
	/// Background agent that will run against the timeouts data store inspecting 
	/// for all expired timeouts and delivering those timeouts to the appropriate
	/// consumers via the service bus.
	/// </summary>
	public class TimeoutsService : ITimeoutsService, IStartable
	{
		private static bool isBusy;
		private static ConcurrentDictionary<Guid, TimeoutExpired> expiredTimeoutsCache;
		private readonly ILogger logger;
		private readonly IControlBus controlBus;
		private readonly ITimeoutsRepository timeoutsRepository;
		private bool disposed;
		private Timer expiredTimeoutCacheTimer;
		private Timer timer;

		public TimeoutsService(
			ILogger logger, 
			IControlBus controlBus, 
			ITimeoutsRepository timeoutsRepository)
		{
			this.logger = logger;
			this.controlBus = controlBus;
			this.timeoutsRepository = timeoutsRepository;

			if (expiredTimeoutsCache == null)
				expiredTimeoutsCache = new ConcurrentDictionary<Guid, TimeoutExpired>();
		}

		public IServiceBus Bus { get; set; }

		public bool IsRunning { get; private set; }

		public void Start()
		{
			timer = new Timer(1000); // every second:
			timer.Elapsed += OnTimerElapsed;
			timer.Start();

			expiredTimeoutCacheTimer = new Timer(3*60*1000); // three minute sweep of cache
			expiredTimeoutCacheTimer.Elapsed += OnCacheSweepTimerElasped;
			expiredTimeoutCacheTimer.Start();

			logger.LogDebugMessage("Timeout service started.");
			IsRunning = true;
		}

		public void Stop()
		{
			Dispose();
			this.logger.LogDebugMessage("Timeout service stopped.");
		}

		public void Dispose()
		{
			IsRunning = false;
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public void RegisterTimeout(ScheduleTimeout timeout)
		{
			if (disposed) return;

			string msg = string.Format("Time out scheduled for message '{0}' with instance identifier of '{1}' " +
			                           "created at '{2}' for delivery at '{3}' on endpoint '{4}'.",
			                           timeout.MessageToDeliver.GetType().FullName,
			                           timeout.Id,
			                           timeout.CreatedOn,
			                           timeout.At,
			                           timeout.Endpoint);

			this.logger.LogInfoMessage(msg);

			timeoutsRepository.Add(timeout);

			DeliverScheduleTimeoutMessageToControlEndpoint(timeout);
		}

		public void RegisterCancel(CancelTimeout timeout)
		{
			if (disposed) return;

			string msg = string.Format("Cancellation message registered for scheduled timeout with " +
			                           "corresponding instance id of '{0}'.",
			                           timeout.TimeoutId);

			this.logger.LogInfoMessage(msg);

			timeoutsRepository.Remove(timeout.TimeoutId);

			DeliverCancellationMessageToControlEndpoint(timeout);
		}

		public void Dispose(bool disposing)
		{
			if (this.disposed == false)
			{
				if (disposing == true)
				{
					if (timer != null)
					{
						timer.Elapsed -= OnTimerElapsed;
						timer.Stop();
						timer.Dispose();
					}

					this.timer = null;

					if (expiredTimeoutCacheTimer != null)
					{
						expiredTimeoutCacheTimer.Elapsed -= OnCacheSweepTimerElasped;
						expiredTimeoutCacheTimer.Stop();
						expiredTimeoutCacheTimer.Dispose();
					}

					this.expiredTimeoutCacheTimer = null;

					if (expiredTimeoutsCache != null)
					{
						expiredTimeoutsCache.Clear();
					}

					expiredTimeoutsCache = null;
				}
			}
			this.disposed = true;
		}

		public void OnTimerElapsed(object sender, ElapsedEventArgs e)
		{
			if (disposed) return;

			// poll the repository for all timeouts that have expired 
			// and deliver the message out to the interested parties:
			ICollection<ScheduleTimeout> timeouts =
				timeoutsRepository.FindAll(this.Bus.Endpoint.EndpointUri.ToString());

			foreach (ScheduleTimeout timeout in timeouts)
			{
				if (timeout.HasExpired() == false) continue;

				var expiredTimeout = new TimeoutExpired
				                     	{
				                     		CorrelationId = timeout.Id,
				                     		RequestedAt = timeout.CreatedOn,
				                     		Endpoint = timeout.Endpoint,
				                     		Message = timeout.MessageToDeliver
				                     	};

				DeliverExpiredTimeout(expiredTimeout, timeout);
			}
		}

		private void OnCacheSweepTimerElasped(object sender, ElapsedEventArgs e)
		{
			if (disposed) return;

			isBusy = true;

			lock (expiredTimeoutsCache)
				expiredTimeoutsCache.Clear();

			this.logger.LogDebugMessage("Internal timeouts cache cleared.");

			isBusy = false;
		}

		private void DeliverExpiredTimeout(TimeoutExpired expiredTimeout, ScheduleTimeout timeout)
		{
			if (disposed) return;

			while (isBusy)
			{
			}

			lock (expiredTimeoutsCache)
			{
				if (expiredTimeoutsCache.ContainsKey(expiredTimeout.CorrelationId) == false)
				{
					expiredTimeoutsCache.TryAdd(expiredTimeout.CorrelationId, expiredTimeout);

					RegisterCancel(timeout.CreateCancelMessage());

					this.Bus.ConsumeMessages(expiredTimeout);
				}
			}
		}

		private void DeliverScheduleTimeoutMessageToControlEndpoint(ScheduleTimeout message)
		{
			var controlMessage = new TimeoutRequested {ScheduledTimeout = message};
			DeliverToControl(controlMessage);
		}

		private void DeliverCancellationMessageToControlEndpoint(CancelTimeout message)
		{
			var controlMessage = new TimeoutCancelled {CancelledTimeout = message};
			DeliverToControl(controlMessage);
		}

		private void DeliverToControl(IMessage message)
		{
			controlBus.Send(message);
		}
	}
}