using System;
using Monaco.Bus.Internals.Eventing;

namespace Monaco.Bus.Internals.Agent.Impl
{
	public class AgentManager : IAgentManager
	{
		private readonly IAgentRegistry _registry;

		public AgentManager(IAgentRegistry registry)
		{
			_registry = registry;
		}

		#region IAgentManager Members

		public event EventHandler<ComponentStartedEventArgs> ComponentStartedEvent;
		public event EventHandler<ComponentStoppedEventArgs> ComponentStoppedEvent;
		public event EventHandler<ComponentErrorEventArgs> ComponentErrorEvent;

		public void Dispose()
		{
			Stop();
		}

		public bool IsRunning { get; private set; }

		public void Start()
		{
			if (IsRunning)
			{
				return;
			}

			if (_registry != null)
				if (_registry.Agents.Count > 0)
					foreach (BaseAgent agent in _registry.Agents)
					{
						agent.ComponentErrorEvent += ServiceError;
						agent.ComponentStartedEvent += ServiceStarted;
						agent.ComponentStoppedEvent += ServiceStopped;
						agent.Start();
					}

			IsRunning = true;
			OnServiceStarted(GetType().Name);
		}

		public void Stop()
		{
			if (_registry != null)
				if (_registry.Agents.Count > 0)
					foreach (BaseAgent agent in _registry.Agents)
					{
						agent.Stop();
						agent.ComponentErrorEvent -= ServiceError;
						agent.ComponentStartedEvent -= ServiceStarted;
						agent.ComponentStoppedEvent -= ServiceStopped;
					}

			IsRunning = false;
			OnServiceStopped(GetType().Name);
		}

		#endregion

		private void ServiceStopped(object sender, ComponentStoppedEventArgs e)
		{
			OnServiceStopped(e.ComponentName);
		}

		private void ServiceStarted(object sender, ComponentStartedEventArgs e)
		{
			OnServiceStarted(e.ComponentName);
		}

		private void ServiceError(object sender, ComponentErrorEventArgs e)
		{
			string msg = string.Format("Service Error: Message = {0}, Stack Trace = {1}",
			                           e.ErrorMessage, e.Exception.StackTrace);
			OnServiceError(msg);
		}

		private void OnServiceStarted(string serviceName)
		{
			EventHandler<ComponentStartedEventArgs> evt = ComponentStartedEvent;
			if (evt != null)
				evt(this, new ComponentStartedEventArgs(serviceName));
		}

		private void OnServiceStopped(string serviceName)
		{
			EventHandler<ComponentStoppedEventArgs> evt = ComponentStoppedEvent;
			if (evt != null)
				evt(this, new ComponentStoppedEventArgs(serviceName));
		}

		private bool OnServiceError(string theErrorMessage)
		{
			EventHandler<ComponentErrorEventArgs> evt = ComponentErrorEvent;
			bool isHandlerAttached = (evt != null);

			if (isHandlerAttached)
				evt(this, new ComponentErrorEventArgs(theErrorMessage));

			return isHandlerAttached;
		}

		private bool OnServiceError(Exception theException)
		{
			EventHandler<ComponentErrorEventArgs> evt = ComponentErrorEvent;
			bool isHandlerAttached = (evt != null);

			if (isHandlerAttached)
				evt(this, new ComponentErrorEventArgs(theException));

			return isHandlerAttached;
		}
	}
}