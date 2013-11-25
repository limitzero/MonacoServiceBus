using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Monaco.Bus.Agents.Scheduler;
using Monaco.Bus.Exceptions;
using Monaco.Bus.Internals;
using Monaco.Bus.Internals.Collections;
using Monaco.Bus.Internals.Eventing;
using Monaco.Bus.Internals.Reflection;
using Monaco.Bus.MessageManagement.Callbacks;
using Monaco.Bus.MessageManagement.Dispatcher;
using Monaco.Bus.MessageManagement.FaultHandling;
using Monaco.Bus.MessageManagement.Pipeline;
using Monaco.Bus.MessageManagement.Pipeline.Impl.Filters.MesssageModules;
using Monaco.Bus.MessageManagement.Resolving;
using Monaco.Bus.MessageManagement.Serialization;
using Monaco.Bus.Messages;
using Monaco.Bus.Messages.For.Faults;
using Monaco.Bus.Messages.For.Publications;
using Monaco.Bus.Messages.For.Subscriptions;
using Monaco.Bus.Repositories;
using Monaco.Bus.Services.HealthMonitoring.Messages.Events;
using Monaco.Bus.Services.Subscriptions;
using Monaco.Bus.Services.Subscriptions.Messages.Commands;
using Monaco.Bus.Services.Timeout;
using Monaco.Bus.Services.Timeout.Messages.Commands;
using Monaco.Configuration;
using Monaco.Configuration.Registration;
using Monaco.Endpoint;
using Monaco.Endpoint.Factory;
using Monaco.Endpoint.Impl.Control;
using Monaco.Extensibility.Logging;
using Monaco.Extensibility.Storage.Subscriptions;
using Monaco.Extensions;
using Monaco.Subscriptions.Impl;
using Monaco.Transport;

namespace Monaco.Bus
{
	public class DefaultServiceBus : IServiceBus
	{
		private const int MessageBatchSize = 256;
		private static readonly object EndpointLock = new object();
		private static readonly object RequestLock = new object();

		private readonly IContainer container;
		private readonly IScheduler scheduler;
		private readonly ITimeoutsService timeoutsService;
		private readonly IPipeline receivePipeline;
		private readonly ISubscriptionRepository subscriptionRepository;

		private bool disposed;
		private IThreadSafeList<IMessage> requests;
		private IThreadSafeList<IDisposableAction> tokens;
		private ITransport transport;
		private IThreadSafeDictionary<Type, List<IConsumer>> typedHandlersCache;

		public DefaultServiceBus(
			IContainer container,
			ITransport transport,
			IScheduler scheduler,
			ITimeoutsService timeoutsService,
			ISubscriptionRepository subscriptionRepository)
		{
			this.container = container;
			this.scheduler = scheduler;
			this.timeoutsService = timeoutsService;
			this.transport = transport;
			this.subscriptionRepository = subscriptionRepository;
			this.typedHandlersCache = new ThreadSafeDictionary<Type, List<IConsumer>>();
			this.requests = new ThreadSafeList<IMessage>();
			this.tokens = new ThreadSafeList<IDisposableAction>();

			// dedicated pipeline for receipt:
			this.receivePipeline = container.Resolve<IPipeline>();

			this.Endpoint = transport.Endpoint;
		}

		public static IEnvelope CurrentEnvelope { get; private set; }

		#region IServiceBus Members

		public event EventHandler<ComponentNotificationEventArgs> ComponentNotificationEvent;

		public IEndpoint Endpoint { get; private set; }

		public Action<string> OnStart { get; set; }

		public Action<string> OnStop { get; set; }

		public bool IsRunning { get; private set; }

		public TComponent Find<TComponent>()
		{
			TComponent theComponent = default(TComponent);

			if (disposed) return theComponent;

			object aComponent = Find(typeof(TComponent));

			if (aComponent != null)
			{
				theComponent = (TComponent)aComponent;
			}

			return theComponent;
		}

		public object Find(Type component)
		{
			object theComponent = null;

			if (disposed) return null;

			try
			{
				theComponent = container.Resolve(component);
			}
			catch (Exception exception)
			{
				throw CouldNotResolveComponentException(component, exception);
			}

			return theComponent;
		}

		public ICollection<TComponent> FindAll<TComponent>()
		{
			ICollection<TComponent> components = new List<TComponent>();

			if (disposed) return null;

			try
			{
				IEnumerable<TComponent> theComponents = container.ResolveAll<TComponent>();

				foreach (TComponent component in theComponents)
				{
					components.Add(component);
				}
			}
			catch (Exception exception)
			{
				throw CouldNotResolveAllImplementationsFromComponentException(typeof(TComponent), exception);
			}

			return components;
		}

		public TMessage CreateMessage<TMessage>()
		{
			TMessage message = default(TMessage);

			if (disposed) return message;

			if (typeof(TMessage).IsInterface == false || !typeof(IMessage).IsAssignableFrom(typeof(TMessage)))
			{
				throw MessageToCreateNotImplementedAsInterfaceException(typeof(TMessage));
			}

			message = container.Resolve<IReflection>().CreateMessage<TMessage>();

			return message;
		}

		public TMessage CreateMessage<TMessage>(Action<TMessage> create)
		{
			var message = this.CreateMessage<TMessage>();
			create(message);
			return message;
		}

		public void SetEndpoint(IEndpoint endpoint)
		{
			this.Endpoint = endpoint;
		}

		public IControlEndpoint GetControlEndpoint()
		{
			IControlEndpoint controlEndpoint = null;

			try
			{
				controlEndpoint = container.Resolve<IControlEndpoint>();

				if (controlEndpoint != null)
				{
					// make sure that the local endpoint is not the control:
					if (controlEndpoint.Uri != transport.Endpoint.EndpointUri.OriginalString)
						return controlEndpoint;
				}
			}
			catch
			{
				// no control endpoint found...do nothing:	
			}

			return controlEndpoint;
		}

		public void ConsumeMessages(params IMessage[] messages)
		{
			var dispatcher = container.Resolve<IMessageDispatcher>();
			var envelope = new Envelope(messages);
			dispatcher.Dispatch(this, envelope);

			//this.SendInternal(messages);
			//this.Send(this.Endpoint.EndpointUri, messages);
		}

