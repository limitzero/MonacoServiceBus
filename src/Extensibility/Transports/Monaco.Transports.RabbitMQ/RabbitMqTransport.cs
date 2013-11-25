using System;
using System.Collections;
using Castle.MicroKernel;
using Monaco.Bus;
using Monaco.Configuration;
using Monaco.Endpoint;
using Monaco.Transport;
using Monaco.Transports.RabbitMQ.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.MessagePatterns;

namespace Monaco.Transports.RabbitMQ
{
	public class RabbitMqTransport : BaseTransport<BasicDeliverEventArgs>
	{
		private readonly IContainer container;
		private ConnectionFactory factory;
		private Subscription rabbitMQSubscription;

		public RabbitMqTransport(IContainer container, RabbitMqEndpoint endpoint) 
			: base(endpoint)
		{
			this.container = container;
			// MSDTC is not supported for RabbitMQ for .NET clients
			// in order to get proper removal of queue messages 
			// we must hook into the receive completed event 
			// and manually acknowledge the receipt of message 
			// to remove it from the queue.
			this.IsTransactional = false; 
			base.OnReceiveCompleted += OnRabbitMqMessageCompleted;
		}

		public override void OnDisposing()
		{
			this.OnReceiveCompleted -= OnRabbitMqMessageCompleted;
		}

		public override void Connect()
		{
			// initiate the connection to the queues
			if (Disposed) return;

			if (this.factory != null) return;
		
			// always localize the endpoint uri to the transport
			// specific implementation before creating the resource:
			this.Endpoint.Localize();

			try
			{
				CreateQueue(this.Endpoint);
			}
			catch
			{
			}

		}

		public override IEnvelope DoReceive(TimeSpan timeout)
		{
			IEnvelope envelope = null;

			if (Disposed) return envelope;

			try
			{
				Connect();

				BasicDeliverEventArgs message = null;

				if (this.TryPeek(out message, timeout) == true)
				{
					// got a message, go ahead and prepare it for receipt on the bus:
					envelope = this.PrepareForReceipt(message);
				}

			}
			catch
			{
				//HandleMessageQueueException(exception, timeout);
			}

			return envelope;
		}

		public override void Disconnect()
		{
			if (rabbitMQSubscription != null)
			{
				rabbitMQSubscription.Close();
			}
			rabbitMQSubscription = null;

			if (this.factory != null)
			{
				this.factory = null;
			}
		}

		public override void DoSend(IEnvelope envelope)
		{
			this.DoSend(this.Endpoint, envelope);
		}

		public override void DoSend(IEndpoint endpoint, IEnvelope envelope)
		{
			endpoint.Localize();
			var credential = this.GetSettings(endpoint);

			CreateConnectionFactory(credential);

			using(var connection = this.factory.CreateConnection())
			using(var model = connection.CreateModel())
			{
				model.ExchangeDeclare(credential.Exchange, ExchangeType.Fanout, this.IsRecoverable);
				model.QueueDeclare(endpoint.EndpointUri.Host, this.IsRecoverable, false, false, new Hashtable());

				model.QueueBind(endpoint.EndpointUri.Host, credential.Exchange, ""); 

				IBasicProperties basicProperties = 
					model.CreateBasicProperties(); 
				
				model.BasicPublish(credential.Exchange, "", false, false, basicProperties, envelope.Body.PayloadStream); 
			}
		}

		private bool TryPeek(out BasicDeliverEventArgs message, TimeSpan timeout)
		{
			message = null; 

			using(var connection = this.factory.CreateConnection())
			using (IModel model = connection.CreateModel())
			{
				QueueingBasicConsumer consumer = new QueueingBasicConsumer(model);
				string tag = model.BasicConsume(this.Endpoint.EndpointUri.Host, false, consumer);

				object nextMessage = null;
				if( consumer.Queue.Dequeue(timeout.Milliseconds, out nextMessage) == true)
				{
					if(nextMessage != null)
					{
						message = nextMessage as BasicDeliverEventArgs;
					}
				}				
			}

			return message != null;
		}

		private void CreateConnectionFactory(IRabbitMQConfigurationSettings credential)
		{
			this.factory = new ConnectionFactory
			{ 
				HostName = credential.Host, 
				VirtualHost = "/",
				UserName = credential.UserName, 
				Password = credential.Password, 
				Protocol = Protocols.SafeLookup(credential.Protocol),
				Port = credential.Port
			};
		}

		private void OnRabbitMqMessageCompleted(IEnvelope envelope, ITransport transport)
		{
			// this is the transactional "Delete" of the message from the exchange:
			if (this.CurrentMessage != null)
			{
				if (this.rabbitMQSubscription != null)
				{
					this.rabbitMQSubscription.Ack(this.CurrentMessage);
				}
			}
		}

		private IEnvelope PrepareForReceipt(BasicDeliverEventArgs theMessage)
		{
			IEnvelope envelope = null;

			if (Disposed || theMessage == null) return envelope;

			this.CurrentMessage = theMessage;

			try
			{
				byte[] payload = theMessage.Body;
				envelope = PrepareEnvelope(payload, string.Empty, theMessage.BasicProperties.MessageId);
			}
			catch
			{
				throw;
			}

			return envelope;
		}

		private void CreateQueue(IEndpoint endpoint)
		{
			var settings = this.GetSettings(endpoint);
			this.CreateConnectionFactory(settings);

			// create the exchange and the queue
			using(var connection = this.factory.CreateConnection())
			using(var model = connection.CreateModel())
			{
				model.ExchangeDeclare(settings.Exchange, ExchangeType.Fanout, this.IsRecoverable);
				model.QueueDeclare(this.Endpoint.EndpointUri.Host, true, false, false, new Hashtable());
				model.QueueBind(this.Endpoint.EndpointUri.Host, settings.Exchange, ""); 
			}
		}

		private IRabbitMQConfigurationSettings GetSettings(IEndpoint endpoint)
		{
			var configuration = this.container.Resolve<IRabbitMQConfigurationSettings>();
			return configuration;
		}
	}
}