using System;
using System.Collections.Generic;
using Monaco.Bus.Internals.Collections;

namespace Monaco.Bus.Internals.Agent.Impl
{
	public class AgentRegistry : IAgentRegistry, IDisposable
	{
		private static readonly object _registry_lock = new object();
		private bool _disposing;
		private IThreadSafeList<BaseAgent> _registry;

		public AgentRegistry()
		{
			if (_registry == null)
			{
				_registry = new ThreadSafeList<BaseAgent>();
			}
		}

		#region IAgentRegistry Members

		public ICollection<BaseAgent> Agents
		{
			get
			{
				if (_disposing) return null;

				lock (_registry_lock)
				{
					return _registry.Instance;
				}
			}
		}

		public void Register(BaseAgent agent)
		{
			if (_disposing) return;

			lock (_registry_lock)
			{
				if (!_registry.Contains(agent))
				{
					_registry.Add(agent);
				}
			}
		}

		public void Register(params BaseAgent[] agents)
		{
			if (_disposing) return;

			lock (_registry_lock)
			{
				foreach (BaseAgent agent in agents)
				{
					Register(agent);
				}
			}
		}

		public void Unregister<TAGENT>() where TAGENT : BaseAgent
		{
			lock (_registry_lock)
			{
				int index = 0;
				foreach (BaseAgent agent in _registry)
				{
					if (typeof (TAGENT) != agent.GetType())
					{
						index++;
						continue;
					}
					break;
				}

				if (index <= _registry.Count)
				{
					_registry.RemoveAt(index);
				}
			}
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			_disposing = true;

			if (_registry != null)
			{
				_registry.Clear();
				_registry = null;
			}
		}

		#endregion
	}
}