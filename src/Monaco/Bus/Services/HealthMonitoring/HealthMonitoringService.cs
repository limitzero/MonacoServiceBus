using System;
using System.Collections.Generic;
using System.Linq;
using Monaco.Bus.Services.HealthMonitoring.Messages.Commands;
using Monaco.Bus.Services.HealthMonitoring.Messages.Events;
using Monaco.Bus.Services.Timeout.Messages.Commands;
using Monaco.Extensibility.Logging;
using Monaco.Extensions;

namespace Monaco.Bus.Services.HealthMonitoring
{
	public class HealthMonitoringService :
		Consumes<EndpointStarted>,
		Consumes<EndpointStopped>,
		Consumes<EndpointMessageSent>,
		Consumes<EndpointMessageReceived>,
		Consumes<EndpointHeartBeat>,
		Consumes<RecycleStatisticsMessage>,
		Consumes<PrepareEndpointStatus>,
		Consumes<TakeEndpointOffline>,
		Consumes<BringEndpointBackOnline>,
		Consumes<Ping>
	{
		private static object dataLock = new object();
		private static HealthMonitoringData Data;

		private readonly IServiceBus bus;
		private readonly IControlBus controlBus;

		public HealthMonitoringService(IServiceBus bus, IControlBus controlBus)
		{
			this.bus = bus;
			this.controlBus = controlBus;

			if(Data == null)
				Data = new HealthMonitoringData();
		}

		~HealthMonitoringService()
		{
			Data = null;
		}

		#region Consumes<BringEndpointBackOnline> Members

		public void Consume(BringEndpointBackOnline message)
		{
			//if (((IStartable)Bus.Transport).IsRunning != true)
			//    ((IStartable)Bus.Transport).Start();

			var controlMessage = new EndpointBackOnline
			                     	{
			                     		At = DateTime.Now,
			                     		Endpoint = this.bus.Endpoint.EndpointUri.ToString()
			                     	};

			controlBus.Send(controlMessage);
		}

		#endregion

		#region Consumes<EndpointHeartBeat> Members

		public void Consume(EndpointHeartBeat message)
		{
			this.bus.Find<ILogger>().LogInfoMessage(string.Format("Heartbeat received from '{0}'.", message.EndpointUri));
			var statistic = new HealthMonitoringStatistic(message.EndpointUri);

			message.Received = DateTime.Now;

			statistic.RecordHeartBeat(message);

			lock(dataLock)
				Data.RegisterStatistic(statistic);

			var heartbeatReceived = new EndpointHeartbeatReceived {Heartbeat = message};

			controlBus.Send(heartbeatReceived);
		}

		#endregion

		#region Consumes<EndpointMessageReceived> Members

		public void Consume(EndpointMessageReceived message)
		{
			if (!controlBus.IsAvailable) return;

			this.bus.Find<ILogger>().LogInfoMessage(string.Format("Message '{0}' received from endpoint '{1}'.",
			                                                 message.Message.GetType().FullName,
			                                                 message.EndpointUri));

			var statistic = new HealthMonitoringStatistic(message.EndpointUri);
			statistic.RecordMessageReceived();

			lock (dataLock)
				Data.RegisterStatistic(statistic);
		}

		#endregion

		#region Consumes<EndpointMessageSent> Members

		public void Consume(EndpointMessageSent message)
		{
			if (!controlBus.IsAvailable) return;

			this.bus.Find<ILogger>().LogInfoMessage(string.Format("Message '{0}' delivered to endpoint '{1}'.",
			                                                 message.Message.GetType().Name,
			                                                 message.EndpointUri));

			var statistic = new HealthMonitoringStatistic(message.EndpointUri);
			statistic.RecordMessageSent();

			lock (dataLock)
				Data.RegisterStatistic(statistic);
		}

		#endregion

		#region Consumes<EndpointStarted> Members

		public void Consume(EndpointStarted message)
		{
			//var readyToWork = new EndpointReadyForWork {Endpoint = message.Endpoint};
			//_controlBus.Send(readyToWork);
		}

		#endregion

		#region Consumes<EndpointStopped> Members

		public void Consume(EndpointStopped message)
		{
			controlBus.Send(message);
		}

		#endregion

		#region Consumes<Ping> Members

		public void Consume(Ping message)
		{
			this.bus.Reply<Pong>(m =>
			                	{
			                		m.ResponderEndpoint = this.bus.Endpoint.EndpointUri.ToString();
			                		m.RequestorEndpoint = message.RequestorEndpoint;
			                		m.Delta = DateTime.Now - message.SentAt;
			                		m.ReceivedAt = DateTime.Now;
			                	});
		}

		#endregion

		#region Consumes<PrepareEndpointStatus> Members

		public void Consume(PrepareEndpointStatus message)
		{
			var statistics = new List<HealthMonitoringStatistic>();

			foreach (HealthMonitoringStatistic statistic in Data.Statistics)
			{
				ICollection<HealthMonitoringStatistic> endpointStats =
					GetStatisticsForEndpoint(message.EndpointUri);

				HealthMonitoringStatistic endpointStatistic =
					ComputeStatistics(message.EndpointUri, endpointStats);

				if (!statistics.Exists(x => x.EndpointUri == endpointStatistic.EndpointUri))
				{
					statistics.Add(endpointStatistic);
				}
			}

			if (statistics.Count > 0)
			{
				var result = new EndpointStatusPrepared
				             	{
				             		Statistics = statistics.Distinct().ToList(),
				             		Endpoint = this.bus.Endpoint.EndpointUri.ToString()
				             	};

				controlBus.Send(result);
			}
		}

		#endregion

		#region Consumes<RecycleStatisticsMessage> Members

		public void Consume(RecycleStatisticsMessage message)
		{
			var logger = this.bus.Find<ILogger>();
			logger.LogInfoMessage("Recycling all endpoint statistics...");

			lock (dataLock)
			{
				Data = new HealthMonitoringData();
			}
		}

		#endregion

		#region Consumes<TakeEndpointOffline> Members

		public void Consume(TakeEndpointOffline message)
		{
			TimeSpan? timespan = new TimeSpan().CreateFromInterval(message.Duration);
			var online = new BringEndpointBackOnline {Endpoint = this.bus.Endpoint.EndpointUri.ToString()};

			if (timespan.HasValue == false) return;

			var timeout = new ScheduleTimeout(timespan.Value, online);
			this.bus.Send(timeout);

			//((IStartable)Bus.Transport).Stop();

			var controlMessage = new EndpointTakenOffline
			                     	{
			                     		At = DateTime.Now,
			                     		Duration = message.Duration,
										Endpoint = this.bus.Endpoint.EndpointUri.ToString()
			                     	};

			controlBus.Send(controlMessage);
		}

		#endregion

		private ICollection<HealthMonitoringStatistic> GetStatisticsForEndpoint(string endpointUri)
		{
			lock (dataLock)
			{
				ICollection<HealthMonitoringStatistic> statistics =
					(from theStatistic in Data.Statistics
					 where
					 	theStatistic.EndpointUri.Trim().ToLower() ==
					 	endpointUri.Trim().ToLower()
					 select theStatistic).Distinct().ToList();
				return statistics;
			}
		}

		private HealthMonitoringStatistic ComputeStatistics(string endpointUri,
		                                                    IEnumerable<HealthMonitoringStatistic> statistics)
		{
			var endpointStatistic =
				new HealthMonitoringStatistic(endpointUri);

			foreach (HealthMonitoringStatistic statistic in statistics)
			{
				endpointStatistic.NumberOfMessagesReceived += statistic.NumberOfMessagesReceived;
				endpointStatistic.NumberOfMessagesSent += statistic.NumberOfMessagesSent;
				endpointStatistic.Heartbeats.AddRange(statistic.Heartbeats);
			}

			endpointStatistic.SetHeartBeatStatus();

			return endpointStatistic;
		}
	}
}