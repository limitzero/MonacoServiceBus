using Monaco.Bus.Services.Timeout.Messages.Commands;
using Monaco.Bus.Services.Timeout.Messages.Events;
using Monaco.Extensibility.Logging;

namespace Monaco.Bus.Services.Timeout
{
	public class TimeoutsConsumer :
		Consumes<ScheduleTimeout>,
		Consumes<TimeoutExpired>,
		Consumes<CancelTimeout>
	{
		private readonly IServiceBus bus;
		private readonly ITimeoutsService timeoutsService;

		public TimeoutsConsumer(IServiceBus bus, ITimeoutsService timeoutsService)
		{
			this.bus = bus;
			this.timeoutsService = timeoutsService;
		}

		public void Consume(CancelTimeout message)
		{
			timeoutsService.RegisterCancel(message);
		}

		public void Consume(ScheduleTimeout message)
		{
			message.Endpoint = bus.Endpoint.EndpointUri.OriginalString;
			timeoutsService.RegisterTimeout(message);
		}

		public void Consume(TimeoutExpired message)
		{
			this.bus.ConsumeMessages(message.Message as IMessage);
		}
	}
}