using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using Monaco.Transports.DB.Configuration;

namespace Monaco.Transports.DB.Internals
{
	/// <summary>
	/// This will create the neccessary SQL actions to manage a database queue.
	/// </summary>
	public class SqlQueueManager
	{
		private readonly ISqlDbConfigurationSettings configuration;

		public SqlQueueManager(ISqlDbConfigurationSettings configuration)
		{
			this.configuration = configuration;
		}

		public string CreateTableName(string queueName, bool includeBrackets = false)
		{
			string table = string.Format("{0}", queueName);

			if (includeBrackets)
				table = string.Format("[{0}]", table);

			return table;
		}

		public string CreatePrimaryKey(string queueName, bool includeBrackets = false)
		{
			string key = string.Format("{0}Id", queueName);

			if (includeBrackets)
				key = string.Format("[{0}]", key);

			return key;
		}

		public string GetNextMessageSQL(string queueName)
		{
			StringBuilder builder = new StringBuilder();
			builder.AppendFormat("select top 1 {0}.* from {0} order by dCreated DESC;", this.CreateTableName(queueName, true));
			return builder.ToString();
		}

		public string DropQueueSQL(string queueName)
		{
			StringBuilder builder = new StringBuilder();

			// build drop statement:
			builder.AppendFormat(
				"IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].{0}') AND type in (N'U'))",
				this.CreateTableName(queueName, true)).AppendLine()
				.AppendFormat("DROP TABLE [dbo].{0}", this.CreateTableName(queueName, true)).AppendLine()
				.AppendLine("GO");

			return builder.ToString();
		}

		public string CreateQueueSQL(string queueName)
		{
			StringBuilder builder = new StringBuilder();

			// build create statement:
			builder
				.AppendFormat(
					"IF  NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].{0}') AND type in (N'U'))",
					this.CreateTableName(queueName, true))
				.AppendLine()
				.AppendLine("BEGIN")
				.AppendFormat("CREATE TABLE [dbo].{0} (", CreateTableName(queueName, true)).AppendLine()
				.AppendLine("ID [int] IDENTITY(1,1) NOT NULL, ")
				.AppendLine("[xMessage] [xml] NOT NULL, ")
				.AppendLine("[dProcessed] [datetime] NULL, ")
				.AppendLine("[dCreated] [datetime] NULL, ")
				.AppendLine("[cLabel] [varchar] (255) NULL, ")
				.AppendFormat("CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED", CreatePrimaryKey(queueName)).AppendLine()
				.AppendLine("( ID ASC")
				.AppendLine(") WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, ")
				.AppendLine("IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]")
				.AppendLine(") ON [PRIMARY]")
				.Append("END");

			return builder.ToString();
		}

		public SqlCommand GetCommandToInsertQueueMessage(SqlQueueMessage message)
		{
			StringBuilder builder = new StringBuilder();
			builder.AppendFormat("INSERT INTO {0} (xMessage, dCreated, cLabel)",
								 this.CreateTableName(message.QueueName, true)).AppendLine()
				.AppendFormat("VALUES (@xMessage, @dCreated, @cLabel)");

			SqlCommand command = new SqlCommand();
			command.CommandText = builder.ToString();

			SqlParameter content = new SqlParameter("@xMessage", SqlDbType.Xml);
			content.Direction = ParameterDirection.Input;
			content.Value = message.Message;
			command.Parameters.Add(content);

			SqlParameter createdDateParameter = new SqlParameter("@dCreated", SqlDbType.DateTime);
			createdDateParameter.Direction = ParameterDirection.Input;
			createdDateParameter.IsNullable = true;
			createdDateParameter.Value = message.CreatedDate;
			command.Parameters.Add(createdDateParameter);

			SqlParameter labelParameter = new SqlParameter("@cLabel", SqlDbType.VarChar);
			labelParameter.Direction = ParameterDirection.Input;
			labelParameter.IsNullable = true;
			labelParameter.Value = string.IsNullOrEmpty(message.MessageLabel) ? null : message.MessageLabel;
			command.Parameters.Add(labelParameter);

			return command;
		}

		public void ExecuteSql(SqlConnection connection, string sql)
		{
			using (SqlCommand command = new SqlCommand(sql, connection))
			{
				command.ExecuteNonQuery();
			}
		}

		public int ExecuteCommandForNextMessageID(SqlConnection connection, string queue)
		{
			int messageId = 0;

			SqlCommand command = this.GetCommandForNextMessageID(queue);
			command.Connection = connection;

			messageId = (int)command.ExecuteScalar();

			return messageId;
		}

		/// <summary>
		/// This will execute a generated command with the supplied connection.
		/// </summary>
		/// <param name="connection"></param>
		/// <param name="command"></param>
		public void ExecuteCommand(SqlConnection connection, SqlCommand command)
		{
			command.Connection = connection;
			command.ExecuteNonQuery();
		}

