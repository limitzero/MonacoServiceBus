using System;
using Monaco.Endpoint;
using Monaco.Endpoint.Impl;

namespace Monaco.Msmq.Transport
{
    public class MsmqEndpointBuilder : IEndpointBuilder<MsmqEndpoint>
    {
        public string Scheme { get; private set; }

        public MsmqEndpointBuilder()
        {
            this.Scheme = MsmqEndpointAddress.SCHEME;
        }

        public IEndpointBuilderSubscription CreateSubscription()
        {
            string description = "Builder scheme for sending and receiving messages via Microsoft Message Queue (MSMQ).";
            EndpointBuilderSubscription subscription = new EndpointBuilderSubscription(Scheme, this.GetType().AssemblyQualifiedName, description);
            return subscription;
        }

        public MsmqEndpoint Build(string uri)
        {
            string name = string.Format("MSMQ-{0}", Guid.NewGuid().ToString());
            return Build(name, uri);
        }

        public MsmqEndpoint Build(string name, string uri)
        {
            MsmqEndpointAddress address  = new MsmqEndpointAddress(uri);
            MsmqTransport transport = new MsmqTransport();
            MsmqEndpoint endpoint = new MsmqEndpoint(address, transport);
            
            return endpoint;
        }
    }
}