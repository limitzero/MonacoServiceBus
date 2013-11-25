using System;
using System.IO;
using System.Messaging;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Transactions;
using log4net;
using Monaco.Bus;
using Monaco.Bus.Internals;

namespace Monaco.Transport.Msmq
{
	public class MsmqTransport : BaseTransport<Message>
	{
		private readonly ILog _logger = LogManager.GetLogger(typeof(MsmqTransport));
		private MessageQueue _queue;
		public static readonly string SCHEME = "msmq";
		public static readonly string PROTOCOL = SCHEME + @"://";

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

		public override void Connect()
		{
			// initiate the connection to the queues
			if (Disposed) return;

			if (_queue != null) return;

			try
			{
				CreateTransactionalQueue(this.EndpointUri);
			}
			catch
			{
			}

			// always normalize the endpoint uri to the transport
			// specific implementation before creating the resource:
			string location = Normalize(this.EndpointUri);
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
			this.DoSend(null, envelope);
		}

		public override void DoSend(string location, IEnvelope envelope)
		{
			string path = string.Empty;

			if (Disposed) return;

			if (string.IsNullOrEmpty(location))
			{
				Connect();
				path = Normalize(this.EndpointUri);
			}
			else
			{
				CreateTransactionalQueue(location);
				path = Normalize(location);
			}

			var toSend = new Message();

			AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

			using (var txn = new TransactionScope(TransactionScopeOption.RequiresNew))
			using (var queue = new MessageQueue(path, QueueAccessMode.Send))
			{
				try
				{
					toSend.BodyStream = new MemoryStream(envelope.Body.PayloadStream);
					toSend.Recoverable = true;
					toSend.Label = envelope.Body.Label;

					if (path.Contains(Environment.MachineName))
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
			this.Connect();

			try
			{
				this._queue.ReceiveById(this.CurrentMessage.Id);
			}
			catch (MessageQueueException messageQueueException)
			{
				this.HandleMessageQueueException(messageQueueException, TimeSpan.Zero);
			}

		}

		private bool TryPeek(out Message message, TimeSpan timeout)
		{
			message = null;

			try
			{
				message = this._queue.Peek(timeout);
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
			return true;
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

		private static void CreateTransactionalQueue(string path)
		{
			AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

			string location = Normalize(path);

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

		public static string Normalize(string address)
		{
			string path = string.Empty;

			string thePath = address.Replace(PROTOCOL, string.Empty);
			string[] theParts = thePath.Split(new char[] { '/' });

			string server = theParts[0].Trim().ToLower() == "localhost"
								? Environment.MachineName
								: theParts[0].Trim().ToUpper();

			string queue = theParts[1].Trim().ToLower();

			if (server != Environment.MachineName)
			{
				// needed for store-and-forward capability of MSMQ:
				path = string.Format(@"FormatName:DIRECT=OS:{0}\{1}", server, queue);
			}
			else
			{
				path = string.Format(@"{0}\Private$\{1}", server, queue);
			}

			return path;
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