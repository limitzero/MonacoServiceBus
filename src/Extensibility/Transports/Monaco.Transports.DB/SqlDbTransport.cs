using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Transactions;
using Monaco.Bus;
using Monaco.Configuration;
using Monaco.Endpoint;
using Monaco.Transport;
using Monaco.Transports.DB.Configuration;
using Monaco.Transports.DB.Internals;

namespace Monaco.Transports.DB
{
	public class SqlDbTransport : BaseTransport<SqlQueueMessage>
	{
		private readonly IContainer container;
		private SqlQueueManager manager;
		private SqlConnection connection;

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

		public SqlDbTransport(IContainer container, SqlDbEndpoint endpoint)
			: base(endpoint)
		{
			this.container = container;
			this.IsTransactional = true;
			base.OnReceiveCompleted += OnSqlDbReceiveCompleted;
		}

		public override void OnDisposing()
		{
			this.OnReceiveCompleted -= OnSqlDbReceiveCompleted;
		}

		public override void Connect()
		{
			// initiate the connection to the SQL Server:
			if (Disposed || this.connection != null) return;

			// always localize the endpoint uri to the transport
			// specific implementation before creating the resource:
			this.Endpoint.Localize();

			try
			{
				ConnectToDBQueueStorage();

				if(this.connection != null)
				{
					this.connection.Open();
				}

				this.manager = new SqlQueueManager(this.GetSqlConfiguration());
				CreateDatabaseQueue(this.Endpoint.LocalizedEndpointUri);
			}
			catch
			{
			}
		}

		public override void Disconnect()
		{
			if(this.connection != null)
			{
				if(this.connection.State ==ConnectionState.Open)
				{
					this.connection.Close();
				}
			}
			this.connection = null;
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

				SqlQueueMessage message = null;

				if (this.TryPeek(out message, timeout) == true)
				{
					// got a message, go ahead and prepare it for receipt on the bus:
					envelope = this.PrepareForReceipt(message);
				}
			}
			catch (SqlException exception)
			{
				// do something here
			}

			return envelope;
		}

		public override void DoSend(IEnvelope envelope)
		{
			this.DoSend(this.Endpoint, envelope);
		}

		public override void DoSend(IEndpoint endpoint, IEnvelope envelope)
		{
			// create the database queue if it does not exist:
			CreateDatabaseQueue(endpoint.LocalizedEndpointUri);

			using (var txn = new TransactionScope(TransactionScopeOption.RequiresNew))
			{
				try
				{
					string message = ASCIIEncoding.ASCII.GetString(envelope.Body.GetStream());

					SqlCommand sendCommand = 
						this.manager.GetCommandForDBQueueMessageInsert(endpoint.LocalizedEndpointUri,
						message, envelope.Body.Label);

		            this.manager.ExecuteCommand(this.connection, sendCommand);

					txn.Complete();
				}
				catch (Exception sendException)
				{
					throw sendException;
				}
			}

		}

		private bool TryPeek(out SqlQueueMessage message, TimeSpan timeout)
		{
			message = null;
			int messageid = 0;
			SqlQueueMessage peekedMessage = null;

			try
			{
				this.ExecuteInTransaction(() =>
				                          	{
				                          		if(this.manager.TryPeekOnDBQueue(this.connection,
				                          		                                                    this.Endpoint.LocalizedEndpointUri,
				                          		                                                    out messageid,
				                          		                                                    (int) timeout.TotalSeconds))
				                          		{
													peekedMessage = this.manager.GetNextMessageById(this.connection,
																								 this.Endpoint.LocalizedEndpointUri,
																								 messageid);
				                          		}
				                          	});

				//message = this._manager.GetNextQueueMessage(this._connection,
				//        this.Endpoint.LocalizedEndpointUri,
				//        (int)timeout.TotalSeconds) ;
			}
			catch (SqlException tryPeekException)
			{
				//if (messageQueueException.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
				//{
				//    // could not peek a message in the given time, move on (no need to log this):
				//    //_logger.Warn("Could not peek a message from queue: " + _queue.QueueName + " in the given timeframe", 
				//    //    messageQueueException);
				//    return false;
				//}

				// nothing returned, move on:
				return false;
			}
			catch (Exception exception)
			{
				// could not peek a message, move on:
				//_logger.Warn("Could not peek a message from queue: " + _queue.QueueName, exception);
				return false;
			}

			message = peekedMessage;
			return message != null;
		}

