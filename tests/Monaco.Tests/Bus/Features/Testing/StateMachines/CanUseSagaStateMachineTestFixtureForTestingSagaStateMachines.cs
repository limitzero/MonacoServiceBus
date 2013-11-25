using System;
using Monaco.StateMachine;
using Monaco.Testing.StateMachines;
using Xunit;

namespace Monaco.Tests.Bus.Features.Testing.StateMachines
{
	public class CanUseSagaStateMachineTestFixtureForTestingSagaStateMachines
		: StateMachineTestContext<CanUseSagaStateMachineTestFixtureForTestingSagaStateMachines.MyStateMachine>
	{
		[Fact]
		public void can_process_first_message_and_transition_to_awaiting_payment_state()
		{
			string accountNumber = Guid.NewGuid().ToString(); // correlation token

			Verify(
				When(s => s.NewOrderReceived,
						m=> m.AccountNumber = accountNumber)
					.ExpectToRequestTimeout<PrepareReceiptForCustomer>(TimeSpan.FromSeconds(5), 
						m => m.AccountNumber = accountNumber)

					// can do asserts on state machine data to make sure it is changed properly for storage
				   // during the verification tests of the expectations on the state machine:
					.SetAssertOn<MyStateMachineData>(data => data.Counter == 1)
					.ExpectToTransitionToState(s => s.AwaitingPayment),

				 When(statemachine => statemachine.InvoiceReceived, 
				     m => m.AccountNumber = accountNumber)
					.ExpectToPublish<PrepareReceiptForCustomer>(m=> m.AccountNumber = accountNumber)
					.ExpectToComplete()

					);
		}

		public class MyStateMachineData : IStateMachineData
		{
			public Guid Id { get; set; }
			public string State { get; set; }
			public int Version { get; set; }
			public int Counter { get; set; }
			public string AccountNumber { get; set; }
		}

		public class OrderReceived : IMessage
		{
			public string AccountNumber { get; set; }
		}

		public class PrepareReceiptForCustomer : IMessage
		{
			public string AccountNumber { get; set; }
			public double Amount { get; set; }
		}

		public class InvoiceGenerated : IMessage
		{
			public Guid CorrelationId { get; set; }
			public string AccountNumber { get; set; }
		}

		public class MyStateMachine :
		   SagaStateMachine<MyStateMachineData>,
		   StartedBy<OrderReceived>,
		   OrchestratedBy<Message2>,
		   OrchestratedBy<InvoiceGenerated>
		{
			public Event<OrderReceived> NewOrderReceived { get; set; }
			public Event<InvoiceGenerated> InvoiceReceived { get; set; }

			public State AwaitingPayment { get; set; }
			public State Empty { get; set; }
			public State WaitingForInvoice { get; set; }

			public override void Define()
			{
				Initially(
					When(NewOrderReceived)
							.Do((msg) =>
							    	{
							    		this.Data.Counter += 1;
							    		this.Data.AccountNumber = msg.AccountNumber;
							    	})
							.Delay<PrepareReceiptForCustomer>(TimeSpan.FromSeconds(5), (rcvd, msg) =>
							                                                           	{
							                                                           		msg.AccountNumber = msg.AccountNumber;
							                                                           	})
							.TransitionTo(AwaitingPayment)
						);

				While(WaitingForInvoice,
					When(InvoiceReceived)
					   .Publish<PrepareReceiptForCustomer>((invoice, receipt) =>
					                                       	{
					                                       		receipt.AccountNumber = this.Data.AccountNumber;
																System.Diagnostics.Debug.WriteLine("Publishing message...with counter " + this.Data.Counter);
															})
					   .Complete()
					   );

			}

			public void Consume(OrderReceived message)
			{

			}

			public void Consume(Message2 message)
			{

			}

			public void Consume(InvoiceGenerated message)
			{

			}

			public override void ConfigureHowToFindStateMachineInstanceDataFromMessages()
			{
				CorrelateMessageToStateMachineData<OrderReceived>(
					statemachine => statemachine.AccountNumber, 
					message => message.AccountNumber);

				CorrelateMessageToStateMachineData<InvoiceGenerated>(
					s => s.AccountNumber,
					m => m.AccountNumber);
			}
		}
	}
}