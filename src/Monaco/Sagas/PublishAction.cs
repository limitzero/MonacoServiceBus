using System;
using Monaco.Extensions;

namespace Monaco.Sagas
{
	public abstract class BaseStateMachineAction
	{
		public abstract void Execute();

		protected bool IsMocked(IServiceBus serviceBus)
		{
			return serviceBus.GetType().FullName.Contains("proxy");
		}

		protected TMessage CreateMessage<TMessage>(IServiceBus serviceBus)
		{
			var message = default(TMessage);

			return message;
		}
	}

	public class DoAction<TCurrentMessage> : BaseStateMachineAction
	 where TCurrentMessage : ISagaMessage
	{
		private readonly TCurrentMessage _currentMessage;
		private readonly Action<TCurrentMessage> _do;

		public DoAction(IServiceBus serviceBus,
			TCurrentMessage currentMessage,
			Action<TCurrentMessage> doAction)
		{
			_currentMessage = currentMessage;
			_do = doAction;
		}

		public override void Execute()
		{
			_do(_currentMessage);
		}
	}

	public class PublishAction<TCurrentMessage, TMessageToPublish> : BaseStateMachineAction 
		where TCurrentMessage : ISagaMessage
	{
		private readonly IServiceBus _serviceBus;
		private readonly TCurrentMessage _currentMessage;
		private readonly Action<TCurrentMessage, TMessageToPublish> _publish;
		
		public PublishAction(IServiceBus serviceBus, 
			TCurrentMessage currentMessage, 
			Action<TCurrentMessage, TMessageToPublish> publish)
		{
			_serviceBus = serviceBus;
			_currentMessage = currentMessage;
			_publish = publish;
		}

		public override void Execute()
		{
			var message = CreateMessage<TMessageToPublish>(_serviceBus);
			_publish(_currentMessage, message);

			if(!IsMocked(_serviceBus))
			{
				_serviceBus.Publish(message);
			}
		}
	}

	public class SendAction<TCurrentMessage, TMessageToSend> : BaseStateMachineAction
		where TCurrentMessage : ISagaMessage
	{
		private readonly IServiceBus _serviceBus;
		private readonly TCurrentMessage _currentMessage;
		private readonly Action<TCurrentMessage, TMessageToSend> _send;

		public SendAction(IServiceBus serviceBus,
			TCurrentMessage currentMessage,
			Action<TCurrentMessage, TMessageToSend> publish)
		{
			_serviceBus = serviceBus;
			_currentMessage = currentMessage;
			_send = publish;
		}

		public override void Execute()
		{
			var message = CreateMessage<TMessageToSend>(_serviceBus);
			_send(_currentMessage, message);

			if (!IsMocked(_serviceBus))
			{
				_serviceBus.Send(message);
			}
		}
	}

	public class SendToEndpointAction<TCurrentMessage, TMessageToSend> : BaseStateMachineAction
		where TCurrentMessage : ISagaMessage
	{
		private readonly IServiceBus _serviceBus;
		private readonly string _endpoint;
		private readonly TCurrentMessage _currentMessage;
		private readonly Action<TCurrentMessage, TMessageToSend> _send;

		public SendToEndpointAction(IServiceBus serviceBus,
			string endpoint,
			TCurrentMessage currentMessage,
			Action<TCurrentMessage, TMessageToSend> send)
		{
			_serviceBus = serviceBus;
			_endpoint = endpoint;
			_currentMessage = currentMessage;
			_send = send;
		}

		public override void Execute()
		{
			var message = CreateMessage<TMessageToSend>(_serviceBus);
			_send(_currentMessage, message);

			if (!IsMocked(_serviceBus))
			{
				_serviceBus.Send(_endpoint.ToUri(), message);
			}
		}
	}

	public class ReplyAction<TCurrentMessage, TMessageToReplyWith> : BaseStateMachineAction
		where TCurrentMessage : ISagaMessage
	{
		private readonly IServiceBus _serviceBus;
		private readonly TCurrentMessage _currentMessage;
		private readonly Action<TCurrentMessage, TMessageToReplyWith> _reply;

		public ReplyAction(IServiceBus serviceBus,
			TCurrentMessage currentMessage,
			Action<TCurrentMessage, TMessageToReplyWith> reply)
		{
			_serviceBus = serviceBus;
			_currentMessage = currentMessage;
			_reply = reply;
		}

		public override void Execute()
		{
			var message = CreateMessage<TMessageToReplyWith>(_serviceBus);
			_reply(_currentMessage, message);

			if (!IsMocked(_serviceBus))
			{
				_serviceBus.Reply(message);
			}
		}
	}

	public class DelayAction<TCurrentMessage, TMessageToDefer> : BaseStateMachineAction
		where TCurrentMessage : ISagaMessage
	{
		private readonly IServiceBus _serviceBus;
		private readonly TCurrentMessage _currentMessage;
		private readonly TimeSpan _interval;
		private readonly Action<TCurrentMessage, TMessageToDefer> _delay;

		public DelayAction(IServiceBus serviceBus,
			TCurrentMessage currentMessage,
			TimeSpan interval, 
			Action<TCurrentMessage, TMessageToDefer> delay)
		{
			_serviceBus = serviceBus;
			_currentMessage = currentMessage;
			_interval = interval;
			_delay = delay;
		}

		public override void Execute()
		{
			var message = CreateMessage<TMessageToDefer>(_serviceBus);
			_delay(_currentMessage, message);

			if (!IsMocked(_serviceBus))
			{
				_serviceBus.HandleMessageLater(_interval, message as IMessage);
			}
		}
	}
}