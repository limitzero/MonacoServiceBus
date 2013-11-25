using System;
using System.Collections.Generic;
using System.Reflection;
using Monaco.Bus.MessageManagement.Dispatcher.Internal;

namespace Monaco
{
	/// <summary>
	/// Abstract class that encompasses a set of actions for consuming a message.
	/// </summary>
	public abstract class MessageConsumer
	{
		protected MessageConsumer()
		{
			Conditions = new Dictionary<Type, List<Action>>();
		}

		/// <summary>
		/// Gets or sets the current message that the consumer is processing.
		/// </summary>
		public object CurrentMessage { get; set; }

		/// <summary>
		/// Gets the set of conditions to execute for the particular message.
		/// </summary>
		public IDictionary<Type, List<Action>> Conditions { get; private set; }

		/// <summary>
		/// Gets or sets the current instance of the <seealso cref="IServiceBus"/> for the message consumer instance.
		/// </summary>
		public IServiceBus Bus { get; set; }

		/// <summary>
		/// This is the marker for configuring the message consumer actions for handling messages.
		/// </summary>
		public abstract void Define();

		public void Reset()
		{
			Conditions.Clear();
		}

		/// <summary>
		/// This is the condition that happens for a message arriving to the message consumer for processing.
		/// All subsequent actions that are to be taken can be defined in the anonymous delegate.
		/// </summary>
		/// <typeparam name="TMessage">Message that triggered the message processing event</typeparam>
		public void UponReceiving<TMessage>(Action<TMessage> action)
			where TMessage : IMessage
		{
			Action consumeAction = () =>
			                       	{
			                       		// execute the 'Consume' action defined by the interface:
			                       		ConsumeMessage(this, CurrentMessage);

			                       		// execute the user defined action for message on DSL:
			                       		action((TMessage) CurrentMessage);
			                       	};

			var actions = new List<Action>();

			actions.Add(consumeAction);

			try
			{
				Conditions.Add(typeof (TMessage), actions);
			}
			catch
			{
			}
		}

		private void ConsumeMessage(object component, object message)
		{
			MethodInfo consumerMethod =
				new MessageToMethodMapper().Map(component, message);

			if (consumerMethod != null)
			{
				new MessageMethodInvoker().Invoke(component, consumerMethod, message);
			}
		}
	}

	/// <summary>
	/// Base class that allows message consumption with non-persistant state
	/// that can be managed across one service bus instance.
	/// </summary>
	/// <typeparam name="TData">Data to be preserved in-memory across calls</typeparam>
	public abstract class MessageConsumer<TData> :
		MessageConsumer, IDisposable where TData : class, new()
	{
		private static TData _data;

		protected MessageConsumer()
		{
			if (Data == null)
			{
				Data = new TData();
			}
		}

		protected bool IsDisposing { get; private set; }

		protected TData Data
		{
			get { return _data; }
			set { _data = value; }
		}

		#region IDisposable Members

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

		private void Dispose(bool disposing)
		{
			IsDisposing = disposing;

			if (disposing)
			{
				Data = null;
			}
		}

		~MessageConsumer()
		{
			Dispose(true);
		}
	}
}