		private IEnvelope PrepareForReceipt(SqlQueueMessage theMessage)
		{
			IEnvelope envelope = null;

			if (Disposed || theMessage == null) return envelope;

			this.CurrentMessage = theMessage;

			try
			{
				byte[] payload = ASCIIEncoding.ASCII.GetBytes(theMessage.Message);
				envelope = PrepareEnvelope(payload, theMessage.MessageLabel, theMessage.Id.ToString());
			}
			catch
			{
				throw;
			}

			return envelope;
		}

		public override void RemoveCurrentMessage()
		{
			if (this.CurrentMessage == null) return;

			this.Connect();

			this.ExecuteInTransaction(()=>
			                          	{
											SqlCommand deleteCommand = this.manager.GetCommandForDBQueueMessageDelete(this.CurrentMessage);
											this.manager.ExecuteCommand(this.connection, deleteCommand);
			                          	});
		}

		private void OnSqlDbReceiveCompleted(IEnvelope envelope, ITransport transport)
		{
			if (this.CurrentMessage != null)
			{
				this.ExecuteInTransaction(() =>
				                          	{
				                          		var command = this.manager.GetCommandForDBQueueMessageDelete(this.CurrentMessage);
				                          		this.manager.ExecuteCommand(this.connection, command);
				                          	});
			}
		}

		private void OnDBMessageDisposing(IEnvelope envelope, ITransport transport)
		{
			// check for transaction support before removing:
			if (this.IsTransactional == true)
			{

			}
		}

		private void ConnectToDBQueueStorage()
		{
			SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
			ISqlDbConfigurationSettings configuration = this.GetSqlConfiguration();

			builder.DataSource = configuration.Server;
			builder.InitialCatalog = configuration.Catalog;

			if (string.IsNullOrEmpty(configuration.UserName) == false
				&& string.IsNullOrEmpty(configuration.Password) == false)
			{
				builder.UserID = configuration.UserName;
				builder.Password = configuration.Password;
				builder.IntegratedSecurity = false;
			}
			else
			{
				builder.IntegratedSecurity = true;
			}

			this.connection = new SqlConnection(builder.ConnectionString);
		}

		private ISqlDbConfigurationSettings GetSqlConfiguration()
		{
			var configuration = this.container.Resolve<ISqlDbConfigurationSettings>();
			return configuration;
		}

		private void CreateDatabaseQueue(string queueName)
		{
			// the queueName will be the name of the table:
			string sql = this.manager.CreateQueueSQL(queueName);
			this.manager.ExecuteSql(this.connection, sql);
		}

		private void CheckForRecovery(bool recoverable)
		{
			if (recoverable == false)
			{
				this.Connect();

				this.ExecuteInTransaction(()=>
				                          	{
				                          		SqlCommand purgeCommand =
				                          			this.manager.GetCommandToPurgeDBMessageQueue(this.Endpoint.LocalizedEndpointUri);
												this.manager.ExecuteCommand(this.connection, purgeCommand);
				                          	});
			}
		}

		private void ExecuteInTransaction(Action statement)
		{
			using(var txn = new TransactionScope(TransactionScopeOption.RequiresNew))
			{
				try
				{
					statement.Invoke();
					txn.Complete();
				}
				catch (Exception executeInTransactionException)
				{
					throw executeInTransactionException;
				}
			}
		}

	}
}