using System.Collections.Generic;

namespace Monaco.Bus.Internals.Agent
{
	/// <summary>
	/// Contract for registry to holding all of the background services that the infrastructure will use.
	/// </summary>
	public interface IAgentRegistry
	{
		/// <summary>
		/// Gets the collection of background agents for the emssage bus instance.
		/// </summary>
		ICollection<BaseAgent> Agents { get; }

		/// <summary>
		/// This will register a background agent for the message bus instance.
		/// </summary>
		/// <param name="agent">Agent to regiser</param>
		void Register(BaseAgent agent);

		/// <summary>
		/// This will register a set of background agents for the message bus instance.
		/// </summary>
		/// <param name="agents">Agents to regiser</param>
		void Register(params BaseAgent[] agents);

		/// <summary>
		/// This will unregister a background agent for the message bus instance.
		/// </summary>
		/// <typeparam name="TAGENT"></typeparam>
		void Unregister<TAGENT>() where TAGENT : BaseAgent;
	}
}