using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Monaco.Bus;
using Monaco.Bus.Internals.Agent;
using Monaco.Bus.Internals.Collections;
using Monaco.Bus.MessageManagement.Serialization;
using Monaco.Endpoint;

namespace Monaco.Transport
{
	/// <summary>
	/// Base functionality of a transport that can send and receive messages.
	/// </summary>
	public abstract class BaseTransport<TCurrentMessage> : BaseAgent, ITransport
	{
		private static ReceiverCache _cache;
		private static readonly object _cache_lock = new object();
		private IThreadSafeDictionary<string, int> _errorCache;

		protected BaseTransport(BaseEndpoint endpoint)
		{
			Endpoint = endpoint;

			IsRecoverable = true;

			// set the transactions up for concurrent reads (default)
			// (this will not be honored if the transport does not support
			// the TransactionScope or MSDTC):
			TransactionIsolationLevel = IsolationLevel.RepeatableRead;
		}

		/// <summary>
		/// Gets or sets the current message that is being processed by the transport on a receive.
		/// </summary>
		public TCurrentMessage CurrentMessage { get; set; }

		#region ITransport Members

		public IEndpoint Endpoint { get; private set; }
		public event Action<ITransport> OnReceiveStart;
		public event Action<IEnvelope, ITransport> OnReceiveCompleted;
		public event Action<IEnvelope, Exception> OnReceiveError;
		public event Action<IEnvelope, Exception> OnRetryExhausted;
		public event EventHandler<MessageReceivedEventArgs> OnMessageReceived;

		public IsolationLevel TransactionIsolationLevel { get; set; }

		public int NumberOfWorkerThreads { get; set; }

		public bool IsTransactional { get; set; }

		public virtual int MaxRetries { get; set; }

		public virtual bool IsRecoverable { get; set; }

		public ISerializationProvider SerializationProvider { get; set; }

		public virtual void RemoveCurrentMessage()
		{
		}

		public virtual void Connect()
		{
		}

		public virtual void Disconnect()
		{
		}

		public virtual void Reconnect()
		{
			Disconnect();
			Connect();
		}

		/// <summary>
		/// This will receive a message from the indicated endpoint via 
		/// the specifics of the particular technology transport. After
		/// the message is received, it will call a void method on the 
		/// caller to notify of the received message.
		/// </summary>
		public virtual IEnvelope Receive()
		{
			IEnvelope envelope = Receive(TimeSpan.Zero);
			return envelope;
		}

		/// <summary>
		/// This will receive a message from the indicated endpoint via 
		/// the specifics of the particular technology transport. After
		/// the message is received, it will call a void method on the 
		/// caller to notify of the received message.
		/// </summary>
		public IEnvelope Receive(TimeSpan timeout)
		{
			IEnvelope envelope = null;

			if (Disposed) return envelope;

			try
			{
				envelope = DoReceive(timeout);

				if (_cache != null)
				{
					if (envelope != null)
					{
						// enqueue the message to the receiver cache to 
						// prevent multiple deliveries across threads:
						envelope = _cache.Receive(envelope);
					}
				}
			}
			catch
			{
				if (IsTransactional == false)
				{
					RegisterForRetry(() => Receive(timeout), envelope, MaxRetries);
				}
				else
				{
					throw;
				}
			}

			return envelope;
		}

		/// <summary>
		/// This will send a message to the desired 
		/// endpoint location via the transport technology.
		/// This will delegate to the <see cref="DoSend(IEnvelope)"/>
		/// method for implementation.
		/// </summary>
		/// <param name="envelope"></param>
		public void Send(IEnvelope envelope)
		{
			if (Disposed) return;

			try
			{
				// send the entire contents of the envelope to the destination:
				byte[] payload = SerializationProvider.SerializeToBytes(envelope);
				envelope.Body.SetStream(payload);
				DoSend(envelope);
			}
			catch
			{
				// register the send operation for retry:
				RegisterForRetry(() => DoSend(envelope), envelope, MaxRetries);
			}
		}

		/// <summary>
		/// This will allow the transport to send a message to a desired location
		/// </summary>
		/// <param name="endpoint"></param>
		/// <param name="envelope"></param>
		public void Send(IEndpoint endpoint, IEnvelope envelope)
		{
			if (Disposed) return;

			if (endpoint == null)
				throw new ArgumentException("The endpoint can not be null for a send operation.");

			try
			{
				// send the entire contents of the envelope to the endpoint:
				byte[] payload = SerializationProvider.SerializeToBytes(envelope);
				envelope.Body.SetStream(payload);

				// normalize the endpoint before sending the message:
				endpoint.Localize();

				DoSend(endpoint, envelope);
			}
			catch
			{
				// register the send operation for retry:
				RegisterForRetry(() => DoSend(endpoint, envelope), envelope, MaxRetries);
			}
		}

		#endregion

