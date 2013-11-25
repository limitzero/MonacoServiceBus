using System.Collections.Generic;
using Monaco.Modelling.BusinessModel.Capabilities;
using Monaco.Modelling.BusinessModel.Elements;

namespace Monaco.Modelling
{
	/// <summary>
	/// Abstract class that holds the model definition of the business capabilities.
	/// </summary>
	/// <typeparam name="TModelLibrary"></typeparam>
	public abstract class AbstractBusinessCapabilityModel<TModelLibrary>
		where TModelLibrary : AbstractBusinessProcessModelLibrary, new()
	{
		/// <summary>
		/// Gets the underlying model library elements for use in capability model.
		/// </summary>
		public TModelLibrary ModelLibrary { get; private set; }

		public IList<BusinessCapabilityDefinition> CapabilityDefinitions { get; private set; }

		protected AbstractBusinessCapabilityModel()
		{
			this.ModelLibrary = new TModelLibrary();
			this.CapabilityDefinitions = new List<BusinessCapabilityDefinition>();
		}

		public abstract void Define();

		/// <summary>
		/// This will initiate the configuration of the capability definition.
		/// </summary>
		/// <param name="definition"></param>
		protected void Configure(BusinessCapabilityDefinition definition)
		{
			this.CapabilityDefinitions.Add(definition);
		}

		/// <summary>
		/// This will return the definition of a business capability.
		/// </summary>
		/// <param name="capability">Current capability being described/modeled</param>
		/// <returns></returns>
		protected BusinessCapabilityDefinition For(Capability capability)
		{
			return new BusinessCapabilityDefinition(capability);
		}
	}
}