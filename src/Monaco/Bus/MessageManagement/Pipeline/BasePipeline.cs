using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Monaco.Bus.Internals;
using Monaco.Bus.Internals.Collections;
using Monaco.Bus.MessageManagement.Dispatcher;
using Monaco.Bus.MessageManagement.Resolving;
using Monaco.Bus.MessageManagement.Serialization;
using Monaco.Bus.Messages;
using Monaco.Bus.Messages.For.Recovery;
using Monaco.Configuration;
using Monaco.Endpoint.Factory;
using Monaco.Extensibility.Logging;
using Monaco.Extensibility.Storage.Subscriptions;
using Monaco.Extensions;
using Monaco.Subscriptions.Impl;

namespace Monaco.Bus.MessageManagement.Pipeline
{
	public delegate void BlockOnReceiveDelegate(IEnvelope envelope);

	public abstract class BasePipeline : IPipeline
	{
		private readonly IContainer container;
		private readonly ILogger logger;
		private readonly IThreadSafeList<IPipelineFilter> _postReceiveFilters;
		private readonly IThreadSafeList<IPipelineFilter> _postSendFilters;
		private readonly IThreadSafeList<IPipelineFilter> _preSendFilters;
		private readonly IThreadSafeList<IPipelineFilter> _preReceiveFilters;
		private static readonly object _executeFiltersLock = new object();
		private Guid pipelineId = Guid.Empty;
		private IServiceBus bus;

		protected BasePipeline(IContainer container)
		{
			this.container = container;
			logger = this.container.Resolve<ILogger>();

			_preSendFilters = new ThreadSafeList<IPipelineFilter>();
			_postSendFilters = new ThreadSafeList<IPipelineFilter>();

			_preReceiveFilters = new ThreadSafeList<IPipelineFilter>();
			_postReceiveFilters = new ThreadSafeList<IPipelineFilter>();

			pipelineId = Guid.NewGuid();
		}

		public IEnumerable<IPipelineFilter> PreSendFilters
		{
			get { return _preSendFilters; }
		}

		public IEnumerable<IPipelineFilter> PostSendFilters
		{
			get { return _postSendFilters; }
		}

		public IEnumerable<IPipelineFilter> PreReceiveFilters
		{
			get { return _preReceiveFilters; }
		}

		public IEnumerable<IPipelineFilter> PostReceiveFilters
		{
			get { return _postReceiveFilters; }
		}

		public void Execute(PipelineDirection direction, IServiceBus bus, IEnvelope envelope)
		{
			this.bus = bus;

			logger.LogDebugMessage(string.Format("{0} - started for {1} with message(s) '{2}'",
												  GetPipelineIdentifier(),
												  direction.ToString().ToLower(), 
												  envelope.Body.Label));

			if (direction == PipelineDirection.Send)
			{
				ExecuteSendPipeline(envelope);
			}
			else
			{
				ExecuteReceivePipeline(envelope);
			}

			this.logger.LogDebugMessage(string.Format("{0} - stopped for {1} with message(s) '{2}'",
												  GetPipelineIdentifier(),
												  direction.ToString().ToLower(), 
												  envelope.Body.Label));
		}

		public void RegisterPreSendFilter(IPipelineFilter filter)
		{
			_preSendFilters.AddUnique(filter);
		}

		public void RegisterPostSendFilter(IPipelineFilter filter)
		{
			_postSendFilters.AddUnique(filter);
		}

		public void RegisterPreReceiveFilter(IPipelineFilter filter)
		{
			_preReceiveFilters.AddUnique(filter);
		}

		public void RegisterPostReceiveFilter(IPipelineFilter filter)
		{
			_postReceiveFilters.AddUnique(filter);
		}

		private void ExecuteReceivePipeline(IEnvelope envelope)
		{
			//// block on receive until all of the tasks are done:
			//Task preReceiveTask = Task.Factory.StartNew(() => ExecutePreReceiveFilters(envelope));
			//Task resolveAndDispatchTask = Task.Factory.StartNew(() => ExecuteResolveAndDispatchForReceive(envelope));
			//Task postReceiveTask = Task.Factory.StartNew(() => ExecutePostReceiveFilters(envelope));

			//// wait for the message to be handled:
			//Task.WaitAll(preReceiveTask, resolveAndDispatchTask, postReceiveTask);

			ExecutePreReceiveFilters(envelope);
			ExecuteResolveAndDispatchForReceive(envelope);
			ExecutePostReceiveFilters(envelope);
		}

		private void ExecuteResolveAndDispatchForReceive(IEnvelope envelope)
		{
			foreach (var message in envelope.Body.Payload)
			{
				// find all consumers for the message:
				var resolver = container.Resolve<IResolveMessageToConsumers>();
				IEnumerable<IConsumer> consumers = resolver.ResolveAll(message);

				if (new List<IConsumer>(consumers).Count == 0)
				{
					if (typeof(IAdminMessage).IsAssignableFrom(message.GetType()) == false)
					{
						// move the message that could not be consumed to the recovery/error endpoint:
						envelope.Footer.RecordException("The message does not have a consumer defined for processing.");
						var recoveryMessage = new RecoveryMessage { Envelope = envelope, OccuredAt = System.DateTime.Now };
						this.bus.ConsumeMessages(recoveryMessage);
					}
				}
				else
				{
					// deliver the message to the consumers:
					var dispatcher = container.Resolve<IMessageDispatcher>();
					var toReceive = envelope.Clone(message);
					dispatcher.Dispatch(this.bus, consumers, toReceive);
				}
			}
		}