		public void Publish<TMessage>() where TMessage : IMessage
		{
			var toPublish = CreateMessage<TMessage>();
			Publish(toPublish);
		}

		public void Publish<TMessage>(Action<TMessage> action)
			where TMessage : class, IMessage, new()
		{
			var message = new TMessage();

			if (action != null)
				action(message);

			Publish(message);
		}

		public void Publish(IMessage message)
		{
			var messages = new object[] { message };
			Publish(messages);
		}

		public void Publish(params object[] messages)
		{
			if (disposed) return;

			if (messages.Length > MessageBatchSize)
			{
				throw new MessageBatchExceededException();
			}

			var messagesToPublish = new List<object>();

			var nonSubscriptionDefinedMessages = new List<object>();

			IControlEndpoint control = GetControlEndpoint();

			foreach (object message in messages)
			{
				if (FindAllSubscriptionsForMessage(message).Count == 0)
				{
					if (control == null && typeof(IAdminMessage).IsAssignableFrom(message.GetType())) continue;
					nonSubscriptionDefinedMessages.Add(message);
				}
			}

			if (nonSubscriptionDefinedMessages.Count > 0)
			{
				// do not throw an error here as other messages in the batch may be ok:
				NoSubscriptionsRegisteredForMessagesPublicationException(nonSubscriptionDefinedMessages);

				if (control == null)
				{
					InvokeFaultHandlerForNonPublishableMessages(nonSubscriptionDefinedMessages);
				}
				else
				{
					foreach (IMessage nonSubscriptionDefinedMessage in nonSubscriptionDefinedMessages)
					{
						ForwardMessagesToControlEndpoint(
							new PublishMessage
								{
									Message = nonSubscriptionDefinedMessage,
									Endpoint = transport.Endpoint.EndpointUri.OriginalString
								});
					}
				}

				messagesToPublish = (from message in messages
									 where nonSubscriptionDefinedMessages.Contains(message) == false
									 select message).ToList();
			}
			else
			{
				messagesToPublish.AddRange(messages);
			}

			PublishInternal(messagesToPublish.ToArray());
		}

		public void Notify<TMessage>(Action<TMessage> action)
			where TMessage : class, IMessage, new()
		{
			var message = new TMessage();
			action(message);
			Notify(new object[] { message });
		}

		public void Notify(params object[] messages)
		{
			foreach (object message in messages)
			{
				ICollection<Subscription> subscriptions = FindAllSubscriptionsForMessage(message);

				if (subscriptions.Count == 0) continue;

				PublishInternal(message);
			}
		}

		public ICallback Send<TMessage>() where TMessage : IMessage
		{
			var toSend = CreateMessage<TMessage>();
			return Send(toSend);
		}

		public ICallback Send(params object[] messages)
		{
			var callback = Find<ICallback>();

			if (disposed) return null;

			foreach (IMessage message in messages)
				EnqueueRequestForPossibleReply(message);

			SendInternal(messages);

			return callback;
		}

		public ICallback Send<TMessage>(Action<TMessage> action)
			where TMessage : class, IMessage, new()
		{
			if (disposed) return null;

			var message = new TMessage();
			action(message);

			return Send(message);
		}

		public ICallback Send(IMessage message)
		{
			var messages = new List<IMessage> { message };
			return Send(messages.ToArray());
		}

		public ICallback Send(Uri endpoint, params object[] messages)
		{
			if (disposed) return null;

			var callback = Find<ICallback>();

			GuardOnBatchLimit(messages);

			var envelope = new Envelope(messages);
			envelope.Header.LocalEndpoint = transport.Endpoint.EndpointUri.ToString();
			envelope.Header.RemoteEndpoint = endpoint.ToString();
			envelope.Header.ReplyEndpoint = transport.Endpoint.EndpointUri.ToString();

			if (this.Endpoint.EndpointUri.OriginalString.Equals(endpoint.OriginalString) == false)
				envelope.Header.RecordStage(this, messages, "Send(Remote)");

			DispatchMessageViaTransport(endpoint, envelope);
		
			return callback;
		}

		public void Reply<TMessage>(Action<TMessage> action)
			where TMessage : class, IMessage, new()
		{
			var message = new TMessage();
			action(message);
			Reply(message);
		}

		public void Reply(object message)
		{
			if (this.disposed || CurrentEnvelope == null) return;

			// make sure the reply is correlated to a request before completing the cycle
			// (an internal callback is registered for every "Send" operation):
			var repository = Find<ICallBackRepository>();
			ICallback callback = repository.Correlate(CurrentEnvelope.Body.Payload, message);

			// make sure the callback is executed (if one is configured for the message response):
			if (callback != null)
			{
				try
				{
					callback.Complete(message);
				}
				catch (Exception ex)
				{
					var notificationEventArgs = new ComponentNotificationEventArgs(NotificationLevel.Warn,
																				   string.Format(
																					"Error completing call back for request '{0}' with reply '{1}'. Reason: {2} , Stack Trace : {3} ",
																					callback.ResponseMessage.GetType().Name,
																					message.GetType().Name,
																					ex.Message,
																					ex.StackTrace));
					OnBusNotification(notificationEventArgs);
				}
				finally
				{
					if (callback.AsyncRequest != null)
						repository.UnRegister(callback);
				}
			}
			else
			{
				// send the reply back to all interested parties (only if the async callback is not registered):
				if (CurrentEnvelope != null)
				{
					Send(CurrentEnvelope.Header.ReplyEndpoint.ToUri(), message);
					CurrentEnvelope = null;
				}
			}
		}

		/// <summary>
		/// This will add a one-time instance consumer w/subscription 
		/// to the service bus and will be enlisted in the global set 
		/// of consumer to message subscriptions. Also, the consumer 
		/// must inherit from <seealso cref="TransientConsumerOf{T}"/>
		/// to participate in the instance subscription and can be unregistered
		/// by calling the Dispose method.
		/// </summary>
		/// <typeparam name="TConsumer">Type of the consumer for transient message consumption.</typeparam>
		/// <returns>
		/// A <seealso cref="IDisposableAction"/> that can be called to remove the temporary registration.
		/// </returns>
		public IDisposableAction AddInstanceConsumer<TConsumer>() where TConsumer : IConsumer
		{
			if (disposed) return null;

			IDisposableAction token = CreateInstanceConsumerWithSubscriptions(typeof(TConsumer));
			tokens.Add(token);

			return token;
		}

