using System;
using System.Text;
using System.Threading;
using Monaco.Configuration;
using Xunit;

namespace Monaco.Tests.Bus.Features.Tasks
{
	public class ProducerEndpointConfig : ICanConfigureEndpoint
	{
		public  void Configure(IConfiguration configuration)
		{
			configuration
				.WithContainer(c => c.UsingWindsor())
				.WithStorage(s => s.UsingInMemoryStorage())
				.WithTransport(t => t.UsingMsmq())
				.WithEndpoint(e => e.MapTask<InspectFaxForLoanDocumentImagesTask>("Loan Document Fax Image Inspection Task",
					TimeSpan.FromSeconds(5)));
		}
	}
	
	// message produced for consumption:
	public class LoanDocumentImage : IMessage
	{
		public string Id { get; set; }
		public byte[] Image { get; set; }
	}

	// task for producing message for consumption inside of an endpoint:
	public class InspectFaxForLoanDocumentImagesTask : Produces<LoanDocumentImage>
	{
		public LoanDocumentImage Produce()
		{
			// create the message for a loan image (producer):
			var id = Guid.NewGuid().ToString();
			return new LoanDocumentImage {Id = id, Image = ASCIIEncoding.ASCII.GetBytes(id)};
		}
	}

	public class CanScheduleTaskToProduceMessage : IDisposable, 
		TransientConsumerOf<LoanDocumentImage>
	{
		private static int receivedMessages;
		private MonacoConfiguration configuration;
		private  ManualResetEvent wait;

		public CanScheduleTaskToProduceMessage()
		{
			configuration =
				MonacoConfiguration.BootFromEndpoint<ProducerEndpointConfig>(@"sample.config");
			wait = new ManualResetEvent(false);
			receivedMessages = 0;
		}

		public void Dispose()
		{
			if (configuration != null)
			{
				configuration.Dispose();
			}
			configuration = null;

			if(wait != null)
			{
				wait.Close();
			}
			wait = null;
		}

		[Fact]
		public void can_register_message_producer_to_send_scheduled_messages_to_the_bus_for_consumption()
		{
			using(var bus = configuration.Container.Resolve<IServiceBus>())
			{
				bus.AddInstanceConsumer(this);
				bus.Start();

				wait.WaitOne(TimeSpan.FromSeconds(5)); 
				
				// fired once on start (immediate dispatch) from default configuration:
				Assert.Equal(1, receivedMessages);
			}
		}

		public void Consume(LoanDocumentImage message)
		{
			receivedMessages++;
			wait.Set();
		}
	}

}