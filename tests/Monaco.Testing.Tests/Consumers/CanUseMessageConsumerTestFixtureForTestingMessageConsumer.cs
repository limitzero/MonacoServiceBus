using System;
using Monaco.Testing.MessageConsumers;
using Xunit;

namespace Monaco.Testing.Tests.Consumers
{
	public class CanUseMessageConsumerTestFixtureForTestingMessageConsumer
		: MessageConsumerTestContext<OrderProcessingMessageConsumer>
	{
		[Fact]
		public void can_send_invoice_when_new_shipment_has_arrived()
		{
			// this is a bad test :( will go with it for now...take a look at the way 
			// the Send is done in the message consumer...yuck !!!
			Verify(
				When<NewShipment>()
					.ExpectToSend<PrepareInvoice>());
		}
	}

	public class PrepareInvoice : IMessage { }

	public class NewShipment : IMessage
	{
		public int CustomerId { get; set; }
	}

	// this instance data is stored within the container 
	// and is not thread-safe be careful doing this as 
	// the data will be a static instance!!! Calls to the Dispose()
	// method wil erase the running data object contents.
	public class OrderProcessingMessageConsumerData
	{
		public int CustomerId { get; set; }
	}

	public class OrderProcessingMessageConsumer :
		MessageConsumer<OrderProcessingMessageConsumerData>,
		Consumes<NewShipment>
	{
		private static readonly object data_lock = new object();

		public override void Define()
		{
			UponReceiving<NewShipment>(message =>
										{
											this.AccessConsumerData((data)=>
											                	{
																	data.CustomerId = message.CustomerId;
											                	});
											
											this.Bus.Send<PrepareInvoice>(m => { });
										});
		}

		public void Consume(NewShipment message)
		{
		}

		private void AccessConsumerData(Action<OrderProcessingMessageConsumerData> accessAction)
		{
			// semi-thread safe mutation of local data...
			lock(data_lock)
			{
				accessAction(this.Data);
			}
		}
	}
}