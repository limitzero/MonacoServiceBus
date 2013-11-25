using Monaco.Modelling.BusinessModel.Elements;

namespace Monaco.Modelling.Tests.Model
{
	// second, define the capability (feature that consitutes a business action)
	// and its collaborators.
	public class AcmeBusinessCapabilityModel : 
		AbstractBusinessCapabilityModel<AcmeBusinessProcessModelLibrary>
	{
		public override void Define()
		{
			this.DefineShippingCapability();
			this.DefineInvoicingCapability();
			this.DefineSchedulingCapability();
		}

		private void DefineShippingCapability()
		{
			For(ModelLibrary.ShippingAnOrder)
				.WithActors(ModelLibrary.Shipper)
				.WithInputMessages(ModelLibrary.ShipmentReceived)
				.WithPerformanceScoreOf(CapabilityScore.Medium)
				.WithBusinessValueScoreOf(CapabilityScore.High);
		}

		private void DefineInvoicingCapability()
		{
			For(ModelLibrary.Invoicing)
				.WithActors(ModelLibrary.Invoicer)
				.WithInputMessages(ModelLibrary.ShipmentReceived)
				.WithExceptionMessages(ModelLibrary.PriceCalculationFailed)
				.WithPerformanceScoreOf(CapabilityScore.Medium)
				.WithBusinessValueScoreOf(CapabilityScore.High);
		}

		private void DefineSchedulingCapability()
		{
			For(ModelLibrary.Scheduling)
				.WithActors(ModelLibrary.Scheduler)
				.WithInputMessages(ModelLibrary.ShipmentReceived)
				.WithPerformanceScoreOf(CapabilityScore.Medium)
				.WithBusinessValueScoreOf(CapabilityScore.High);
		}
	}
}