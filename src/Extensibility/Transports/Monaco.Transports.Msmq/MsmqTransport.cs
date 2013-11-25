using System;
using System.IO;
using System.Linq;
using System.Messaging;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Transactions;
using log4net;
using Monaco.Bus;
using Monaco.Endpoint;
using Monaco.Transport;

namespace Monaco.Transports.Msmq
{
	public class MsmqTransport : BaseTransport<Message>
	{
		private readonly ILog _logger = LogManager.GetLogger(typeof(MsmqTransport));
		private MessageQueue _queue;

		public override bool IsRecoverable
		{
			get
			{
				return base.IsRecoverable;
			}
			set
			{
				base.IsRecoverable = value;
				this.CheckForRecovery(value);
			}
		}

		public MsmqTransport(MsmqEndpoint endpoint)
			:base(endpoint)
		{
			this.IsTransactional = true;
			base.OnReceiveCompleted += OnMsmqReceiveCompleted;
		}

		public override void OnDisposing()
		{
			this.OnReceiveCompleted -= OnMsmqReceiveCompleted;
		}

		

		public override void Connect()
		{
			// initiate the connection to the queues
			if (Disposed) return;

			if (_queue != null) return;


			// always localize the endpoint uri to the transport
			// specific implementation before creating the resource:
			this.Endpoint.Localize();
			string location = this.Endpoint.LocalizedEndpointUri;

			try
			{
				CreateTransactionalQueue(location);
			}
			catch
			{
			}

			_queue = new MessageQueue(location);

			var mpf = new MessagePropertyFilter();

			try
			{
				mpf.SetAll();
			}
			catch
			{
			}

			_queue.MessageReadPropertyFilter = mpf;
		}

		public override void Disconnect()
		{
			if (_queue != null)
			{
				_queue.Dispose();
				_queue = null;
			}
		}

		public override void Reconnect()
		{
			if (Disposed) return;

			this.Disconnect();
			this.Connect();
		}

		public override IEnvelope DoReceive(TimeSpan timeout)
		{
			IEnvelope envelope = null;

			if (Disposed) return envelope;

			try
			{
				Connect();

				Message message = null;

				if (this.TryPeek(out message, timeout) == true)
				{
					// got a message, go ahead and prepare it for receipt on the bus:
					envelope = this.PrepareForReceipt(message);
				}

			}
			catch (MessageQueueException exception)
			{
				HandleMessageQueueException(exception, timeout);
			}

			return envelope;
		}

		public override void DoSend(IEnvelope envelope)
		{
			this.DoSend(this.Endpoint, envelope);
		}

		public override void DoSend(IEndpoint endpoint, IEnvelope envelope)
		{
			if (Disposed) return;

			endpoint.Localize();

			// create the queue, if it does not exist:
			CreateTransactionalQueue(endpoint.LocalizedEndpointUri);

			var toSend = new Message();

			AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

			using (var txn = new TransactionScope(TransactionScopeOption.RequiresNew))
			using (var queue = new MessageQueue(endpoint.LocalizedEndpointUri, QueueAccessMode.Send))
			{
				try
				{
					toSend.BodyStream = new MemoryStream(envelope.Body.GetStream());
					toSend.Recoverable = true;
					toSend.Label = envelope.Body.Label;

					if (endpoint.LocalizedEndpointUri.Contains(Environment.MachineName))
						queue.Send(toSend, GetTransactionTypeForSend(IsTransactional));
					else
					{
						queue.Send(toSend);
					}

					txn.Complete();
				}
				catch
				{
					throw;
				}
			}
		}

		public override void RemoveCurrentMessage()
		{
			if (this.CurrentMessage == null) return;

			this.Connect();
			
			try
			{
				this._queue.ReceiveById(this.CurrentMessage.Id);
			}
			catch (MessageQueueException messageQueueException)
			{
				this.HandleMessageQueueException(messageQueueException, TimeSpan.Zero);
			}
			catch
			{
				// message is not there anymore..ignore
			}
			

		}

		private void OnMsmqReceiveCompleted(IEnvelope arg1, ITransport arg2)
		{
			this.RemoveCurrentMessage();
		}