		/// <summary>
		/// This will add a one-time instance consumer w/subscription 
		/// to the service bus and will be enlisted in the global set 
		/// of consumer to message subscriptions. Also, the consumer 
		/// must inherit from <seealso cref="TransientConsumerOf{T}"/>
		/// to participate in the instance subscription and can be unregistered
		/// by calling the Dispose method.
		/// </summary>
		/// <param name="instance">The current instance of the instance subscription.</param>
		/// <returns>
		/// A <seealso cref="IDisposableAction"/> that can be called to remove the temporary registration.
		/// </returns>
		public IDisposableAction AddInstanceConsumer(object instance)
		{
			if (disposed) return null;

			IDisposableAction token = CreateInstanceConsumerWithSubscriptions(instance.GetType());
			tokens.Add(token);

			return token;
		}

		/// <summary>
		/// This will enqueue a request to be sent using the BeginXXX/EndXXX pattern for managing
		/// the <seealso cref="IAsyncResult"/> object or to manage a semi-sychronous request reply 
		/// scenario where the calling code needs to directly get the response to a request.
		/// </summary>
		/// <returns></returns>
		public IServiceAsyncRequest EnqueueRequest()
		{
			return new ServiceBusAsyncRequestResult(this);
		}

		/// <summary>
		/// This will forcibly complete an asynchrous request to the service bus and return a message (if necessary)
		/// </summary>
		/// <typeparam name="TMessage">Requested message to be responded to asynchronously</typeparam>
		/// <param name="response"></param>
		public void CompleteAsyncRequestFor<TMessage>(IMessage response)
			where TMessage : class, IMessage, new()
		{
			var callbackRepository = Find<ICallBackRepository>();

			if (callbackRepository == null) return;

			ICallback callback = callbackRepository.Correlate(new TMessage(), response);

			if (callback == null) return;

			callback.Complete(response);

			callbackRepository.UnRegister(callback);
		}

		/// <summary>
		/// This will enqueue a message for the bus to handle at a later time.
		/// </summary>
		/// <param name="delay">Amount of time to delay handling of the mesasge</param>
		/// <param name="message">Message to delay delivery</param>
		public void Defer(TimeSpan delay, IMessage message)
		{
			var timeout = new ScheduleTimeout(delay, message)
							{
								Endpoint = this.Endpoint.EndpointUri.OriginalString
							};
			this.Send(this.Endpoint.EndpointUri, timeout);
		}

		/// <summary>
		/// This will configure the service bus according to the defined components and semantics of the endpoint configuration.
		/// </summary>
		/// <typeparam name="TEndpointConfiguration">Type corresponding to the endpoint configuration for the service bus.</typeparam>
		public void ConfiguredWithEndpoint<TEndpointConfiguration>()
			where TEndpointConfiguration : class, ICanConfigureEndpoint, new()
		{
			var endpointConfiguration = new TEndpointConfiguration();
			ConfiguredWithEndpoint(endpointConfiguration.GetType());
		}

		/// <summary>
		/// This will configure the service bus according to the defined components and semantics of the endpoint configuration.
		/// </summary>
		/// <param name="endpointConfigurationType">Type corresponding to the endpoint configuration for the service bus.</param>
		public void ConfiguredWithEndpoint(Type endpointConfigurationType)
		{
			if (typeof(ICanConfigureEndpoint).IsAssignableFrom(endpointConfigurationType) == false)
			{
				OnComponentNotification(this, new ComponentNotificationEventArgs(NotificationLevel.Warn,
																				 string.Format(
																					"The endpoint configuration of '{0}' was derived from '{1}." +
																					"All resources registered for this configuration WILL NOT participate on the message bus.",
																					endpointConfigurationType.FullName,
																					typeof(ICanConfigureEndpoint).FullName)));
				return;
			}

			var reflection = Find<IReflection>();
			var endpointConfiguration = reflection.BuildInstance(endpointConfigurationType) as ICanConfigureEndpoint;

			try
			{
				endpointConfiguration.Configure(Configuration.Configuration.Instance);

				OnComponentNotification(this, new ComponentNotificationEventArgs(NotificationLevel.Info,
																				 string.Format(
																					"The endpoint configuration of '{0}' was configured on the service bus at [{1}].",
																					endpointConfigurationType.FullName,
																					transport.Endpoint.EndpointUri)));
			}
			catch (Exception exception)
			{
				OnComponentNotification(this,
										new ComponentNotificationEventArgs(NotificationLevel.Warn,
																		   string.Format(
																			"An error has occurred while attempting to use the endpoint configuration '{0}', " +
																			"no components for this configuration will be used. Reason: {1}",
																			endpointConfigurationType.FullName,
																			exception)));
				throw;
			}
		}

		public void Start()
		{
			try
			{
				Initialize();
			}
			catch (Exception exception)
			{
				// throw and log the error and clean-up any initial state:
				Terminate(false, true);
				throw CouldNotStartMessageBusException(exception);
			}
		}

