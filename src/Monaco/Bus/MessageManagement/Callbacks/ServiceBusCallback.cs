using System;
using Monaco.Bus.Messages;
using Monaco.Bus.Repositories;
using Monaco.Bus.Services.Timeout.Messages.Commands;
using Monaco.Extensibility.Logging;

namespace Monaco.Bus.MessageManagement.Callbacks
{
	public class ServiceBusCallback : ICallback
	{
		private readonly IServiceBus _bus;
		private readonly ICallBackRepository _callBackRepository;
		private CancelTimeout _cancellationTimeoutMessage;

		/// <summary>
		/// Initializes an instance of a message bus callback for a message that is sent for a request/response scenario.
		/// </summary>
		/// <param name="bus"></param>
		/// <param name="callBackRepository"></param>
		public ServiceBusCallback(IServiceBus bus, ICallBackRepository callBackRepository)
		{
			_bus = bus;
			_callBackRepository = callBackRepository;
		}

		#region ICallback Members

		/// <summary>
		/// Gets or sets the request message that is sent to the 
		/// bus for a corresponding reply message.
		/// </summary>
		public object RequestMessage { get; private set; }

		public IServiceAsyncRequest AsyncRequest { get; set; }

		/// <summary>
		/// Gets the response message to send to client.
		/// </summary>
		public object ResponseMessage { get; private set; }

		/// <summary>
		/// Gets the callback function to execute on the client.
		/// </summary>
		public Action Callback { get; private set; }

		public void Start(object request)
		{
			RequestMessage = request;

			if (!typeof (IAdminMessage).IsAssignableFrom(RequestMessage.GetType()))
				_callBackRepository.Register(this);
		}

		public void Complete(object replyMessage)
		{
			ResponseMessage = replyMessage;

			if (_cancellationTimeoutMessage != null)
			{
				_bus.Send(_cancellationTimeoutMessage);
			}

			if (Callback != null)
			{
				_bus.Find<ILogger>().LogInfoMessage(string.Format(
					"Invoking call back action for request '{0}' for reply '{1}'.",
					RequestMessage.GetType().Name,
					replyMessage.GetType().Name));
				Callback();
			}

			if (AsyncRequest != null)
			{
				AsyncRequest.Complete(ResponseMessage);
			}
		}

		public ICallback Register(Action callback)
		{
			Callback = callback;

			string message = string.Format("Registering call back '{0}' for request '{1}'.",
			                               callback.Method.Name,
			                               RequestMessage.GetType().Name);

			_bus.Find<ILogger>().LogInfoMessage(message);

			return this;
		}

		public TReplyMessage GetReply<TReplyMessage>() where TReplyMessage : IMessage
		{
			TReplyMessage reply = default(TReplyMessage);

			try
			{
				reply = (TReplyMessage) ResponseMessage;
			}
			catch
			{
			}

			return reply;
		}

		public ICallback WithTimeout(ScheduleTimeout timeout)
		{
			_cancellationTimeoutMessage = timeout.CreateCancelMessage();
			return this;
		}

		#endregion
	}
}