		/// <summary>
		/// This will do a "peek" on the database message queue and return back the identifier of the next 
		/// message that can be pulled from the queue for consumption.
		/// </summary>
		/// <param name="connection"></param>
		/// <param name="queue"></param>
		/// <param name="messageid"></param>
		/// <param name="timeout"></param>
		/// <returns></returns>
		public bool TryPeekOnDBQueue(SqlConnection connection, string queue, out int messageid, int timeout = 0)
		{
			bool success = false;
			messageid = 0;

			SqlCommand tryPeekCommand = this.GetCommandForNextMessageID(queue);

			if (timeout > 0)
				tryPeekCommand.CommandTimeout = timeout;

			tryPeekCommand.Connection = connection;

			using (SqlDataReader reader = tryPeekCommand.ExecuteReader())
			{
				if (reader.HasRows)
				{
					reader.Read();

					int location = reader.GetOrdinal("ID");
					messageid = reader.GetInt32(location);
					success = messageid > 0;
				}
			}

			return success;
		}

		public SqlQueueMessage GetNextMessageById(SqlConnection connection, string queue, int messageId)
		{
			StringBuilder builder = new StringBuilder();
			builder.AppendFormat("SELECT {0}.* FROM {0} WHERE ID = @ID", this.CreateTableName(queue, true));

			SqlCommand command = new SqlCommand();
			command.CommandText = builder.ToString();
			command.CommandType = CommandType.Text;
			command.Connection = connection; 

			SqlParameter messageIdParam = new SqlParameter("@ID", SqlDbType.Int);
			messageIdParam.Direction = ParameterDirection.Input;
			messageIdParam.Value = messageId;
			command.Parameters.Add(messageIdParam);

			string message = string.Empty;
			string label = string.Empty;
			DateTime? created = null;
			DateTime? processed = null;

			using (command)
			{
				using (SqlDataReader reader = command.ExecuteReader())
				{
					if (reader.HasRows == false) return null;

					reader.Read();

					//Int32.TryParse(reader["ID"] as string, out id); -- will will assume that the message id is sufficient here
					message = reader["xMessage"] as string;
					label = reader["cLabel"] as string;

					if (Convert.IsDBNull(reader["dCreated"]) == false)
					{
						int location = reader.GetOrdinal("dCreated");
						created = reader.GetDateTime(location);
					}

					if (Convert.IsDBNull(reader["dProcessed"]) == false)
					{
						int location = reader.GetOrdinal("dProcessed");
						processed = reader.GetDateTime(location);
					}
				}
			}

			// create the new message from the sql queue:
			SqlQueueMessage queueMessage = new SqlQueueMessage(messageId, queue, message, created, processed);
			queueMessage.MessageLabel = label;

			return queueMessage;
		}



		/// <summary>
		/// This will get the next message from the database queue for consumption.
		/// </summary>
		/// <param name="connection"></param>
		/// <param name="queueName"></param>
		/// <param name="timeSpanInSeconds"></param>
		/// <returns></returns>
		public SqlQueueMessage GetNextQueueMessage(SqlConnection connection,
			string queueName,
			int timeSpanInSeconds = 0)
		{
			int id = 0;
			string message = string.Empty;
			string label = string.Empty;
			DateTime? created = null;
			DateTime? processed = null;

			using (SqlCommand command = this.GetCommandForDBQueueMessageSelect(queueName))
			{
				command.Connection = connection;

				if (timeSpanInSeconds > 0)
				{
					command.CommandTimeout = timeSpanInSeconds;
				}

				using (SqlDataReader reader = command.ExecuteReader())
				{
					if (reader.HasRows == false) return null;

					reader.Read();

					Int32.TryParse(reader["ID"] as string, out id);

					message = reader["xMessage"] as string;

					label = reader["cLabel"] as string;

					if (Convert.IsDBNull(reader["dCreated"]) == false)
					{
						int location = reader.GetOrdinal("dCreated");
						created = reader.GetDateTime(location);
					}

					if (Convert.IsDBNull(reader["dProcessed"]) == false)
					{
						int location = reader.GetOrdinal("dProcessed");
						processed = reader.GetDateTime(location);
					}
				}
			}

			SqlQueueMessage queueMessage = new SqlQueueMessage(id, queueName, message, created, processed);
			queueMessage.MessageLabel = label;

			return queueMessage;
		}

		/// <summary>
		/// This will create the command to get the next message in the database message queue.
		/// </summary>
		/// <param name="queue"></param>
		/// <returns></returns>
		public SqlCommand GetCommandForDBQueueMessageSelect(string queue)
		{
			SqlCommand command = new SqlCommand();
			StringBuilder builder = new StringBuilder();

			if (configuration.AutoDelete == true)
			{
				// no need to worry about previous processed messages:
				builder.AppendFormat("SELECT TOP 1 {0}.* FROM {0} ORDER BY ID ASC",
									 this.CreateTableName(queue, true));
			}
			else
			{
				// skip messages that are marked as 'processed' and get the rest:
				builder.AppendFormat("SELECT TOP 1 {0}.* FROM {0} WHERE dProcessed IS NULL ORDER BY ID ASC",
									 this.CreateTableName(queue, true));
			}

			command.CommandText = builder.ToString();
			command.CommandType = CommandType.Text;

			return command;
		}

