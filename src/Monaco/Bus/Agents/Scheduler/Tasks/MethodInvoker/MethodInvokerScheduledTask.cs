using System;
using System.Linq;
using System.Reflection;
using Monaco.Bus.Agents.Scheduler.EventArgs;
using Monaco.Bus.Internals.Eventing;

namespace Monaco.Bus.Agents.Scheduler.Tasks.MethodInvoker
{
	public class MethodInvokerScheduledTask : IMethodInvokerScheduledTask
	{
		#region IMethodInvokerScheduledTask Members
		 
		public event EventHandler<ScheduledTaskMessageCreatedEventArgs> ScheduledTaskMessageCreated;
		public event EventHandler<ComponentNotificationEventArgs> ComponentNotificationEvent;
		public event EventHandler<ComponentErrorEventArgs> ComponentErrorEvent;

		public bool IsExecuting { get; private set; }
		public object Instance { get; set; }
		public MethodInfo Method { get; set; }
		public string MethodName { get; set; }
		public object[] Messages { get; set; }
		public string Interval { get; set; }

		public void Execute()
		{
			try
			{
				if (Method == null && !string.IsNullOrEmpty(MethodName))
				{
					FindMethodForMethodName();
				}

				if (Method == null) return;

				var result = Method.Invoke(Instance, Messages) as IMessage;
				
				if (result != null)
					OnScheduledTaskExecuted(result);
			}
			catch (Exception exception)
			{
				if (!OnScheduledTaskError(Instance.GetType().Name, Method.Name, exception))
					throw;
			}
		}

		#endregion

		private void FindMethodForMethodName()
		{
			Method = null;

			try
			{
				Method = (from method in Instance.GetType().GetMethods()
				          where method.Name == MethodName
				          select method).FirstOrDefault();
			}
			catch
			{
			}
		}

		private bool OnScheduledTaskError(string instanceName, string methodName, Exception exception)
		{
			Exception theRootExeception = exception;

			while (exception != null)
			{
				theRootExeception = exception;
				exception = exception.InnerException;
			}

			string message = string.Format("Error : Component : {0}, AddInstanceSubscription: {1}, Message: {2}",
			                               instanceName, methodName, theRootExeception.Message);

			EventHandler<ComponentErrorEventArgs> evt = ComponentErrorEvent;

			bool isEventHandlerAttached = (evt != null);

			if (isEventHandlerAttached)
			{
				evt(this, new ComponentErrorEventArgs(message, theRootExeception));
			}

			return isEventHandlerAttached;
		}

		private void OnScheduledTaskExecuted(IMessage message)
		{
			EventHandler<ComponentNotificationEventArgs> evt = ComponentNotificationEvent;
			string notificationMessage =
				string.Format("Task '{0}' with method '{1}' executed producing message '{2}' for consumption on the message bus.",
				              Instance.GetType().FullName, Method.Name,
				              message.GetType().FullName);

			if (evt != null)
				evt(this, new ComponentNotificationEventArgs(notificationMessage));

			EventHandler<ScheduledTaskMessageCreatedEventArgs> createdEvent = ScheduledTaskMessageCreated;

			if (createdEvent != null)
			{
				createdEvent(this, new ScheduledTaskMessageCreatedEventArgs(this, message));
			}
		}
	}
}