		public void Stop()
		{
			if (IsRunning == false) return;
			Dispose();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

		public event EventHandler<ComponentErrorEventArgs> ComponentErrorEvent;

		public IDisposableAction Subscribe<TMessage>() where TMessage : IMessage
		{
			return Subscribe(typeof(TMessage));
		}

		public void Dispose(bool disposing)
		{
			if (this.disposed == false)
			{
				if (disposing == true)
				{
					Terminate();
				}
				this.disposed = true;
			}
		}

		private IDisposableAction Subscribe(Type message, Type consumer = null)
		{
			if (disposed) return null;

			if (!typeof(IMessage).IsAssignableFrom(message)) return new DisposableAction(() => { });

			// this is a dynamic subscription that should be 
			// removed when not needed any more for the bus:
			var subscription = new Subscription();
			subscription.Component = consumer == null ? "{N/A}" : consumer.FullName;
			subscription.Message = message.FullName;
			subscription.Uri = transport.Endpoint.EndpointUri.OriginalString;

			this.ConsumeMessages(new RegisterSubscriptionMessage { Subscription = subscription });
			Action remove = () => this.ConsumeMessages(new UnregisterSubscriptionMessage { Subscription = subscription });

			var token = new DisposableAction(remove);
			tokens.Add(token);

			return token;
		}

		private void Initialize()
		{
			// start the background processes:
			try
			{
				RefreshSubscriptions();
				RefreshEndpoint(true);
				RefreshAgents(true);
				RefreshScheduler(true);
				RefreshBusModules(true);
			}
			catch (Exception e)
			{
				if (!OnBusError(e))
					throw;
			}

			// send the event to the log that the bus has started:
			OnComponentStarted(this, new ComponentStartedEventArgs("Bus"));

			// signal any external event consumers of this bus that it has started:
			if (OnStart != null)
			{
				OnStart("Bus Started");
			}

			// send the message to allow the consumers to gracefully start-up:
			var msg = new EndpointStarted { Endpoint = transport.Endpoint.EndpointUri.OriginalString };
			var envelope = new Envelope(msg);
			envelope.Header.RecordStage(this, msg, "Bus Started");
			
			InvokeConsumersForReceive(envelope);

			IsRunning = true;
		}

		private void Terminate(bool haltInfrastructure = true, bool cleanUpState = true)
		{
			if (IsRunning)
			{
				// halt the infrastructure for the bus:
				if (haltInfrastructure)
				{
					// forcibly un-subscribe transient instances:
					foreach (IDisposableAction token in this.tokens)
					{
						token.Dispose();
					}

					// send the message to allow the consumers to gracefully shut-down:
					var msg = new EndpointStopped { Endpoint = this.Endpoint.EndpointUri.OriginalString };
					var envelope = new Envelope(msg);
					envelope.Header.RecordStage(this, msg, "Bus Stop");

					InvokeConsumersForReceive(envelope);

					// stop the background processes:
					RefreshEndpoint(false);
					RefreshAgents(false);
					RefreshScheduler(false);
					RefreshBusModules(false);

					// send the event to the log that the bus has stopped:
					OnComponentStopped(this, new ComponentStoppedEventArgs("Bus"));

					// signal any external event consumers of this bus that it has stopped:
					if (OnStop != null)
					{
						OnStop("Bus Stopped");
					}
				}
			}

			// clean up any internal state:
			if (cleanUpState)
			{
				if (tokens != null)
				{
					tokens.Clear();
				}
				tokens = null;

				if (requests != null)
				{
					requests.Clear();
				}
				requests = null;

				if (typedHandlersCache != null)
				{
					typedHandlersCache.Clear();
				}
				typedHandlersCache = null;

				if (transport != null)
				{
					transport = null;
				}
			}

			IsRunning = false;
		}

		private void RefreshSubscriptions()
		{
			var dispatcher = container.Resolve<IMessageDispatcher>();
			dispatcher.Dispatch(this, new Envelope(new RefreshLocalSubscriptions()));
		}

		private void RefreshEndpoint(bool isStarting)
		{
			if (isStarting)
			{
				if (transport == null)
					throw new ApplicationException(
						string.Format(
							"No transports have been configured and/or registered for the scheme '{0}' on the service bus endpoint." +
							"Please remember to include the library that contains your transport to the executable directory of the service bus.",
							transport.Endpoint.Scheme));

				lock (EndpointLock)
				{
					transport.OnMessageReceived += OnMessageReceivedEventHandler;
					transport.OnReceiveError += OnTransportReceiveErrorEventHandler;
					transport.OnRetryExhausted += OnTransportRetryExhaustedEventHandler;
					transport.SerializationProvider = Find<ISerializationProvider>();
					((IStartable)transport).Start();

					OnBusNotification(new ComponentNotificationEventArgs(NotificationLevel.Info,
																		 string.Format(
																			"Service Bus Transport Information >> Type: {0} , Txns Supported: {1}, Txn Isolation Level : {2}, " +
																			" #Worker Threads: {3}, # Retries: {4}",
																			transport.GetType().FullName,
																			transport.IsTransactional,
																			transport.TransactionIsolationLevel,
																			transport.NumberOfWorkerThreads.ToString("G"),
																			transport.MaxRetries.ToString("G")
																			)));
				}
			}
			else
			{
				lock (EndpointLock)
				{
					transport.OnMessageReceived -= OnMessageReceivedEventHandler;
					transport.OnReceiveError -= OnTransportReceiveErrorEventHandler;
					transport.OnRetryExhausted -= OnTransportRetryExhaustedEventHandler;
					((IStartable)transport).Stop();
				}
			}
		}

		private void RefreshAgents(bool isStarting)
		{
			if (isStarting)
			{
				this.timeoutsService.Bus = this;
				((IStartable)this.timeoutsService).Start();
			}
			else
			{
				if (this.timeoutsService != null)
				{
					((IStartable)this.timeoutsService).Stop();
				}
			}
		}

		private void RefreshScheduler(bool isStarting)
		{
			if (isStarting)
			{
				this.scheduler.ComponentNotificationEvent += OnComponentNotification;
				this.scheduler.ComponentErrorEvent += OnComponentError;

				if (this.scheduler.IsRunning == false)
				{
					this.scheduler.OnMessageReceived = (message) => RouteScheduledMessage(message);
					this.scheduler.Start();
				}
			}
			else
			{
				this.scheduler.OnMessageReceived = null;
				this.scheduler.ComponentNotificationEvent -= OnComponentNotification;
				this.scheduler.ComponentErrorEvent -= OnComponentError;
				this.scheduler.Stop();
			}
		}

		private void RefreshBusModules(bool isStarting)
		{
			ICollection<IBusModule> modules = null;

			try
			{
				modules = FindAll<IBusModule>();
			}
			catch
			{
			}

			if (modules != null)
			{
				foreach (IBusModule module in modules)
				{
					try
					{
						if (isStarting)
						{
							module.Start(container);

							var notificationEventArgs = new ComponentNotificationEventArgs(NotificationLevel.Info,
																						   string.Format(
																							"Bus module '{0}' started on endpoint '{1}'",
																							module.GetType().Name,
																							transport.Endpoint.EndpointUri.OriginalString));

							OnBusNotification(notificationEventArgs);
						}
						else
						{
							module.Dispose();

							var notificationEventArgs = new ComponentNotificationEventArgs(NotificationLevel.Info,
																						   string.Format(
																							"Bus module '{0}' stopped on endpoint '{1}",
																							module.GetType().Name,
																							transport.Endpoint.EndpointUri.OriginalString));

							OnBusNotification(notificationEventArgs);
						}
					}
					catch (Exception exception)
					{
						var notificationEventArgs = new ComponentNotificationEventArgs(NotificationLevel.Warn,
																					   string.Format(
																						"An error has occurred while starting/stopping bus module '{0}', the module will not be avaliable if starting the service bus. Reason: {1} , Stack Trace : {2} ",
																						module.GetType().Name,
																						exception.Message,
																						exception.StackTrace));
						OnBusNotification(notificationEventArgs);

						module.Dispose();

						continue;
					}
				}
			}
		}

		private void InvokeFaultHandlerForNonPublishableMessages(IEnumerable<object> messages)
		{
			foreach (object message in messages)
			{
				// move the message that could not be consumed to the recovery/error endpoint:
				var fault = new NoSubscriptionForMessageFaultMessage { Message = message };
				var processor = Find<IFaultProcessor>();
				processor.Process(fault);
			}
		}

		private void EnqueueRequestForPossibleReply<TMessage>(TMessage message)
			where TMessage : class, IMessage
		{
			lock (RequestLock)
			{
				if (typeof(IAdminMessage).IsAssignableFrom(message.GetType()) == false)
				{
					requests.AddUnique(message);
				}
			}
		}

		/// <summary>
		/// This will take any message that is created by the scheduled
		/// task and send it to the corresponding endpoint or consumer.
		/// </summary>
		/// <param name="message">Message sent by scheduled task</param>
		private void RouteScheduledMessage(object message)
		{
			if (message is EndpointHeartBeat)
			{
				var endpoint = new Uri(((EndpointHeartBeat)message).EndpointUri);
				Send(endpoint, message);
			}
			else if (typeof(IAdminMessage).IsAssignableFrom(message.GetType()))
			{
				this.ConsumeMessages(message as IMessage);
			}
			else
			{
				Publish(message);
			}
		}

		/// <summary>
		/// This will ensure that a message batch or a collection on a message does 
		/// not exceed the pre-set limit for itemized collection passing on the bus instance.
		/// </summary>
		/// <param name="messages">Collection to inspect for exceeding batch limit</param>
		private static void GuardOnBatchLimit(ICollection<object> messages)
		{
			if (messages.Count > MessageBatchSize)
			{
				throw new MessageBatchExceededException();
			}

			foreach (object message in messages)
			{
				IEnumerable<PropertyInfo> collectionProperties =
					(from property in message.GetType().GetProperties()
					 where typeof(IEnumerable).IsAssignableFrom(
						property.PropertyType)
					 select property).ToList();

				foreach (PropertyInfo property in collectionProperties)
				{
					ICollection collection = null;

					try
					{
						collection = (ICollection)property.GetValue(message, null);
					}
					catch
					{
						continue;
					}

					if (collection != null)
					{
						if (collection.Count > MessageBatchSize)
						{
							throw new MessageBatchExceededForMessageException(message);
						}
					}
				}
			}
		}

		/// <summary>
		/// This will send the message out to the message owners for processing.
		/// </summary>
		/// <param name="messages">Collection of messages to send.</param>
		private void SendInternal(params object[] messages)
		{
			this.PushMessages("Send", messages);
		}

		/// <summary>
		/// This will send the message out to the intended parties for processing.
		/// </summary>
		/// <param name="messages">Collection of messages to publish.</param>
		private void PublishInternal(params object[] messages)
		{
			this.PushMessages("Publish", messages);
		}

		/// <summary>
		/// This will send the message out to the message owners for processing.
		/// </summary>
		/// <param name="stage"></param>
		/// <param name="messages">Collection of messages to send.</param>
		private void PushMessages(string stage, params  object[] messages)
		{
			GuardOnBatchLimit(messages);

			var envelope = new Envelope();
			envelope.Header.LocalEndpoint = transport.Endpoint.EndpointUri.OriginalString;
			envelope.Header.ReplyEndpoint = transport.Endpoint.EndpointUri.OriginalString;
			envelope.Body.Payload = messages;
			envelope.Header.RecordStage(this, messages, stage);

			if (envelope.Body.Payload.Count() > 0)
			{
				var pipeline = this.container.Resolve<IPipeline>();
				pipeline.Execute(PipelineDirection.Send, this, envelope);
			}

		}

		/// <summary>
		/// This will find the correct endpoint where the message 
		/// can be consumed and subsequently the message is sent
		/// to the endpoint according to the endpoint semantics.
		/// </summary>
		/// <param name="envelope"></param>
		private ICollection<Subscription> DeliverToEndpoint(IEnvelope envelope)
		{
			object message = envelope.Body.Payload;
			ICollection<Subscription> subscriptions = FindAllSubscriptionsForMessage(message);

			// at this point, a component has issued a request to send a message
			// out to the endpoint for all interested components to pick-up and 
			// process, we need to find all of the parties that are interested in receiving 
			// the message via the subscription repository:
			if (typeof(IAdminMessage).IsAssignableFrom(message.GetType()))
			{
				// do not dispatch the messge to the physical endpoint 
				// if it is an internal admin message or event, this will create a 
				// a recursive loop of receiving and sending on the endpoint,
				// just push it to the component that can handle the message:
				InvokeConsumersForReceive(envelope);
			}
			else
			{
				// dispatch the message to the endpoint:
				InvokeTransportForSend(envelope, subscriptions);
			}

			return subscriptions;
		}

		/// <summary>
		/// This will take the current messaging envelope and 
		/// use the service message pipeline to change the message
		/// into the appropriate format for dispatching to the component 
		/// for consumption.
		/// </summary>
		/// <param name="envelope">Message to send to service for dispatch.</param>
		private void InvokeConsumersForReceive(IEnvelope envelope)
		{
			if(envelope.Body.Payload.Any(m => typeof(IAdminMessage).IsAssignableFrom(m.GetType())) == false)
			{
				BuildCurrentMessageEnvelope(envelope);
			}

			// execute receive pipeline for current message:
			RegisterMessageModulesForPipeline(receivePipeline);
			receivePipeline.Execute(PipelineDirection.Receive, this, envelope);
		}

		private void RegisterMessageModulesForPipeline(IPipeline pipeline)
		{
			var startMessageModulesFilter = new StartMessageModulesFilter(container);
			var endMessageModulesFilter = new EndMessageModulesFilter(container);
			pipeline.RegisterPreReceiveFilter(startMessageModulesFilter);
			pipeline.RegisterPostReceiveFilter(endMessageModulesFilter);
		}

		/// <summary>
		/// This will find the correct message handling component 
		/// and dispatch the message directly to it.
		/// </summary>
		/// <param name="envelope"></param>
		private void DeliverToComponent(IEnvelope envelope)
		{
			//IEnumerable<IConsumer> handlers = null;
			//object message = envelope.Body.Payload;
			//bool faultHandlerExecuted = false;

			var pipeline = container.Resolve<IPipeline>();

			if (pipeline != null)
			{
				pipeline.Execute(PipelineDirection.Receive, this, envelope);
			}
		}

		/// <summary>
		/// This will invoke the message modules for start and completion of message dispatch to a component:
		/// </summary>
		/// <param name="envelope"></param>
		/// <param name="useStartAction"></param>
		private void FireMessageModules(
			IEnvelope envelope,
			bool useStartAction = true)
		{
			if (typeof(IAdminMessage).IsAssignableFrom(envelope.Body.Payload.GetType())) return;

			IEnumerable<IMessageModule> modules = container.ResolveAll<IMessageModule>().Distinct();

			if (modules == null || modules.Count() == 0) return;

			foreach (IMessageModule module in modules)
			{
				try
				{
					if (useStartAction)
					{
						envelope.Header.RecordStage(module, envelope.Body.Payload, "Start Module - " + module.GetType().Name);
						module.OnMessageStartProcessing(this.container, envelope.Body.Payload);
					}
					else
					{
						envelope.Header.RecordStage(module, envelope.Body.Payload, "End Module - " + module.GetType().Name);
						module.OnMessageEndProcessing(container, envelope.Body.Payload);
					}
				}
				catch (Exception exception)
				{
					string msg =
						string.Format(
							"An error has occurred while attempting to execute the message module '{0}', it will be skipped as a result. Reason: {1}",
							module.GetType().FullName, exception);
					OnComponentNotification(this, new ComponentNotificationEventArgs(NotificationLevel.Warn, msg));
					continue;
				}
			}
		}

		/// <summary>
		/// This will take the current native message implemenation 
		/// and find the correct endpoint uri from the subscription 
		/// repository and deliver the message to the indicated endpoint.
		/// </summary>
		/// <param name="envelope">Message to send to an endpoint.</param>
		/// <param name="subscriptions"></param>
		private void InvokeTransportForSend(IEnvelope envelope, IEnumerable<Subscription> subscriptions)
		{
			foreach (Subscription subscription in subscriptions)
			{
				envelope.Header.RemoteEndpoint = subscription.Uri;

				DispatchMessageViaTransport(new Uri(subscription.Uri), envelope);

				var endpointMessageSent =
					new EndpointMessageSent(string.Empty, subscription.Uri, envelope.Body.Payload);

				this.ConsumeMessages(endpointMessageSent);
			}
		}

		/// <summary>
		/// This will extract all of the consumers of the given message.
		/// </summary>
		/// <param name="envelope"></param>
		/// <param name="message">Message to inspect for all locally available consumers.</param>
		/// <param name="throwExceptionOnHandlerNotFound"></param>
		/// <param name="faultHandlerExecuted"></param>
		/// <returns></returns>
		private IEnumerable<IConsumer> GetHandlersForMessage(IEnvelope envelope,
															 object message,
															 out bool faultHandlerExecuted,
															 bool throwExceptionOnHandlerNotFound = true)
		{
			var theHandlers = new List<IConsumer>();

			faultHandlerExecuted = false;

			if (disposed) return theHandlers;

			// check the cache first for the handlers that are keyed by type:
			typedHandlersCache.TryGetValue(message.GetType(), out theHandlers);

			if (theHandlers != null && theHandlers.Count > 0) return theHandlers;

			// get a new collection of handlers (try-get returns null):
			theHandlers = new List<IConsumer>();

			// check to see if a set of pre-configured handlers are registered for the message:
			var handlerConfigurationRegistry = Find<IHandlerConfigurationRepository>();
			ICollection<Type> configuredHandlers =
				handlerConfigurationRegistry.FindConsumersForMessage(message);

			if (configuredHandlers.Count > 0)
			{
				foreach (Type configuredHandler in configuredHandlers)
				{
					object consumer = Find(configuredHandler);
					if (consumer == null) continue;
					theHandlers.Add(consumer as IConsumer);
				}
			}
			else
			{
				// get the message handlers for the current message and its interfaces:
				List<Type> implementations = ResolvedConsumersImplementationsFromMessage(message.GetType());

				// get the implementations for all of the interfaces on the message proxy:
				if (message.GetType().Name.Contains("Proxy"))
				{
					foreach (Type theInterface in message.GetType().GetInterfaces())
					{
						List<Type> impl = ResolvedConsumersImplementationsFromMessage(theInterface);
						implementations.AddRange(impl);
					}
				}

				// pull back all of the message handlers for the list of implementations:
				foreach (Type implementation in implementations)
				{
					HashSet<IConsumer> handlers = container.ResolveAll(implementation).ToHashSet<IConsumer>();
					theHandlers.AddRange(handlers);
				}
			}

			// add all of the distinct handlers to the cache for the current message:
			if (theHandlers.Count > 0)
			{
				typedHandlersCache.TryAddValue(message.GetType(), theHandlers.Distinct().ToList());
			}

			if (throwExceptionOnHandlerNotFound == false && theHandlers.Count() == 0)
			{
				faultHandlerExecuted = true;

				if (typeof(IAdminMessage).IsAssignableFrom(message.GetType()) == false)
				{
					// move the message that could not be consumed to the recovery/error endpoint:
					var fault = new NoConsumerForMessageFaultMessage { Message = message };
					var processor = Find<IFaultProcessor>();
					processor.Process(fault, envelope);
				}
			}

			return theHandlers.Distinct().ToList();
		}

		/// <summary>
		/// This will create the generic type from the common interface-based consumer contract.
		/// methods.
		/// </summary>
		/// <param name="message">Message to create the generic interface types for.</param>
		/// <returns></returns>
		private static List<Type> ResolvedConsumersImplementationsFromMessage(Type message)
		{
			var consumerImplementations = new List<Type>();

			if (message.FullName.StartsWith(typeof(CorrelatedBy<>).FullName)) return consumerImplementations;
			if (message.FullName.StartsWith("Castle")) return consumerImplementations;

			// transient consumer implementations:
			consumerImplementations.Add(typeof(TransientConsumerOf<>).MakeGenericType(message));
			consumerImplementations.Add(typeof(TransientConsumerOf<>).MakeGenericType(typeof(IMessage)));

			// state machine messages:
			consumerImplementations.Add(typeof(StartedBy<>).MakeGenericType(message));
			consumerImplementations.Add(typeof(OrchestratedBy<>).MakeGenericType(message));

			// add the generic implementation for the state machine messages:
			consumerImplementations.Add(typeof(StartedBy<>).MakeGenericType(typeof(IMessage)));
			consumerImplementations.Add(typeof(OrchestratedBy<>).MakeGenericType(typeof(IMessage)));

			// add the type for the consumer message:
			consumerImplementations.Add(typeof(Consumes<>).MakeGenericType(message));

			// generic catch all implementations from base contract:
			consumerImplementations.Add(typeof(Consumes<>).MakeGenericType(typeof(IMessage)));

			return consumerImplementations;
		}

		/// <summary>
		/// This will create a temporary message subscription for the 
		/// component type provided on the service bus.
		/// </summary>
		/// <param name="theType">Type of the consumer to create a temporary registration for.</param>
		/// <returns></returns>
		private IDisposableAction CreateInstanceConsumerWithSubscriptions(Type theType)
		{
			var theMessages = new List<Type>();
			var keys = new List<string>();

			List<Type> theInterfaces = (from anInterface in theType.GetInterfaces()
										where anInterface.FullName.StartsWith(typeof(TransientConsumerOf<>).FullName)
										select anInterface).Distinct().ToList();

			if (theInterfaces == null || theInterfaces.Count == 0)
			{
				throw InstanceSubscriptionIsNotConfiguredAsTransientConsumerException(theType);
			}

			if (theInterfaces.Count > 0)
			{
				theMessages = (from type in theInterfaces
							   let message = type.GetGenericArguments()[0]
							   select message).Distinct().ToList();

				// can only have transient consumers with concrete messages:
				if ((from message in theMessages where message.IsInterface select message).Count() > 0)
				{
					throw InterfacesDefinedForTransientMessageConsumptionException(theType);
				}

				// register the component against all declared interfaces for message consumption:
				var registrar = container.Resolve<IRegisterConsumer>();
				IEnumerable<string> transientRegistrationKeys = registrar.RegisterType(theType);
				keys = new List<string>(transientRegistrationKeys);
		
				// build the local subscriptions against the component:
				if (theMessages.Count > 0)
				{
					foreach (Type message in theMessages)
					{
						Subscribe(message, theType);
					}
				}
			}

			// create the action to remove the instance subscription and component:
			Action remove = () =>
								{
									foreach (Type message in theMessages)
									{
										var subscription = new Subscription
															{
																Component = theType.FullName,
																Message = message.FullName,
																Uri = this.Endpoint.EndpointUri.OriginalString
															};
										this.ConsumeMessages(new UnregisterSubscriptionMessage { Subscription = subscription });
									}
								};

			OnComponentNotification(this,
									new ComponentNotificationEventArgs(NotificationLevel.Debug,
																	   string.Format(
																		"Created instance subscription for transient consumer '{0}' and its consumed message(s) '[{1}]' on the bus endpoint.",
																		theType.FullName, theMessages.ToItemList())));

			return new DisposableAction(remove);
		}

		private void DispatchMessageViaTransport(Uri endpoint, IEnvelope envelope)
		{
			Exchange exchange = Find<IEndpointFactory>().Build(endpoint);

			if (exchange != null)
			{
				exchange.Transport.SerializationProvider = Find<ISerializationProvider>();
				exchange.Transport.Send(exchange.Endpoint, envelope);
			}
		}

		private void BuildCurrentMessageEnvelope(IEnvelope envelope)
		{
			CurrentEnvelope = envelope;
		}

		private ICollection<Subscription> FindAllSubscriptionsForMessage(object message)
		{
			var subscriptions = new List<Subscription>();

			// get all of the types representing the message:
			Type type = message.GetImplementationFromProxy();
			var types = new List<Type>(type.GetInterfaces());
			types.Add(type);

			// find and remove the proxied message and use the interfaces instead:
			Type proxyMessageType = types.Find(p => p.Name.Contains("Proxy"));

			if (proxyMessageType != null)
				types.Remove(proxyMessageType);

			// resolve all of the subscriptions:
			var repository = container.Resolve<ISubscriptionRepository>();

			foreach (Type messageType in types)
			{
				var currentSubscriptions = repository.Find(messageType);

				if (currentSubscriptions.Count > 0)
				{
					subscriptions.AddRangeDistinct(currentSubscriptions);
				}
			}

			List<Subscription> distinctSubscriptions = (from item in subscriptions select item).ToList().Distinct().ToList();

			return distinctSubscriptions;
		}

		private void ForwardMessagesToControlEndpoint(params object[] messages)
		{
			IControlEndpoint controlEndpoint = GetControlEndpoint();

			if (controlEndpoint == null) return;

			foreach (object message in messages)
			{
				Send(controlEndpoint.Uri.ToUri(), message);
			}
		}

		/// <summary>
		/// This is the callback that is attached to every service endpoint instance 
		/// <seealso cref="RefreshEndpoint"/> that will relay the received message 
		/// to the message bus for possible multi-component consumption.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="messageReceivedEventArgs">Event arguements from the transport containing the message to process.</param>
		private void OnMessageReceivedEventHandler(object sender, MessageReceivedEventArgs messageReceivedEventArgs)
		{
			IEnvelope envelope = messageReceivedEventArgs.Envelope;

			if(envelope.Body.Payload.Any(m=> typeof(IAdminMessage).IsAssignableFrom(m.GetType())) == false)
			{
				var messageReceivedEvent =
					new EndpointMessageReceived(transport.Endpoint.EndpointUri.OriginalString, 
						envelope.Body.Payload.ToItemList());
				this.ConsumeMessages(messageReceivedEvent);
			}

			InvokeConsumersForReceive(envelope);
		}

		private void OnTransportReceiveErrorEventHandler(IEnvelope envelope, Exception exception)
		{
			// ("fault" condition) at this point, let the fault handlers run that are defined for the message:
			var faultProcessor = Find<IFaultProcessor>();
			faultProcessor.Process(envelope.Body.Payload, envelope, exception);
			
			OnComponentNotification(this,
									new ComponentNotificationEventArgs(NotificationLevel.Warn, exception.ToString()));
		}

		private void OnTransportRetryExhaustedEventHandler(IEnvelope envelope, Exception exception)
		{
			OnTransportReceiveErrorEventHandler(envelope, exception);
		}

		private void OnBusNotification(ComponentNotificationEventArgs args)
		{
			EventHandler<ComponentNotificationEventArgs> evt = ComponentNotificationEvent;

			if (evt != null)
			{
				evt(this, args);
			}
			else
			{
				try
				{
					var logger = Find<ILogger>();

					if (logger == null) return;

					switch (args.Level)
					{
						case NotificationLevel.Debug:
							logger.LogDebugMessage(args.Message);
							break;
						case NotificationLevel.Info:
							logger.LogInfoMessage(args.Message);
							break;
						case NotificationLevel.Warn:
							logger.LogWarnMessage(args.Message);
							break;
					}
				}
				catch
				{
					// no logging component registered...
#if DEBUG
					Debug.WriteLine(args.Message);
#endif
				}
			}
		}

		private void OnComponentNotification(object sender, ComponentNotificationEventArgs e)
		{
			OnBusNotification(e);
		}

		private void OnComponentStarted(object sender, ComponentStartedEventArgs e)
		{
			string message = string.Format("Component '{0}' started.", e.ComponentName);
			var args =
				new ComponentNotificationEventArgs(NotificationLevel.Debug, message);

			OnBusNotification(args);
		}

		private void OnComponentStopped(object sender, ComponentStoppedEventArgs e)
		{
			string message = string.Format("Component '{0}' stopped.", e.ComponentName);
			var args = new ComponentNotificationEventArgs(NotificationLevel.Debug, message);

			OnBusNotification(args);
		}

		private void OnComponentError(object sender, ComponentErrorEventArgs e)
		{
			OnBusError(e.Exception);
		}

		private bool OnBusError(Exception exception)
		{
			EventHandler<ComponentErrorEventArgs> evt = ComponentErrorEvent;

			bool isHandlerAttached = (evt != null);

			if (isHandlerAttached)
				evt(this, new ComponentErrorEventArgs(exception));

			var logger = Find<ILogger>();

			if (logger != null)
			{
				logger.LogErrorMessage(exception.Message, exception);
			}

			return isHandlerAttached;
		}

		private void NoSubscriptionsRegisteredForMessagesPublicationException(IEnumerable<object> messages)
		{
			var theMessages = new StringBuilder();

			foreach (object message in messages)
			{
				theMessages.Append(message.GetType().FullName).Append(", ");
			}

			if (GetControlEndpoint() == null)
			{
				string listing = theMessages.ToString().TrimEnd(new[] { ',', ' ' });

				string msg =
					string.Format(
						"For the following message(s) '{0}' a subscription entry was not found for publication on bus endpoint '{1}'. " +
						"Please ensure that the message(s) has been properly mapped to an endpoint for enlistment in the subscriptions listing for publication.",
						listing,
						transport.Endpoint.EndpointUri.OriginalString);

				var theException = new Exception(msg);

				Find<ILogger>().LogErrorMessage(theException.Message, theException);
			}
			else
			{
				ForwardMessagesToControlEndpoint(messages.ToArray());
			}
		}

		private Exception CouldNotResolveAllImplementationsFromComponentException(Type component, Exception exception)
		{
			var theException =
				new Exception(
					string.Format("The following component '{0}' did not not have any mulitple implemenations in the container." +
								  "Please check the external configuration and/or the endpoint configuration to make sure that the component and its implemenations are added to the container.",
								  component.FullName), exception);
			Find<ILogger>().LogErrorMessage(theException.Message, theException);
			return theException;
		}

		private Exception CouldNotResolveComponentException(Type component, Exception exception)
		{
			var theException =
				new Exception(string.Format("The following component '{0}' could not be resolved from the container." +
											"Please check the external configuration and/or the endpoint configuration to make sure that the component is added to the container.",
											component.FullName), exception);
			Find<ILogger>().LogErrorMessage(theException.Message, theException);
			return theException;
		}

		private Exception CouldNotCreateComponentFromInterfaceException(Type component, Exception exception)
		{
			var theException =
				new Exception(
					string.Format("The following component '{0}' could not be created from its interface-based implementation." +
								  "Please check the external configuration and/or the endpoint configuration to make sure that the message is added to the container.",
								  component.FullName), exception);
			Find<ILogger>().LogErrorMessage(theException.Message, theException);
			return theException;
		}

		private Exception CouldNotStartMessageBusException(Exception exception)
		{
			var theException =
				new Exception("An error has occurred while attempting to start the message bus. Reason:" + exception.Message,
							  exception);
			Find<ILogger>().LogErrorMessage(theException.Message, theException);
			return theException;
		}

		private Exception InstanceSubscriptionIsNotConfiguredAsTransientConsumerException(Type consumer)
		{
			var theException =
				new Exception(string.Format("For registering the message consumer '{0}' as a transient instance, the consumer " +
											"must only implement the interface '{1}'.",
											consumer.FullName,
											typeof(TransientConsumerOf<>).FullName.Replace("`1", "<>")));
			Find<ILogger>().LogWarnMessage(theException.Message, theException);
			return theException;
		}

		private Exception MessageToCreateNotImplementedAsInterfaceException(Type type)
		{
			var theException =
				new Exception(
					string.Format(
						"The message '{0}' was not defined as an interface for the bus to create a proxy from the interface specification. " +
						"In order to create a message, the definition must be supplied as an interface.", type.FullName));
			Find<ILogger>().LogWarnMessage(theException.Message, theException);
			return theException;
		}

		private Exception InterfacesDefinedForTransientMessageConsumptionException(Type type)
		{
			var theException =
				new Exception(string.Format("For the transient message consumer '{0}' that is registered on the message bus, " +
											"the list of consumed messages can not be based on an interface definition. Only concrete message types can be noted " +
											"for consumption on a transient message consumer.", type.FullName));

			Find<ILogger>().LogWarnMessage(theException.Message, theException);

			return theException;
		}
	}
}