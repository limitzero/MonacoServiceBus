using System;

namespace Monaco.Bus.Internals.Eventing
{
	public class EndpointErrorReceivedEventArgs : EventArgs
	{
		//public IEndpoint Endpoint { get; set; }
		public Exception Exception { get; set; }

		//public EndpointErrorReceivedEventArgs(IEndpoint endpoint, Exception exception)
		//{
		//    Endpoint = endpoint;
		//    Exception = exception;
		//}
	}
}