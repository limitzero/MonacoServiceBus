using System;
using System.Collections.Generic;
using Castle.DynamicProxy;
using Monaco.Bus.Internals;
using Monaco.Bus.Internals.Eventing;
using Monaco.Bus.MessageManagement.Callbacks;
using Monaco.Configuration;
using Monaco.Configuration.Endpoint;
using Monaco.Endpoint;
using Monaco.Endpoint.Impl.Control;
using Monaco.Testing.Internals.Interceptors.Impl;

namespace Monaco.Testing.Internals.Invocations
{
	public class MockServiceBus : IServiceBus
	{
		public MockServiceBus()
		{
			ComponentNotificationEvent += OnComponentNotification;
		}

		#region IServiceBus Members

		public virtual TComponent Find<TComponent>()
		{
			return default(TComponent);
		}

		public object Find(Type component)
		{
			return null;
		}

		public virtual ICollection<TComponent> FindAll<TComponent>()
		{
			return new List<TComponent>();
		}

		public virtual void Publish<TMessage>() where TMessage : IMessage
		{
		}

		public virtual void Publish<TMessage>(Action<TMessage> action) where TMessage : class, IMessage, new()
		{
		}

		public virtual void Publish(IMessage message)
		{
		}

		public virtual void Publish(params object[] messages)
		{
		}

		public virtual void Notify(params object[] messages)
		{
		}

		public virtual void Notify<TMessage>(Action<TMessage> action) where TMessage : class, IMessage, new()
		{
		}

		public virtual ICallback Send<TMessage>() where TMessage : IMessage
		{
			return null;
		}

		public virtual ICallback Send(params object[] message)
		{
			return null;
		}

		public virtual ICallback Send(Uri endpoint, params object[] messages)
		{
			return null;
		}

		public virtual ICallback Send(IMessage message)
		{
			return null;
		}

		public virtual ICallback Send<TMessage>(Action<TMessage> action) where TMessage : class, IMessage, new()
		{
			return null;
		}

		public virtual void Reply(object message)
		{
		}

		public virtual void Reply<TMessage>(Action<TMessage> action) where TMessage : class, IMessage, new()
		{
		}

		public virtual void HandleMessageLater(TimeSpan delay, IMessage message)
		{
		}

		public virtual IDisposableAction AddInstanceConsumer<TConsumer>() where TConsumer : IConsumer
		{
			return null;
		}

		public virtual IDisposableAction AddInstanceConsumer(object instance)
		{
			return null;
		}

		public virtual void ConsumeMessages(params IMessage[] messages)
		{
		}

		public virtual void ConfiguredWithEndpoint<TEndpointConfiguration>()
			where TEndpointConfiguration : class, ICanConfigureEndpoint, new()
		{
		}

		public virtual void ConfiguredWithEndpoint(Type endpointConfigurationType)
		{
		}

		public virtual IEndpoint Endpoint { get; set; }

		public virtual TMessage CreateMessage<TMessage>()
		{
			var interfaceStorage = new InterfacePersistance();
			var interceptor = new InterfaceInterceptor(interfaceStorage);

			var proxyGenerator = new ProxyGenerator();
			object proxy = proxyGenerator.CreateInterfaceProxyWithoutTarget(typeof (TMessage), interceptor);
			return (TMessage) proxy;
		}

		public TMessage CreateMessage<TMessage>(Action<TMessage> create)
		{
			var message = this.CreateMessage<TMessage>();
			create(message);
			return message;
		}

		public void SetEndpoint(IEndpoint endpoint)
		{
			this.Endpoint = endpoint;
		}

		public void Dispose()
		{
		}

		public bool IsRunning { get; private set; }

		public void Start()
		{
		}

		public void Stop()
		{
		}

		public event EventHandler<ComponentNotificationEventArgs> ComponentNotificationEvent;
		public Action<string> OnStart { get; set; }
		public Action<string> OnStop { get; set; }

		public virtual IServiceAsyncRequest EnqueueRequest()
		{
			return null;
		}

		public virtual IControlEndpoint GetControlEndpoint()
		{
			return null;
		}

		public virtual void CompleteAsyncRequestFor<TMessage>(IMessage response) where TMessage : class, IMessage, new()
		{
		}

		#endregion

		private void OnComponentNotification(object sender, ComponentNotificationEventArgs e)
		{
			if (ComponentNotificationEvent != null)
			{
				ComponentNotificationEvent(this, new ComponentNotificationEventArgs(string.Empty));
			}
		}
	}
}