		public override void Start()
		{
			Connect();

			_errorCache = new ThreadSafeDictionary<string, int>();

			base.Concurrency = NumberOfWorkerThreads;
			base.Frequency = 0;

			_cache = new ReceiverCache();
			_cache.Start();

			base.Start();
		}

		public override void Stop()
		{
			if (_cache != null)
			{
				_cache.Stop();
			}
			_cache = null;

			Disconnect();
			base.Stop();
		}

		public abstract IEnvelope DoReceive(TimeSpan timeout);

		public abstract void DoSend(IEnvelope envelope);

		public abstract void DoSend(IEndpoint endpoint, IEnvelope envelope);

		public override void Execute()
		{
			if (Disposed || IsRunning == false) return;
			ReceiveMessages();
		}

		/// <summary>
		/// This will create the neccessary transmission message structure for a message received from 
		/// physical storage.
		/// </summary>
		/// <param name="payload"></param>
		/// <param name="label"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		protected IEnvelope PrepareEnvelope(byte[] payload, string label = "", string id = "")
		{
			IEnvelope envelope = null;

			if (SerializationProvider == null) return envelope;

			envelope = SerializationProvider.Deserialize<Envelope>(payload);
			envelope.Body.SetStream(payload);

			if (string.IsNullOrEmpty(id) == false)
			{
				envelope.Header.MessageId = id;
			}

			return envelope;
		}

		private void ReceiveMessages()
		{
			if (IsTransactional)
			{
				ReceiveWithTransaction();
			}
			else
			{
				GuardForReceive();
			}
		}

		private void GuardForReceive()
		{
			IEnvelope envelope = Receive();

			if (envelope != null)
			{
				OnTransportMessageReceived(envelope);
				OnTransportReceiveCompleted(envelope);
			}
		}

		private void ReceiveWithTransaction()
		{
			IEnvelope envelope = null;

			var options = new TransactionOptions();
			options.IsolationLevel = TransactionIsolationLevel;

			using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew, options))
			{
				try
				{
					OnTransportReceiveStarted();

					envelope = Receive();

					if (envelope != null)
					{
						envelope.Header.LocalEndpoint = Endpoint.EndpointUri.OriginalString;
						OnTransportMessageReceived(envelope);
					}

					scope.Complete();
				}
				catch (Exception exception)
				{
					if (HasExeceededRetryAttempts(envelope))
					{
						OnTransportReceiveError(envelope, exception);
						scope.Complete();
					}
					else
					{
						throw;
					}
				}
				finally
				{
					OnTransportReceiveCompleted(envelope);
				}
			}
		}

		private bool HasExeceededRetryAttempts(IEnvelope envelope)
		{
			bool hasExceededRetryAttempts = false;

			lock (_cache_lock)
			{
				int retries;
				_errorCache.TryGetValue(envelope.Header.MessageId, out retries);

				if (retries == 0) retries = 1; // out value will return zero if not found:

				if (retries > MaxRetries)
				{
					_errorCache.Remove(envelope.Header.MessageId);
					hasExceededRetryAttempts = true;
				}
				else
				{
					_errorCache.Remove(envelope.Body.Label);
					Interlocked.Increment(ref retries);
					_errorCache.Add(envelope.Body.Label, retries);
				}
			}

			return hasExceededRetryAttempts;
		}

		private void OnTransportRetryExhausted(IEnvelope envelope, Exception exception)
		{
			Action<IEnvelope, Exception> action = OnRetryExhausted;

			if (action != null)
			{
				action(envelope, exception);
			}
		}

		private void RegisterForRetry(Action action, IEnvelope envelope, int retries)
		{
			Exception theException = null;

			try
			{
				for (int retry = 1; retry < retries; retry++)
				{
					try
					{
						action();
						theException = null;
						break;
					}
					catch (Exception exception)
					{
						string msg =
							string.Format(
								"Retry attempt #{0} for message '{1}' from endpoint '{2}'. Reason: {3}",
								retry,
								envelope.Body.Label,
								envelope.Header.LocalEndpoint,
								exception);
						OnReceiveError(envelope, new Exception(msg));

						theException = exception;

						continue;
					}
				}

				if (theException != null)
				{
					throw theException;
				}
			}
			catch
			{
			}
		}

		private void OnTransportReceiveStarted()
		{
			Action<ITransport> action = OnReceiveStart;

			if (action != null)
			{
				action(this);
			}
		}

		private void OnTransportReceiveCompleted(IEnvelope message)
		{
			Action<IEnvelope, ITransport> action = OnReceiveCompleted;

			if (action != null)
			{
				action(message, this);
			}
		}

		private void OnTransportMessageReceived(IEnvelope envelope)
		{
			EventHandler<MessageReceivedEventArgs> evt = OnMessageReceived;

			if (evt != null)
			{
				evt(this, new MessageReceivedEventArgs(envelope));
			}
		}

		private void OnTransportReceiveError(IEnvelope message, Exception exception)
		{
			Action<IEnvelope, Exception> action = OnReceiveError;

			if (action != null)
				action(message, exception);
		}
	}
}