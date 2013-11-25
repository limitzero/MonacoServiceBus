using System;
using System.Linq;
using Monaco.Extensibility.Storage.StateMachines;
using Monaco.StateMachine;
using Monaco.Testing.StateMachines;
using Xunit;

namespace Monaco.Testing.Tests
{
	/// <summary>
	/// Taken from http://skillsmatter.com/podcast/home/death-batch-job
	/// </summary>
	public class when_reaching_the_threshold_within_a_year_for_purchases_for_customer
		: StateMachineTestContext<PreferredCustomerPolicyStateMachine>
	{
		public when_reaching_the_threshold_within_a_year_for_purchases_for_customer()
		{
			// only used when basic message to instance data correlation  is not sufficient:
			RegisterStateMachineDataFinder<PreferredCustomerPolicyStateMachineStateMachineDataFinder>();
		}

		[Fact]
		public void they_should_become_a_preferred_customer()
		{
			int customerId = 100; // this is the token for the customer or business key identifying a customer
		
			Verify(
				When(statemachine => statemachine.NewOrderIsAccepted,
					 (message) =>
					 {
						 message.CustomerId = customerId;
						 message.OrderValue = 3000;
					 })
					.ExpectNotToPublish<CustomerMadePreferred>(),

				// sending another order to increment the volume should make the customer "preferred"...
				When(statemachine => statemachine.NewOrderIsAccepted,
				    (message) =>
				    {
				        message.CustomerId = customerId;
				        message.OrderValue = 3000;
				    })
				    .ExpectToPublish<CustomerMadePreferred>()
				    // change the customer back to 'regular' status 365 days after they have
				    // been made 'preferred' (a yearly promotion):
				    .ExpectToRequestTimeout<PreferredStatusExpired>(TimeSpan.FromDays(365), 
					  (message) =>
					  	{
					  		message.CustomerId = customerId; 
					  	}),

				// change the customer status to "regular" and make sure the totals are zeroed-out
				// when the expiration message occurs after a year's time:
				WhenTimeoutIsFired()
					.ExpectToPublish<CustomerCampaignEnded>(m => m.CustomerId = customerId)

					.SetAssertOn<PreferredCustomerPolicyStateMachineData>( d => d.Running365DayTotal  == 3000, 
					"The running 365 day total should be 3000 after the status has changed from 'Preferred' and the campaign has ended")
				);

		}
	}

	public interface CustomerMadePreferred : IMessage { }

	public interface CustomerCampaignEnded: IMessage
	{
		int CustomerId { get; set; }
	}

	public interface OrderAccepted : IMessage
	{
		int CustomerId { get; set; }
		int OrderValue { get; set; }
	}

	public class PreferredStatusExpired : IMessage
	{
		public int CustomerId { get; set; }
		public OrderAccepted AcceptedOrder { get; set; }
	}

	// instance data stored for the state machine per initiated instance:
	public class PreferredCustomerPolicyStateMachineData : IStateMachineData
	{
		public Guid Id { get; set; }
		public string State { get; set; }
		public int Version { get; set; }
		public int Running365DayTotal { get; set; }
		public int CustomerId { get; set; }
		public bool IsPreferredStatusNotificationSent { get; set; }
	}

	// state machine to govern the condition of making customers 'preferred':
	public class PreferredCustomerPolicyStateMachine :
		SagaStateMachine<PreferredCustomerPolicyStateMachineData>,
		StartedBy<OrderAccepted>,
		OrchestratedBy<PreferredStatusExpired>
	{
		public Event<OrderAccepted> NewOrderIsAccepted { get; set; }
		public Event<PreferredStatusExpired> PreferredStatusHasExpired { get; set; }

		public override void Define()
		{
			this.Name = "Preferred Customer Policy";

			Initially(
				When(NewOrderIsAccepted)
					.Do((msg) =>
							{
								this.Data.CustomerId = msg.CustomerId;
								this.Data.Running365DayTotal += msg.OrderValue;

								// only send the notification once after the threshold 
								// is reached, otherwise they will be sent numerous 'preferred'
								// material:
								CheckForPreferredStatus(msg);
							}
							, "Increment the running total and check to see if the customer meets preferred " +
							"status based on current order volume and publish the preffered status message")
				);

			Also(
				When(PreferredStatusHasExpired)
					.Do((message) =>
					    	{
					    		// decrement the current order total for next year's running total:
					    		this.Data.Running365DayTotal -= message.AcceptedOrder.OrderValue;
					    		this.Data.IsPreferredStatusNotificationSent = false;
					    	},
					    "Change the status of the customer from 'preferred' due to the campaign ending and decrementing the " +
					    "running total of order volume by current order total.")
					.Publish<CustomerCampaignEnded>((s, m) => m.CustomerId = Data.CustomerId)
					);
		}

		private void CheckForPreferredStatus(OrderAccepted acceptedOrder)
		{
			if (this.Data.Running365DayTotal > 5000 && 
			    this.Data.IsPreferredStatusNotificationSent == false)
			{
				Bus.Publish<CustomerMadePreferred>();

				var expired = new PreferredStatusExpired
				              	{
				              		AcceptedOrder = acceptedOrder,
				              		CustomerId = Data.CustomerId
				              	};

				RequestTimeout(TimeSpan.FromDays(365), expired);
				this.Data.IsPreferredStatusNotificationSent = true;
			}
		}

		public override void ConfigureHowToFindStateMachineInstanceDataFromMessages()
		{
			// need to know how to create new instances of state machine data and correlate
			// subsequent messages to the instance data (in this case the customer id will be the simple 
			// correlation identifer):

			CorrelateMessageToStateMachineData<OrderAccepted>(statemachine => statemachine.CustomerId,
				message => message.CustomerId);

			CorrelateMessageToStateMachineData<PreferredStatusExpired>(statemachine =>statemachine.CustomerId, 
				message => message.CustomerId);
		}

		public void Consume(OrderAccepted message)
		{

		}

		public void Consume(PreferredStatusExpired message)
		{
		}
	}

	/// <summary>
	/// This configures how to find the instance data for subsequent messages in a state machine (if they are not 
	/// simply correlated to the state machine data by common correlation identifier)
	/// </summary>
	public class PreferredCustomerPolicyStateMachineStateMachineDataFinder
		: IStateMachineDataFinder<PreferredCustomerPolicyStateMachineData, PreferredStatusExpired>
	{
		private readonly IStateMachineDataRepository<PreferredCustomerPolicyStateMachineData> _repository;

		public PreferredCustomerPolicyStateMachineStateMachineDataFinder(
			IStateMachineDataRepository<PreferredCustomerPolicyStateMachineData> repository)
		{
			_repository = repository;
		}

		public PreferredCustomerPolicyStateMachineData Find(PreferredStatusExpired message)
		{
			return (from match in _repository.FindAll()
					   where match.CustomerId == message.CustomerId
					   select match).FirstOrDefault();
		}
	}
}