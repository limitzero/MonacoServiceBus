using System.Collections.Generic;
using Monaco.Modelling.BusinessModel.Elements;

namespace Monaco.Modelling.BusinessModel.Capabilities
{
	public abstract class BusinessCapability : IModelElement
	{
		public string Name { get; set; }
		public string Description { get; set; }

		public CapabilityScore OverallScore { get; private set; }
		public CapabilityScore BusinessValue { get; private set; }
		public CapabilityScore Performance { get; private set; }

		public IList<Actor> Actors { get; private set; }
		public IList<Message> Messages { get; private set; }
		public IList<Cost> Costs { get; private set; }
		public IList<ServiceLevelExpectation> ServiceLevelExpectations { get; private set; }
		public IList<BusinessCapability> DependentCapabilities { get; private set; }

		public void EnlistActor(Actor actor)
		{
			this.Actors.Add(actor);
		}

		public void RequiresMessage(Message message)
		{
			this.Messages.Add(message);
		}

		public void HasCost(Cost cost)
		{
			this.Costs.Add(cost);
		}

		public void HasServiceLevelExpectationOf(ServiceLevelExpectation sle)
		{
			this.ServiceLevelExpectations.Add(sle);
		}

		public void HasBusinessValueScore(CapabilityScore score)
		{
			this.BusinessValue = score;
		}

		public void HasPerformanceScore(CapabilityScore score)
		{
			this.Performance = score;
		}

		public void HasDependencyOf(BusinessCapability capability)
		{
			this.DependentCapabilities.Add(capability);
		}
	}
}