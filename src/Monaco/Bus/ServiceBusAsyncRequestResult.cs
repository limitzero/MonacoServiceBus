using System;
using System.Threading;
using Monaco.Bus.Internals;
using Monaco.Bus.Internals.Threading;
using Monaco.Bus.MessageManagement.Callbacks;
using Monaco.Bus.Repositories;

namespace Monaco.Bus
{
	public class ServiceBusAsyncRequestResult : AsyncResult, IServiceAsyncRequest
	{
		private readonly IServiceBus _bus;
		private ICallback _callback;
		private TimeSpan? _timeout;
		private IMessage _timeoutMessage;
		private IDisposableAction _token;
		private Action _userCallbackAction;

		public ServiceBusAsyncRequestResult(IServiceBus bus)
		{
			_bus = bus;
		}

		#region IServiceAsyncRequest Members

		public IMessage Request { get; private set; }

		public object Response { get; private set; }

		public IServiceAsyncRequest For(object service)
		{
			_token = _bus.AddInstanceConsumer(service);
			return this;
		}

		public IServiceAsyncRequest For<TService>() where TService : class, IConsumer
		{
			_token = _bus.AddInstanceConsumer<TService>();
			return this;
		}

		public IServiceAsyncRequest WithCallback(AsyncCallback callback, object state)
		{
			InitializeCallbackAndState(callback, state);
			return this;
		}

		public IServiceAsyncRequest WithCallback(Action callback)
		{
			_userCallbackAction = callback;
			return this;
		}

		public IServiceAsyncRequest WithTimeout(TimeSpan timeout, IMessage timeoutMessage)
		{
			_timeout = timeout;
			_timeoutMessage = timeoutMessage;
			return this;
		}

		public IServiceAsyncRequest WithTimeout<TMessage>(TimeSpan timeout, Action<TMessage> action)
			where TMessage : IMessage
		{
			TMessage message = default(TMessage);

			if (typeof (TMessage).IsInterface)
			{
				message = _bus.CreateMessage<TMessage>();
			}
			else
			{
				message = _bus.Find<TMessage>();
			}

			action(message);

			return WithTimeout(timeout, message);
		}

		public void Send(IMessage message, TimeSpan? timeout = null)
		{
			Request = message;

			// create the callback to hold the response:
			_callback = _bus.Find<ICallback>();
			_callback.AsyncRequest = this;
			_callback.Start(Request);

			// enqueue the message to be delivered:
			ThreadPool.QueueUserWorkItem(Callback, this);

			if (_timeout != null)
			{
				AsyncWaitHandle.WaitOne(_timeout.Value);
				Complete();
			}
			else
			{
				// wait for the "Complete" method to be fired:
				AsyncWaitHandle.WaitOne();
			}
		}

		public void Complete()
		{
			if (IsCompleted) return;

			if (_token != null)
				_token.Dispose();

			if (_timeoutMessage != null)
			{
				_bus.Publish(_timeoutMessage);
			}

			if (_userCallbackAction != null)
			{
				try
				{
					_userCallbackAction();
				}
				catch
				{
				}
			}

			// remove the callback reference from the repository:
			var repository = _bus.Find<ICallBackRepository>();
			repository.UnRegister(_callback);

			OnCompleted(this);
		}

		public void Complete(object message)
		{
			if (IsCompleted == false)
			{
				Response = message;
				Complete();
			}
		}

		public TMessage GetReply<TMessage>() where TMessage : IMessage
		{
			TMessage response = default(TMessage);

			if (Response == null) return response;

			if (IsCompleted)
			{
				try
				{
					response = (TMessage) Response;
				}
				catch
				{
				}
			}

			return response;
		}

		#endregion

		~ServiceBusAsyncRequestResult()
		{
			if (_token != null)
			{
				_token.Dispose();
			}
			_token = null;

			_callback = null;
			_userCallbackAction = null;
		}

		private void Callback(object asyncResult)
		{
			if (IsCompleted) return;
			_bus.Send(Request);
		}
	}
}