		private bool TryPeek(out Message message, TimeSpan timeout)
		{
			message = null;

			try
			{
				if (this.IsRunning)
				{
					message = this._queue.Peek(timeout);
				}
				else
				{
					using (var enumerator = _queue.GetMessageEnumerator2())
					{
						while (enumerator.MoveNext(timeout))
						{
							message = enumerator.Current;
							if (message != null)
								break;
						}
					}
				}

			}
			catch (MessageQueueException messageQueueException)
			{
				if (messageQueueException.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
				{
					// could not peek a message in the given time, move on (no need to log this):
					//_logger.Warn("Could not peek a message from queue: " + _queue.QueueName + " in the given timeframe", 
					//    messageQueueException);
					return false;
				}

				// nothing returned, move on:
				return false;
			}
			catch (Exception exception)
			{
				// could not peek a message, move on:
				_logger.Warn("Could not peek a message from queue: " + _queue.QueueName, exception);
				return false;
			}

			return message != null;
		}

		private IEnvelope PrepareForReceipt(Message theMessage)
		{
			IEnvelope envelope = null;

			if (Disposed || theMessage == null) return envelope;

			Message theReceivedMessage = _queue.ReceiveById(theMessage.Id,
														GetTransactionTypeForReceive(this.IsTransactional));

			this.CurrentMessage = theReceivedMessage;

			try
			{
				byte[] payload = { };

				theReceivedMessage.BodyStream.Seek(0, SeekOrigin.Begin);

				using (TextReader reader = new StreamReader(theReceivedMessage.BodyStream))
				{
					payload = ASCIIEncoding.ASCII.GetBytes(reader.ReadToEnd());
				}

				envelope = PrepareEnvelope(payload, theReceivedMessage.Label, theReceivedMessage.Id);

			}
			catch
			{
				throw;
			}

			return envelope;
		}

		private static void CreateTransactionalQueue(string location)
		{
			AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

			// can not create a non-local queue (just send it
			// and MSMQ will store and forward it):
			if (!location.Contains(Environment.MachineName)) return;

			if (!MessageQueue.Exists(location))
			{
				MessageQueue.Create(location, true);
			}
			else
			{
				var queue = new MessageQueue(location);
				if (!queue.Transactional)
					throw new ArgumentException("The queue [" + location + "] must be transactional.");
			}
		}

		private void CheckForRecovery(bool recoverable)
		{
			if (recoverable == false)
			{
				this.Connect();

				try
				{
					_queue.Purge();
				}
				catch (Exception e)
				{
					_logger.Warn("Could not purge queue " + _queue.QueueName + ". Reason: " + e.ToString());
				}
			}
		}

		private static MessageQueueTransactionType GetTransactionTypeForReceive(bool isTransactional)
		{
			if (isTransactional)
				return MessageQueueTransactionType.Automatic;
			return MessageQueueTransactionType.Single;
		}

		private static MessageQueueTransactionType GetTransactionTypeForSend(bool isTransactional)
		{
			if (isTransactional)
				return MessageQueueTransactionType.Automatic;

			return MessageQueueTransactionType.Single;
		}

		private void HandleMessageQueueException(MessageQueueException theException, TimeSpan timeout)
		{
			switch (theException.MessageQueueErrorCode)
			{
				case MessageQueueErrorCode.ServiceNotAvailable:
					Thread.Sleep(timeout);
					this.Reconnect();
					break;

				case MessageQueueErrorCode.IOTimeout:
					break;

				case MessageQueueErrorCode.QueueNotAvailable:

				case MessageQueueErrorCode.MessageNotFound:
					break;

				case MessageQueueErrorCode.PropertyNotAllowed:
				case MessageQueueErrorCode.AccessDenied:

				case MessageQueueErrorCode.QueueDeleted:
					//if (_log.IsErrorEnabled) 
					//    _log.Error("The message queue was not available: " + _address.FormatName, ex); 
					Thread.Sleep(timeout);
					Reconnect();
					break;

				case MessageQueueErrorCode.QueueNotFound:
				case MessageQueueErrorCode.IllegalFormatName:
				case MessageQueueErrorCode.MachineNotFound:
					//if (_log.IsErrorEnabled) 
					//    _log.Error("The message queue was not found or is improperly named: " + _address.FormatName, ex); 
					Thread.Sleep(timeout);
					Reconnect();
					break;
				case MessageQueueErrorCode.MessageAlreadyReceived:
					// we are competing with another consumer, no reason to report an error since	
					// the message has already been handled.					
					//if (_log.IsDebugEnabled)						
					//        _log.Debug("The message was removed from the queue before it could be received. This could be the result of another service reading from the same queue.");					
					break;
				case MessageQueueErrorCode.InvalidHandle:
				case MessageQueueErrorCode.StaleHandle:
					//if (_log.IsErrorEnabled)						
					//   _log.Error("The message queue handle is stale or no longer valid due to a restart of the message queuing service: " + _address.FormatName, ex);		
					Reconnect();
					Thread.Sleep(timeout);
					break;
				default:
					break;
			}
		}
	}
}