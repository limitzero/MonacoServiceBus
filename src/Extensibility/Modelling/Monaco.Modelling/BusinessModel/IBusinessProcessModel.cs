using System.Collections.Generic;
using Monaco.Modelling.BusinessModel.Elements;

namespace Monaco.Modelling.BusinessModel
{
	public interface IBusinessProcessModel
	{
		string Name { get; set; }
		AbstractBusinessProcessModelLibrary GetLibrary();
		IDictionary<Capability, List<BusinessServiceDefinition>> CapabilityServiceDefinitions { get; set; }
		string Description { get; set; }

		void Define();
		void For(Capability capability,params BusinessServiceDefinition[] definitions);
		BusinessServiceDefinition When(Message message);
		BusinessServiceDefinition Then(Message message);
	}
}