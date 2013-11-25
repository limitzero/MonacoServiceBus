namespace Monaco.Modelling.Tests.Model
{
	// lastly, create the process model that says what capability will be realized 
	// along with its corresponding process model elements.
	public class AcmeBusinessProcessModel :
		AbstractBusinessProcessModel<AcmeBusinessCapabilityModel, AcmeBusinessProcessModelLibrary>
	{
		public override void Define()
		{
			For(ModelLibrary.ShippingAnOrder,

				When(ModelLibrary.ShipmentReceived)
					.ExecuteTask(ModelLibrary.RequestShipping)
					.WaitForActivity(ModelLibrary.ProcessSchedule,
						ModelLibrary.ScheduledProcessed),

			    Then(ModelLibrary.PriceCalculationFailed)
					.ExecuteTaskAndReturnMessage(ModelLibrary.CancelShipment,
						ModelLibrary.ShipmentCancelled)
			    	.ThenComplete()

					);

			For(ModelLibrary.Invoicing,
					When(ModelLibrary.ShipmentReceived)
					.ExecuteTaskAndReturnMessage(ModelLibrary.InitiatePriceCalculation,
					   null, ModelLibrary.PriceCalculationFailed)
					.WaitForActivity(ModelLibrary.ProcessSchedule,
					  ModelLibrary.ScheduledProcessed)
					.ExecuteTask(ModelLibrary.CompletePriceCalculation)	
					.ThenComplete()
				);


			For(ModelLibrary.Scheduling,

				When(ModelLibrary.ShipmentReceived)
					.ExecuteTask(ModelLibrary.RequestProductionScheduling)
					.ExecuteTask(ModelLibrary.HandleShippingPrice)
					.WaitForActivity(ModelLibrary.ProcessInvoice,
							ModelLibrary.InvoiceProcessed),

				Then(ModelLibrary.PriceCalculationFailed)
						.ExecuteTask(ModelLibrary.HaltProductionSchedule)
						.ThenComplete()
				);

		}
	}
}