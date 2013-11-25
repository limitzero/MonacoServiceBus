using System.Net;
using System.Net.Sockets;
using RabbitMQ.Client;

namespace Monaco.Transports.RabbitMQ
{
	public class RabbitMQCredential
	{
		public string Host { get; private set; }
		public string Exchange { get; private set; }
		public string Queue { get; private set; }
		public string User { get; private set; }
		public string Password { get; private set; }
		public string Protocol { get; private set; }
		public int Port { get; private set; }
		
			public RabbitMQCredential(string host, string exchange, string queue, string user, 
				string password = "")
				:this(host, exchange, queue, user, password, Protocols.DefaultProtocol.ApiName, AmqpTcpEndpoint.UseDefaultPort)
			{

			}

		public RabbitMQCredential(string host, string exchange, string queue, string user, 
			string password = "",
			string protocol = "", 
			int port = 0)
		{
			this.Host = host;
			this.Exchange = exchange;
			this.Queue = queue;
			this.User = user;
			this.Password = password;
			this.Protocol = protocol;

			this.Port = port;
			this.GetFreePort();
		}

		private void GetFreePort()
		{
			if (this.Port == 0)
			{
				Socket sock = new Socket(AddressFamily.InterNetwork,
				                         SocketType.Stream, ProtocolType.Tcp);
				sock.Bind(new IPEndPoint(IPAddress.Parse("121.0.0.1"), 0)); // Pass 0 here.

				this.Port = ((IPEndPoint) sock.LocalEndPoint).Port;
			}
		}
	}
}