		/// <summary>
		/// This will create the command to insert the message into the database message queue.
		/// </summary>
		/// <param name="queueName"></param>
		/// <param name="message"></param>
		/// <param name="label"></param>
		/// <returns></returns>
		public SqlCommand GetCommandForDBQueueMessageInsert(string queueName, string message, string label = "")
		{
			SqlCommand command = new SqlCommand();
			StringBuilder builder = new StringBuilder();

			builder.AppendFormat("INSERT INTO {0} (xMessage, dCreated, cLabel) VALUES (@xMessage, @dCreated, @cLabel)",
								 this.CreateTableName(queueName, true));

			command.CommandText = builder.ToString();
			command.CommandType = CommandType.Text;

			SqlParameter messageParam = new SqlParameter("@xMessage", SqlDbType.Xml);
			messageParam.Direction = ParameterDirection.Input;
			messageParam.Value = message;
			command.Parameters.Add(messageParam);

			SqlParameter createdParam = new SqlParameter("@dCreated", SqlDbType.DateTime);
			createdParam.IsNullable = true;
			createdParam.Direction = ParameterDirection.Input;
			createdParam.Value = DateTime.Now;
			command.Parameters.Add(createdParam);

			if (string.IsNullOrEmpty(label) == false)
			{
				SqlParameter labelParam = new SqlParameter("@cLabel", SqlDbType.VarChar);
				labelParam.IsNullable = true;
				labelParam.Direction = ParameterDirection.Input;
				labelParam.Value = label;
				command.Parameters.Add(labelParam);
			}

			return command;
		}

		/// <summary>
		/// This will forcibly remove the entire row from the sql database queue, if configured (auto.delete = true)
		/// </summary>
		/// <param name="queueMessage">Current <seealso cref="SqlQueueMessage">database queue message</seealso>to remove</param>
		/// <returns></returns>
		public SqlCommand GetCommandForDBQueueMessageDelete(SqlQueueMessage queueMessage)
		{
			SqlCommand command = new SqlCommand();
			StringBuilder builder = new StringBuilder();

			if (this.configuration.AutoDelete == true)
			{
				// forcibly remove the entire row:
				builder.AppendFormat("DELETE FROM {0} WHERE ID = @ID",
									 this.CreateTableName(queueMessage.QueueName, true));
				command.CommandText = builder.ToString();
				command.CommandType = CommandType.Text;

				SqlParameter idParam = new SqlParameter("@ID", SqlDbType.Int);
				idParam.Direction = ParameterDirection.Input;
				idParam.Value = queueMessage.Id;

				command.Parameters.Add(idParam);
			}

			return command;
		}

		/// <summary>
		/// This will create the command remove all of the records from the underlying queue table.
		/// </summary>
		/// <param name="queueName">Name of the queue table to remove all records.</param>
		public SqlCommand GetCommandToPurgeDBMessageQueue(string queueName)
		{
			SqlCommand command = new SqlCommand();
			command.CommandText = string.Format("DELETE FROM {0}", this.CreateTableName(queueName, true));
			command.CommandType = CommandType.Text;
			return command;
		}

		private SqlCommand GetCommandForNextMessageID(string queue)
		{
			SqlCommand command = new SqlCommand();
			StringBuilder sqlBuilder = new StringBuilder();

			sqlBuilder.AppendLine("DECLARE @NEXT_ID INT").AppendLine();
			sqlBuilder.AppendLine("SET @NEXT_ID = 0").AppendLine().AppendLine();
			sqlBuilder.AppendLine("SET NOCOUNT ON").AppendLine();

			sqlBuilder.AppendFormat("SELECT TOP 1 @NEXT_ID = ID FROM {0} (UPDLOCK) WHERE dProcessed IS NULL ORDER BY ID ASC",
									this.CreateTableName(queue, true)).AppendLine().AppendLine();

			sqlBuilder.AppendLine("IF (@NEXT_ID IS NOT NULL OR @NEXT_ID > 0)").AppendLine();
			sqlBuilder.Append("BEGIN").AppendLine();
			sqlBuilder.AppendFormat("UPDATE {0}  SET dProcessed = GETDATE() WHERE ID = @NEXT_ID",
									this.CreateTableName(queue, true)).AppendLine();
			sqlBuilder.Append("END").AppendLine().AppendLine();

			sqlBuilder.AppendLine("SELECT ID = @NEXT_ID").AppendLine();

			command.CommandText = sqlBuilder.ToString();
			command.CommandType = CommandType.Text;

			return command;
		}
	}
}