		private void ExecuteSendPipeline(IEnvelope envelope)
		{
			// execute pre-send actions for message:
			ExecutePreSendFilters(envelope);

			// send message to endpoint:
			SendEnvelope(envelope);

			// execute post-send actions for message:
			ExecutePostSendFilters(envelope);
		}

		private void SendEnvelope(IEnvelope envelope)
		{
			// find all of the subscriptions for each message and send the envelope
			// to each one (if the subscriptions are different) 
			foreach (var message in envelope.Body.Payload)
			{
				if (message == null) continue;

				ICollection<Subscription> subscriptions = GetAllSubscriptionsForMessage(message);

				if (subscriptions.Count == 0)
				{
					if (typeof(IAdminMessage).IsAssignableFrom(message.GetType()) == true)
					{
						// consume the message on local endpoint (admin messages are not mapped):
						logger.LogDebugMessage(string.Format("{0} - admin message '{1}' directly consumed on service bus endpoint.",
															 GetPipelineIdentifier(),
															 message.GetType().Name));
						ExecuteResolveAndDispatchForReceive(envelope);
					}
					else
					{
						envelope.Footer.RecordException("The message does not have a subscription endpoint defined for delivery.");
						var recovery = new RecoveryMessage { Envelope = envelope, OccuredAt = System.DateTime.Now };
						this.bus.ConsumeMessages(recovery);
					}
				}
				else
				{
					// deliver to the remote endpoint:
					foreach (Subscription subscription in subscriptions)
					{
						var toSend = envelope.Clone(message);
						toSend.Header.RemoteEndpoint = subscription.Uri;
						DispatchMessageViaTransport(new Uri(subscription.Uri), toSend);
					}
				}
			}
		}

		private ICollection<Subscription> GetAllSubscriptionsForMessage(object message)
		{
			var subscriptions = new List<Subscription>();

			// get all of the types representing the message:
			Type type = message.GetImplementationFromProxy();
			var types = new List<Type>(type.GetInterfaces());
			types.Add(type);

			var repository = container.Resolve<ISubscriptionRepository>();

			foreach (Type messageType in types)
			{
				var currentSubscriptions = repository.Find(messageType);
				subscriptions.AddRange(currentSubscriptions);
			}

			return subscriptions;
		}

		/// <summary>
		/// This will find the current endpoint transport and send the message to the 
		/// desired location based on the semantics of the defined transport for the 
		/// current endpoint where the message consumer is defined for accepting 
		/// messages.
		/// </summary>
		/// <param name="endpoint"></param>
		/// <param name="envelope"></param>
		private void DispatchMessageViaTransport(Uri endpoint, IEnvelope envelope)
		{
			Exchange exchange = container.Resolve<IEndpointFactory>().Build(endpoint);

			if (exchange != null)
			{
				exchange.Transport.SerializationProvider = container.Resolve<ISerializationProvider>();
				exchange.Transport.Send(exchange.Endpoint, envelope);
			}

			logger.LogDebugMessage(string.Format("{0} - delivered message '{1}' to endpoint '{2}'",
												  GetPipelineIdentifier(),
												  envelope.Body.Payload.ToItemList(),
												  endpoint.OriginalString));
		}

		private void ExecutePreSendFilters(IEnvelope envelope)
		{
			ExecuteFilters(PreSendFilters, envelope);
		}

		private void ExecutePostSendFilters(IEnvelope envelope)
		{
			ExecuteFilters(PostSendFilters, envelope);
		}

		private void ExecutePreReceiveFilters(IEnvelope envelope)
		{
			ExecuteFilters(PreReceiveFilters, envelope);
		}

		private void ExecutePostReceiveFilters(IEnvelope envelope)
		{
			ExecuteFilters(PostReceiveFilters, envelope);
		}

		private void ExecuteFilters(IEnumerable<IPipelineFilter> filters, IEnvelope envelope)
		{
			lock (_executeFiltersLock)
			{
				foreach (IPipelineFilter filter in filters)
				{
					Exception filterException = null;

					if (TryExecuteFilter(filter, envelope, out filterException) == false)
					{
						// log the problem with the filter and keep moving...
						string message = string.Format("{0} - An error has occurred while attempting " +
													   "to execute filter '{1}'. Reason: {2}",
													   GetPipelineIdentifier(),
													   string.IsNullOrEmpty(filter.Name) ? filter.GetType().Name : filter.Name,
													   filterException.Message);

						logger.LogErrorMessage(message, filterException);
					}
				}
			}
		}

		private static bool TryExecuteFilter(IPipelineFilter filter, IEnvelope envelope, out Exception exception)
		{
			bool success = false;
			exception = null;

			try
			{
				filter.Execute(envelope);
				success = true;
			}
			catch (Exception pipelineFilterExcecption)
			{
				exception = pipelineFilterExcecption;
			}

			return success;
		}

		private string GetPipelineIdentifier()
		{
			return string.Format("Pipline [{0}]", pipelineId);
		}
	}
}