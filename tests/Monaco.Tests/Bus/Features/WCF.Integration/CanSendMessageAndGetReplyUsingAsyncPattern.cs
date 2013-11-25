using System;
using System.ServiceModel;
using System.Threading;
using Monaco.Configuration;
using Monaco.Extensions;
using Monaco.WCF;
using Xunit;

namespace Monaco.Tests.Bus.Features.WCF.Integration
{
	/* Steps to create a self-hosted WCF service on the bus:
	 * 
	 * 1. Create your normal WCF service with contract and concrete implementation, most 
	 *     notably from the async-pattern for WCF.
	 * 
	 * 2. Create an endpoint configuration class that inherits from BaseEndpointConfiguration 
	 *     so that the bus can pick up your module that hosts your WCF service
	 *     
	 * 3. Create a class that inherits the abstract class BaseWCFServiceBusModule<,>, and in the 
	 *     "Configure" method set your binding and endpoint address
	 *     
	 * 4. Start the service bus, use the ChannelFactory<> to create a proxy for your WCF 
	 *     services and send messages to WCF service to call the message consumer on the 
	 *     service bus...(a facade pattern over the service bus using WCF)
	 *     
	 */

	//need endpoint configuration in order to have bus modules hosted:
	public class AsyncPatternEndpointConfig : ICanConfigureEndpoint
	{
		public void Configure(IConfiguration configuration)
		{
			
		}
	}

	public class PingPongWCFServiceBusModule : 
		BaseWCFServiceBusModule<IPingPongServiceAsync, PingPongServiceAsync>
	{
		public override void Configure(IContainer container)
		{
			BasicHttpBinding binding = new BasicHttpBinding();
			EndpointAddress endpointAddress = new EndpointAddress("http://localhost:9090/PingPongService");

			//this.SetBindingAndEndpoint(binding, endpointAddress);
			//this.SetBindingAndEndpointByName("ping-pong-service");
			//this.Binding = binding;
			//this.Endpoint = endpointAddress;
		}
	}

	// sample bus module to host the WCF service (without abstact class) :
	//public class PingPongWCFServiceBusModule : IBusModule
	//{
	//    private static WindsorServiceHost<IPingPongServiceAsync, PingPongServiceAsync> _host;

	//    public void Dispose()
	//    {
	//        // (4) clean up as normal with WCF self-host:
	//        if (_host != null)
	//        {
	//            _host.Close();
	//        }
	//        _host = null;
	//    }

	//    public void Start(IKernel kernel)
	//    {
	//        // (1). let's register the embedded service host factory implementation in the container:
	//        kernel.AddComponent(typeof(WindsorServiceHostFactory).Name, typeof(WindsorServiceHostFactory));

	//        // (2) must make sure that client and server (this piece of code) have the same 
	//        // bindings, nothing special here, you would do this with WCF anyway...
	//        BasicHttpBinding binding = new BasicHttpBinding();
	//        EndpointAddress endpointAddress = new EndpointAddress("http://localhost:9090/PingPongService");

	//        // (3) grab an instance of the custom self-hosted instance of the service (same as using ServiceHost in WCF)
	//        // and keep the host "open" when started, it will be in use as long as the bus is "started":
	//        var serviceHostFactory = kernel.Resolve<WindsorServiceHostFactory>();
	//        _host = serviceHostFactory.CreateSelfHost<IPingPongServiceAsync, PingPongServiceAsync>(binding, endpointAddress.Uri);
	//        _host.Open();
	//    }
	//}

