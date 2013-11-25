using System.Collections.Generic;
using Monaco.Modelling.BusinessModel.Elements;
using Monaco.Modelling.Realization;
using Monaco.Modelling.Tests.Model;
using Xunit;

namespace Monaco.Modelling.Tests
{
	public class ServiceMessageTests : BaseCaseTest
	{
		[Fact]
		public void can_realize_concrete_instances_of_business_process_messages()
		{
			var realizer = new ServiceModelMessageRealizer();
			var model = new AcmeBusinessProcessModel();
			model.Define();

			var actual = realizer.Realize(model, null, null);
			actual = realizer.GetConcreteModel();

			Assert.Equal(this.Expected(System.Reflection.MethodInfo.GetCurrentMethod().Name), actual);
		}

		[Fact]
		public void can_create_class_representing_service_model_saga_state_data()
		{
			var realizer = new ServiceModelSagaDataRealizer();
			var model = new AcmeBusinessProcessModel();
			model.Define();

			var actual = realizer.Realize(model, null, null);
			actual = realizer.GetConcreteModel();

			Assert.Equal(this.Expected(System.Reflection.MethodInfo.GetCurrentMethod().Name), actual.Trim());
		}

		[Fact]
		public void can_create_class_representing_service_model_for_capability()
		{
			var realizer = new ServiceModelStateMachineRealizer();
			var model = new AcmeBusinessProcessModel();
			model.Define();

			var capablities = new List<Capability>(model.CapabilityServiceDefinitions.Keys);
			var capability = capablities[0];

			var definitions = model.CapabilityServiceDefinitions[capability];

			var results = realizer.Realize(model, capability, definitions);

			results = realizer.GetConcreteModel();

			System.Diagnostics.Debug.WriteLine(results);
		}
	}
}