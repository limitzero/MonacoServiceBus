using System;
using System.IO;
using System.Text;
using Monaco.Bus;
using Monaco.Endpoint;
using Monaco.Transport;

namespace Monaco.Transports.File
{
	public class FileTransport : BaseTransport<System.IO.FileInfo>
	{
		private DirectoryInfo _queue;

		public FileTransport(FileEndpoint endpoint) :
			base(endpoint)
		{

		}

		public override void OnDisposing()
		{
			this.OnReceiveCompleted -= OnFileMessageCompleted;
		}

		public override void Connect()
		{
			// initiate the connection to the queues
			if (Disposed) return;

			if (this.IsTransactional == true)
			{
				this.OnReceiveCompleted += OnFileMessageCompleted;
			}

			if (_queue != null) return;

			this.Endpoint.Localize();

			this.CreateQueue(this.Endpoint.LocalizedEndpointUri);
			this._queue = new DirectoryInfo(this.Endpoint.LocalizedEndpointUri);
		}

		public override void Disconnect()
		{
			if (_queue != null)
			{
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

				FileInfo message = null;

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

		public override void DoSend(IEnvelope envelope)
		{
			this.DoSend(this.Endpoint, envelope);
		}

		public override void DoSend(IEndpoint endpoint, IEnvelope envelope)
		{
			string fileName = string.Format("ID-{0}-{1}.txt", System.Guid.NewGuid().ToString(), envelope.Body.Label);
			endpoint.Localize();

			this.CreateQueue(endpoint.LocalizedEndpointUri);

			System.IO.File.WriteAllBytes(Path.Combine(endpoint.LocalizedEndpointUri, fileName), envelope.Body.GetStream());
		}

		private bool TryPeek(out FileInfo message, TimeSpan timeout)
		{
			message = null;

			try
			{
				message = this._queue.GetFiles()[0];
			}
			catch
			{
				// could not peek a message, move on:
				//_logger.Warn("Could not peek a message from queue: " + _queue.QueueName, exception);
				return false;
			}

			return message != null;
		}

		private IEnvelope PrepareForReceipt(FileInfo theMessage)
		{
			IEnvelope envelope = null;

			if (Disposed || theMessage == null) return envelope;

			this.CurrentMessage = theMessage;

			try
			{
				byte[] payload = { };

				using (var stream = theMessage.OpenRead())
				using (TextReader reader = new StreamReader(stream))
				{
					payload = ASCIIEncoding.ASCII.GetBytes(reader.ReadToEnd());
				}

				envelope = PrepareEnvelope(payload, string.Empty, theMessage.Name);
			}
			catch
			{
				throw;
			}

			return envelope;
		}

		private void CreateQueue(string path)
		{
			if (Directory.Exists(path) == false)
			{
				Directory.CreateDirectory(path);
			}
		}

		// Transactions are not supported so here is where we hook into to remove the current message
		// if the transport is set to support tranactions....
		private void OnFileMessageCompleted(IEnvelope envelope, ITransport transport)
		{
			string file = this.CurrentMessage.FullName;
			string fileName = this.CurrentMessage.Name;

			this.CurrentMessage = null; // set to null to avoid locking issue on file

			System.IO.File.Delete(file);
		}

		private bool CanCompleteReceivePostAction(string fileName)
		{
			bool isPostReceiveActionCompleted = false;

			try
			{
				if (this.Endpoint.Properties.ContainsKey(FileEndpointOptions.MoveTo.ToString().ToLower()))
				{
					string path = this.Endpoint.Properties[FileEndpointOptions.MoveTo.ToString().ToLower()];
					if (string.IsNullOrEmpty(path) == false)
					{
						string moveToPath = Path.Combine(this.Endpoint.LocalizedEndpointUri, path);

						if (Directory.Exists(moveToPath) == false)
							Directory.CreateDirectory(moveToPath);

						System.IO.File.Move(fileName, moveToPath + @"\" + fileName);
					}
				}
			}
			finally
			{
			}

			return isPostReceiveActionCompleted;
		}
	}
}