using System;
using System.Collections.Generic;
using System.Linq;
using Monaco.Bus.Internals;
using Monaco.Bus.MessageManagement.Callbacks;
using Monaco.Bus.Messages.For.Timeouts;
using Monaco.Configuration;
using Monaco.Endpoint.Control;
using Monaco.Subscriptions;
using Monaco.Transport;

namespace Monaco.Testing
{
	public class MockServiceBus : IMockServiceBus
	{
		public Action<string> OnStart { get; set; }
		public Action<string> OnStop { get; set; }
		public ITransport Transport { get; private set; }
		public bool IsRunning { get; private set; }
		public List<MessageStore> Messages { get; set; }

		public MockServiceBus()
		{
			this.Messages= new List<MessageStore>();
		}

		public void Verify()
		{

		}

		public void AssertThatMessageWasPublished(IMessage message)
		{
			var messages = new List<IMessage>();

			var publishedMessages = (from msg in this.Messages
			                         where msg.Action == MessageAction.Publish
			                         select msg.Messages).Distinct().ToList();

			foreach (var publishedMessageSet in publishedMessages)
			{
				messages.AddRange(publishedMessageSet);
			}

			var isPublished = (from msg in messages
			                   where msg.GetType() == message.GetType()
			                   select msg).ToList().Count > 0;

			if(!isPublished)
				throw new Exception(string.Format("The saga should publish the message '{0}'", message.GetType().FullName));
		}

		public void AssertThatMessageWasSent(IMessage message)
		{
			var messages = new List<IMessage>();

			var sentMessages = (from msg in this.Messages
									 where msg.Action == MessageAction.Send
									 select msg.Messages).Distinct().ToList();

			foreach (var currentMessageSet in sentMessages)
			{
				messages.AddRange(currentMessageSet);
			}

			var isSent = (from msg in messages
							   where msg.GetType() == message.GetType()
							   select msg).ToList().Count > 0;

			if (!isSent)
				throw new Exception(string.Format("The saga should send the message '{0}'", message.GetType().FullName));
		}

		public void AssertThatMessageWasRepliedTo(IMessage message)
		{
			var messages = new List<IMessage>();

			var repliedMessages = (from msg in this.Messages
								where msg.Action == MessageAction.Reply
								   select msg.Messages).Distinct().ToList();

			foreach (var currentMessageSet in repliedMessages)
			{
				messages.AddRange(currentMessageSet);
			}

			var isRepliedFor = (from msg in messages
						  where msg.GetType() == message.GetType()
						  select msg).ToList().Count > 0;

			if (!isRepliedFor)
				throw new Exception(string.Format("The saga should send reply message '{0}'", message.GetType().FullName));
		}

		public void Dispose()
		{
			
		}

		public void Start()
		{
			
		}

		public void Stop()
		{
			
		}

		public TComponent Find<TComponent>()
		{
			return default(TComponent);
		}

		public object Find(Type component)
		{
			return null;
		}

		public ICollection<TComponent> FindAll<TComponent>()
		{
			return null;
		}

		public TMessage CreateMessage<TMessage>() where TMessage : IMessage
		{
			return default(TMessage);
		}

		public IUnsubscribeToken Subscribe<TMessage>() where TMessage : IMessage
		{
			return null;
		}

		public ICallback Send(params IMessage[] message)
		{
			CreateStore(MessageAction.Send, message);
			return null;
		}

		public ICallback Send(Uri endpoint, params IMessage[] messages)
		{
			CreateStore(MessageAction.Send, messages);
			return null;
		}

		public ICallback Send(IMessage message)
		{
			CreateStore(MessageAction.Send, message);
			return null;
		}

		public ICallback Send<TMessage>(Action<TMessage> action) where TMessage : class, IMessage, new()
		{
			var message = new TMessage();
			action(message);

			CreateStore(MessageAction.Send, message);

			return null;
		}

		public IServiceAsyncRequest EnqueueRequest()
		{
			return null;
		}

		public ICollection<Subscription> Reply(IMessage message)
		{
			var list = new List<IMessage>();
			list.Add(message);

			CreateStore(MessageAction.Reply, message);
		}

		public void Reply<TMessage>(Action<TMessage> action) where TMessage : class, IMessage, new()
		{
			var message = new TMessage();
			action(message);

			CreateStore(MessageAction.Reply, message);
		}

		public ICollection<Subscription> Publish<TMessage>(Action<TMessage> action) where TMessage : class, IMessage, new()
		{
			var message = new TMessage();
			action(message);

			CreateStore(MessageAction.Publish, message);
		}

		public void Publish(params IMessage[] messages)
		{
			CreateStore(MessageAction.Publish, messages);
		}

		public IUnsubscribeToken AddInstanceSubscription<TConsumer>() where TConsumer : IConsumer
		{
			return null;
		}

		public IUnsubscribeToken AddInstanceSubscription(object instance)
		{
			return null;
		}

		public void HandleMessageLater(TimeSpan delay, IMessage message)
		{
			var timeout = new ScheduleTimeout(delay, message);
			this.Send(timeout);
		}

		public void ConfiguredWithEndpoint<TEndpointConfiguration>() where TEndpointConfiguration : BaseEndpointConfiguration, new()
		{
			
		}

		public void ConfiguredWithEndpoint(Type endpointConfigurationType)
		{
			
		}

		public IControlEndpoint GetControlEndpoint()
		{
			return null;
		}

		private void CreateStore(MessageAction action, params IMessage[] messages)
		{
			var store = new MessageStore
			{
				Action = action,
				Messages = new List<IMessage>(messages)
			};

			this.Messages.Add(store);
		}

	}
}