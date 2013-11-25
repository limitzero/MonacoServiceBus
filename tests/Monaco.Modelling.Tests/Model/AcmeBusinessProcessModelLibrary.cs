using Monaco.Modelling.BusinessModel.Elements;

namespace Monaco.Modelling.Tests.Model
{
	// first, defince the process model library that will hold all of your assests 
	// that can participate in the business process.
	public class AcmeBusinessProcessModelLibrary : AbstractBusinessProcessModelLibrary
	{
		#region -- capabilities --
		public Capability ShippingAnOrder { get; set; }
		public Capability Invoicing { get; set; }
		public Capability Scheduling { get; set; }
		#endregion 

		#region -- actors --
		public Actor Shipper { get; set; }
		public Actor Invoicer { get; set; }
		public Actor Scheduler { get; set; }
		#endregion 

		#region -- tasks --
		public Task RequestShipping { get; set; }
		public Task InitiatePriceCalculation { get; set; }
		public Task CompletePriceCalculation { get; set; }
		public Task RequestProductionScheduling { get; set; }
		public Task HandleShippingPrice { get; set; }
		public Task HaltProductionSchedule { get; set; }
		public Task CancelShipment { get; set; }
		#endregion 

		#region -- activities --
		public Activity ProcessSchedule { get; set; }
		public Activity ProcessInvoice { get; set; }
		#endregion 

		#region -- messages --
		public Message ShipmentReceived { get; set; }
		public Message ScheduledProcessed { get; set; }
		public Message InvoiceProcessed { get; set; }
		public Message PriceCalculationFailed { get; set; }
		public Message ShipmentCancelled { get; set; }
		#endregion

		public override void Define()
		{
			ShipmentReceived.Description = "A sample description for the shipment received message.";

			Invoicing.Description =
				"The Accept Order capability describes the organization's ability " +
				"to process vetted orders from a third-party and apply current criteria " +
				"to determine its risk to the business.";
		}
	}
}