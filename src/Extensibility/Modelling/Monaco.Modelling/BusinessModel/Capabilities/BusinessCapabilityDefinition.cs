using Monaco.Modelling.BusinessModel.Elements;

namespace Monaco.Modelling.BusinessModel.Capabilities
{
	public class BusinessCapabilityDefinition
	{
		public Capability Capability { get; private set; }

		public BusinessCapabilityDefinition(Capability capability)
		{
			this.Capability = capability;
		}

		/// <summary>
		/// This will define the actors that are involved in contributing to the current business capability.
		/// </summary>
		/// <param name="actors"></param>
		/// <returns></returns>
		public BusinessCapabilityDefinition WithActors(params Actor[] actors)
		{
			foreach (var actor in actors)
			{
				this.Capability.AssignRole(actor);	
			}

			return this;
		}

		/// <summary>
		/// This will define the high level costs that are associated with the current business capability (if known).
		/// </summary>
		/// <param name="costs"></param>
		/// <returns></returns>
		public BusinessCapabilityDefinition WithCosts(params Cost[] costs)
		{
			this.Capability.AssignCosts(costs);
			return this;
		}

		/// <summary>
		/// This will define the high level messages that are needed for the capability to accept for the function to be realized.
		/// </summary>
		/// <param name="messages"></param>
		/// <returns></returns>
		public BusinessCapabilityDefinition WithInputMessages(params Message[] messages)
		{
			this.Capability.AssignInputMessages(messages);
			return this;
		}

		/// <summary>
		/// This will define the high level messages that are generated for the capability when the function to be realized.
		/// </summary>
		/// <param name="messages"></param>
		/// <returns></returns>
		public BusinessCapabilityDefinition WithOutputMessages(params Message[] messages)
		{
			this.Capability.AssignOutputMessages(messages);
			return this;
		}

		/// <summary>
		/// This will define the high level exception messages that are generated for the capability when the function to be realized.
		/// </summary>
		/// <param name="messages"></param>
		/// <returns></returns>
		public BusinessCapabilityDefinition WithExceptionMessages(params Message[] messages)
		{
			this.Capability.AssignExceptionMessages(messages);
			return this;
		}

		public BusinessCapabilityDefinition WithPerformanceScoreOf(CapabilityScore score)
		{
			this.Capability.AssignPerformanceScoreOf(score);
			return this;
		}

		public BusinessCapabilityDefinition WithBusinessValueScoreOf(CapabilityScore score)
		{
			this.Capability.AssignBusinessValueScoreOf(score);
			return this;
		}

		public BusinessCapabilityDefinition DependsOn(params Capability[] capabilities)
		{
			this.Capability.AssignDependencies(capabilities);
			return this;
		}
	}
}