using System;
using System.Collections.Generic;
using System.Linq;
using Monaco.Modelling.BusinessModel.Elements;

namespace Monaco.Modelling.BusinessModel.Internals
{
	// idea came from : http://www.ibm.com/developerworks/rational/library/09/modelingwithsoaml-1/

	/// <summary>
	/// Abstract class that models at a brief level a business process.
	/// </summary>
	public abstract class BusinessProcessModel
	{
		public string Name { get; set; }
		public string Description { get; set; }
		public ICollection<BusinessProcessModelTriggerCondition> Conditions { get; private set; }

		protected BusinessProcessModel()
		{
			this.Conditions = new List<BusinessProcessModelTriggerCondition>();

			this.PrepareModelElement<Capability>();
			this.PrepareModelElement<Actor>();
			this.PrepareModelElement<Activity>();
			this.PrepareModelElement<Message>();
			this.PrepareModelElement<Task>();
		}

		/// <summary>
		/// This will create a logical responsibility of an overall business 
		/// function to the group or individual(s) responsible for carrying out 
		/// the associated actions to complete it.
		/// </summary>
		/// <param name="capability">Business function ando/or service to be realized</param>
		/// <param name="actor">Business group or individual responsible for execution of said service/function.</param>
		protected void MapCapabilityToActor(Capability capability, Actor actor)
		{
			capability.AssignRole(actor);
		}

		/// <summary>
		/// This is the point on the business process model where the processes, actors and other entities are configured
		/// </summary>
		public abstract void Configure();

		/// <summary>
		/// This defines the starting point of the business process that carried out by a specific <seealso cref="Actor">actor</seealso>
		/// </summary>
		/// <param name="businessProcessModelTriggerCondition"></param>
		public void Initially(BusinessProcessModelTriggerCondition businessProcessModelTriggerCondition)
		{
			var initialCondition = (from condition in this.Conditions
			                        where condition.Stage == BusinessProcessModelStages.Initially
			                        select condition).FirstOrDefault();

			if(initialCondition != null)
				throw new Exception("There has already an initial condition defined for the process. " + 
					"Please configure the process to have only one initial condition.");

			businessProcessModelTriggerCondition.Stage = BusinessProcessModelStages.Initially;
			this.Conditions.Add(businessProcessModelTriggerCondition);
		}

		/// <summary>
		/// This defines the subsequent actions for the business process that is carried out by a specific <seealso cref="Actor">actor</seealso>
		/// </summary>
		/// <param name="businessProcessModelTriggerCondition"></param>
		public void Also(BusinessProcessModelTriggerCondition businessProcessModelTriggerCondition)
		{
			businessProcessModelTriggerCondition.Stage = BusinessProcessModelStages.Also;
			this.Conditions.Add(businessProcessModelTriggerCondition);
		}
		
		/// <summary>
		/// This will assign a certain set of activities and tasks for a specific <seealso cref="Capability">capability</seealso> in the business process
		/// whereby an <seealso cref="Actor">actor</seealso> is responsible.
		/// </summary>
		/// <param name="capability"></param>
		/// <returns></returns>
		public BusinessProcessModelTriggerCondition For(Capability capability)
		{
			var bpm = new BusinessProcessModelTriggerCondition(capability);
			return bpm;
		}

		private void PrepareModelElement<TElement>() where TElement : IModelElement, new()
		{
			var properties = (from property in this.GetType().GetProperties()
			                  where property.PropertyType == typeof (TElement)
			                  select property).Distinct().ToList();

			foreach (var property in properties)
			{
				var element = new TElement();
				element.Name = property.Name;
				property.SetValue(this, element, null);
			}
		}

	}
}