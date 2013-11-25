using System;
using System.Collections.Generic;
using Monaco.Modelling.BusinessModel;
using Monaco.Modelling.BusinessModel.Elements;

namespace Monaco.Modelling
{
	// idea came from : http://www.ibm.com/developerworks/rational/library/09/modelingwithsoaml-1/

	/// <summary>
	/// Abstract class used to build the business process model from the defined capability model.
	/// </summary>
	/// <typeparam name="TModelLibrary">Library holding re-usable elements</typeparam>
	/// <typeparam name="TCapabilityModel">Current capability model that holds all of the higher-level business functions that are to be achieved.</typeparam>
	[Serializable]
	public abstract class AbstractBusinessProcessModel<TCapabilityModel, TModelLibrary> :
		IBusinessProcessModel,
		IModel<TModelLibrary>
		where TModelLibrary : AbstractBusinessProcessModelLibrary, new()
		where TCapabilityModel : AbstractBusinessCapabilityModel<TModelLibrary>, new()
	{
		public TModelLibrary ModelLibrary { get; private set; }

		public IDictionary<Capability, List<BusinessServiceDefinition>> CapabilityServiceDefinitions { get; set; }

		public string Name { get; set; }

		public string Description { get; set; }

		public List<BusinessServiceDefinition> ServiceDefinitions { get;  set; }

		protected AbstractBusinessProcessModel()
		{
			this.CapabilityServiceDefinitions = new Dictionary<Capability, List<BusinessServiceDefinition>>();
			PrepareBusinessProcessLibraryFromCapabilityModel();
		}

		/// <summary>
		/// This is the point on the business process model where the processes, actors and other entities are configured
		/// </summary>
		public abstract void Define();

		/// <summary>
		/// This will assign a certain set of activities and tasks for a 
		/// specific <seealso cref="Capability">capability</seealso> in the business process
		/// whereby an <seealso cref="Actor">actor</seealso> is responsible.
		/// </summary>
		/// <param name="capability">The capability that is to be realized via a series of process definitions</param>
		/// <param name="definitions">The listing of process definitions to accompany the capability</param>
		/// <returns></returns>
		public void For(Capability capability,
		                params BusinessServiceDefinition[] definitions)
		{
			var serviceDefinitions = new List<BusinessServiceDefinition>(definitions);
			serviceDefinitions.ForEach( x => x.Capability = capability);
			this.CapabilityServiceDefinitions.Add(capability, serviceDefinitions);
		}

		/// <summary>
		/// This defines the starting point of the business process that carried out by 
		/// a specific <seealso cref="Actor">actor</seealso> for the <seealso cref="Capability">capability</seealso>.
		/// </summary>
		/// <param name="message">The <seealso cref="Message">message</seealso>that is to start the sequence of processing steps</param>
		public BusinessServiceDefinition When(Message message)
		{
			var definition = new BusinessServiceDefinition(message, BusinessServiceProcessStage.Start);
			return definition;
		}

		/// <summary>
		/// This defines the subsequent point of the business process that carried out by 
		/// a specific <seealso cref="Actor">actor</seealso> for the <seealso cref="Capability">capability</seealso>.
		/// </summary>
		/// <param name="message">The <seealso cref="Message">message</seealso>that is to start the sequence of processing steps</param>
		public BusinessServiceDefinition Then(Message message)
		{
			var definition = new BusinessServiceDefinition(message, BusinessServiceProcessStage.Next);
			return definition;
		}

		public AbstractBusinessProcessModelLibrary GetLibrary()
		{
			return this.ModelLibrary;
		}

		private void PrepareBusinessProcessLibraryFromCapabilityModel()
		{
			var capabiltyModel = new TCapabilityModel();
			capabiltyModel.Define();

			this.ModelLibrary = capabiltyModel.ModelLibrary;
		}
	}
}