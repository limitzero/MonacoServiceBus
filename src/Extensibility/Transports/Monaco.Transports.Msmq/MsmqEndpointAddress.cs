using System;
using Monaco.Endpoint;
using Monaco.Exceptions;

namespace Monaco.Msmq.Transport
{
    /// <summary>
    /// Endpoint address parser for MSMQ based endpoints. 
    /// Scheme: msmq://{server name}/{queue name}
    /// Ex: msmq://localhost/myQueue
    /// </summary>
    public class MsmqEndpointAddress : IEndpointAddress
    {
        public static readonly string SCHEME = "msmq";
        public static readonly string PROTOCOL = SCHEME + @"://";

        private const string _endpointAddressFormat = "msmq://{server or ip address}/{msmq name}";
        private const string _scheme = "msmq";
        private const string _poision = ".poision";
        private string _uri = string.Empty;

        public string Uri { get; private set; }

        public MsmqEndpointAddress(string address)
        {
            this.ValidateAddress(address);
        }

        private void ValidateAddress(string address)
        {
            System.Uri theUri = null;

            // for native addresses, do not validate:
            if (address.ToLower().Contains(@"\private$"))
            {
                this.Uri = address;
                return;
            }

            try
            {
                theUri = new Uri(address);
            }
            catch (Exception ex)
            {
                throw new InvalidEndpointUriAddressException(address, _endpointAddressFormat, ex);
            }

            if (theUri.Scheme != _scheme)
            {
                throw new InvalidEndpointUriAddressException(address, _endpointAddressFormat);
            }

            string thePath = address.Replace(PROTOCOL, string.Empty);
            string[] theParts = thePath.Split(new char[] { '/' });

            if (theParts.Length != 2)
            {
                throw new InvalidEndpointUriAddressException(address, _endpointAddressFormat);
            }

            //this.Uri = this.Normalize(address);
            //_uri = this.Uri;
            this.Uri = address;
        }

        private string Normalize(string address)
        {
            string path = string.Empty;
            string result = @"FormatName:DIRECT=OS:{0}\Private$\{1}";

            string thePath = address.Replace(string.Concat(_scheme, "://"), string.Empty);
            string[] theParts = thePath.Split(new char[] { '/' });

            string server = theParts[0].Trim().ToLower() == "localhost"
                                ? "."
                                : theParts[0].Trim().ToUpper();

            string queue = theParts[1].Trim().ToLower();

            path = string.Format(@"{0}\Private$\{1}", server, queue);

            return path;
        }
    }
}