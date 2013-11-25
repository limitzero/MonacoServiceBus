using System.Collections.Generic;
using Monaco.Modelling.BusinessModel;
using Monaco.Modelling.BusinessModel.Elements;

namespace Monaco.Modelling.Realization
{
	public interface IRealizer
	{
		string Realize(IBusinessProcessModel processModel,
		               Capability capability = null,
		               IEnumerable<BusinessServiceDefinition> definitions = null);
	}
}