	public class CanUseBusToSendMessageAndGetReplyUsingAsyncPattern : IDisposable,
		TransientConsumerOf<PongReplyMessage>,
		TransientConsumerOf<PongReplyMessageTimeout>
	{
		public static ManualResetEvent _wait;
		public static IMessage _received_message;
		private MonacoConfiguration _container;
		public static int _delay; 

		private WcfDisposableClient<IPingPongServiceAsync> _client; 

		public CanUseBusToSendMessageAndGetReplyUsingAsyncPattern()
		{
			_container = new MonacoConfiguration(@"sample.config");
			_wait = new ManualResetEvent(false);
		}

		public void Dispose()
		{
			if (_container != null)
			{
				_container.Dispose();
			}
			_container = null;

			if (_wait != null)
			{
				_wait.Close();
				_wait = null;
			}

			if(_client != null)
			{
				_client.Dispose();
			}
			_client = null;
		}

		[Fact(Skip = "WCF interoperability is deprecated in favor of SignalR")]
		public void can_send_a_message_to_a_consumer_using_async_pattern_over_a_wcf_web_service()
		{
			using (var bus = _container.Resolve<IServiceBus>())
			{
				bus.ConfiguredWithEndpoint<AsyncPatternEndpointConfig>();

				bus.AddInstanceConsumer<PingPongService>();
				bus.AddInstanceConsumer(this);

				bus.Start();

				// resolve the client from the wcf module to communicate to the service:
				var module = bus.Find<PingPongWCFServiceBusModule>();
				_client = module.CreateChannelFactoryClient("ping-pong-client");

				_client.Service.BeginPing(new PingRequestMessage(), OnPingCompleted, null);
				
				_wait.WaitOne(TimeSpan.FromSeconds(20));

				Assert.IsType<PongReplyMessage>(_received_message);

			}
		}

		private void OnPingCompleted(IAsyncResult ar)
		{
			_received_message = _client.Service.EndPing(ar);
			System.Diagnostics.Debug.WriteLine("Pong reply message delivered asynchronously...");
			_wait.Set();
		}

		public void Consume(PongReplyMessage message)
		{
			System.Diagnostics.Debug.WriteLine("Pong reply message delivered to consumer via bus!!!");
		}

		public void Consume(PongReplyMessageTimeout message)
		{
			System.Diagnostics.Debug.WriteLine("The time was exceeded to receive the pong reply");
		}
	}

	// The message consumer handling the message from WCF via the service bus:
	public class PingPongService :
		TransientConsumerOf<PingRequestMessage>
	{
		private readonly IServiceBus _bus;

		public PingPongService(IServiceBus bus)
		{
			_bus = bus;
		}

		public void Consume(PingRequestMessage message)
		{
			// send the reply back to WCF and any interested consumers:
			System.Threading.Thread.Sleep(TimeSpan.FromSeconds(30));
			_bus.Reply(new PongReplyMessage());
		}
	}

	// WCF veneer to get message onto bus and finally to consumer (exposing service to outside world):
	[ServiceContract]
	public interface IPingPongServiceAsync
	{
		[OperationContract(AsyncPattern = true)]
		IAsyncResult BeginPing(PingRequestMessage message, AsyncCallback callback, object state);
		PongReplyMessage EndPing(IAsyncResult asyncResult);
	}

	[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Single)]
	public class PingPongServiceAsync : IPingPongServiceAsync, IDisposable,
		TransientConsumerOf<PongReplyMessageTimeout>
	{
		private static IServiceAsyncRequest _request;
		private readonly IServiceBus _bus;

		public PingPongServiceAsync(IServiceBus bus)
		{
			_bus = bus;
		}

		public void Dispose()
		{
			if(_request != null)
			{
				_request.Complete();
			}
			_request = null;
		}

		public IAsyncResult BeginPing(PingRequestMessage message, AsyncCallback callback, object state)
		{
			_request = _bus.EnqueueRequest().WithCallback(callback, state)
				.WithTimeout<PongReplyMessageTimeout>(5.Seconds().FromNow(),  m=> { });

			_request.Send(message);

			return _request;
		}

		public PongReplyMessage EndPing(IAsyncResult asyncResult)
		{
			return _request.GetReply<PongReplyMessage>();
		}

		public void Consume(PongReplyMessageTimeout message)
		{
			_request.Complete();
		}
	}

	public class PingRequestMessage : IMessage
	{
	}

	public class PongReplyMessage : IMessage { }

	public class PongReplyMessageTimeout : IMessage
	{

	}
}