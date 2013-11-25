using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Castle.DynamicProxy;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Monaco.Bus;
using Monaco.Bus.Agents.Scheduler;
using Monaco.Bus.Internals.Reflection;
using Monaco.Bus.Internals.Reflection.Impl;
using Monaco.Bus.MessageManagement.MessageHandling.Dispatching.ToStateMachine;
using Monaco.Bus.MessageManagement.Serialization;
using Monaco.Bus.MessageManagement.Serialization.Impl;
using Monaco.Endpoint.Factory;
using Monaco.Infrastructure.Logging;
using Monaco.Infrastructure.Logging.Impl;
using Monaco.Persistance.Repositories;
using Monaco.Persistance.Subscriptions;
using Monaco.Testing.Internals.Interceptors.Impl;
using Monaco.Testing.StateMachines;
using Monaco.Transport;
using Monaco.Transport.Virtual;

namespace Monaco.Sagas.StateMachine.Verbalizer
{
	/// <summary>
	/// Class to inspect the current saga state machine and create the scenario text describing its behavior
	/// </summary>
	public class SagaStateMachineVerbalizer
	{
		public string Verbalize<TSagaStateMachine>() where TSagaStateMachine : StateMachine, new()
		{
			return this.Verbalize(new TSagaStateMachine());
		}

		public string Verbalize<TSagaStateMachine>(TSagaStateMachine sagaStateMachine)
			where TSagaStateMachine : StateMachine
		{
			string results = string.Empty;

			// make sure to use a mock of the service bus to avoid actually running the defined conditions:
			var kernel = BuildMockedServiceBus();
			RegisterInterfaceBasedMessages(kernel, sagaStateMachine);
			RegisterConcreteMessages(kernel);
			var bus = kernel.Resolve<IServiceBus>();

			sagaStateMachine.Bus = bus;
			sagaStateMachine.Define();

			using (var stream = new MemoryStream())
			{
				string name = sagaStateMachine.Name ?? sagaStateMachine.GetType().Name;
				var trace = new System.Diagnostics.TextWriterTraceListener(stream);
				var preamble = "Configuration for saga state machine : " + name;
				var separator = string.Empty;
				foreach (var c in preamble)
					separator += "=";

				trace.IndentSize = 2;
				trace.IndentLevel = 0;

				trace.WriteLine(preamble);
				trace.WriteLine(separator);

				// define the portion for "Initially" segment:
				var initially = sagaStateMachine.TriggerConditions
					.Where(x => x.Stage == SagaStage.Initially).SingleOrDefault();

				//WriteInitiallyPart(trace, initially);
				WriteEvents(SagaStage.Initially, trace, initially.Condition);

				trace.WriteLine(string.Empty);

				// define the portions for the "While" and "Also" segments:
				var segments = sagaStateMachine.TriggerConditions
					.Where(x => x.Stage != SagaStage.Initially).ToList();

				foreach (var segment in segments)
				{
					WriteEvents(segment.Stage, trace, segment.Condition);
					trace.WriteLine(string.Empty);
					//WriteWhilePart(trace, part.Value);
				}

				trace.Flush();
				stream.Seek(0, SeekOrigin.Begin);

				using (TextReader reader = new StreamReader(stream))
				{
					results = reader.ReadToEnd();
				}
			}

			return results;
		}

		private static IKernel BuildMockedServiceBus()
		{
			IKernel kernel = new DefaultKernel();

			kernel.Register(Component.For<ISerializationProvider>()
											.ImplementedBy<SharpSerializationProvider>()
											.LifeStyle.Transient);

			kernel.Register(Component.For<IReflection>()
								.ImplementedBy<DefaultReflection>());

			kernel.Register(Component.For<IServiceBus>()
								.ImplementedBy<DefaultServiceBus>());

			kernel.Register(Component.For<ISagaStateMachineMessageDispatcher>()
					.ImplementedBy<SagaStateMachineMessageDispatcher>());

			kernel.Register(Component.For<IScheduler>()
								.ImplementedBy<Scheduler>()
								.LifeStyle.Singleton);

			kernel.Register(Component.For<ILogger>().
								ImplementedBy<NullLogger>());

			kernel.Register(Component.For<ISubscriptionRepository>()
								.ImplementedBy<InMemorySubscriptionRepository>());

			kernel.Register(Component.For<IEndpointFactory>()
							.ImplementedBy<EndpointFactory>()
							.LifeStyle.Singleton);

			// register the default transport:
			kernel.Resolve<IEndpointFactory>().Register(new VirtualEndpointTransportRegistration());

			var virtualTransport = kernel.Resolve<IEndpointFactory>().Build(new Uri("vm://mock")).Transport;

			kernel.AddComponentInstance(typeof(ITransport).Name, typeof(ITransport), virtualTransport);

			return kernel;
		}

		private static void RegisterInterfaceBasedMessages(IKernel kernel, StateMachine stateMachine)
		{
			var toRegister = new List<Type>();

			var interfaces =stateMachine.GetType().GetInterfaces()
				.Where(i => i.Name.StartsWith(typeof(StartedBy<>).Name) || i.Name.StartsWith(typeof(OrchestratedBy<>).Name)).
				ToList().Distinct();

			foreach (var @interface in interfaces)
			{
				var element = GetGenericElement(@interface);

				if (element != null & element.IsInterface == true)
					toRegister.Add(element);
			}

			foreach (var @interface in toRegister)
			{
				var interfaceStorage = new InterfacePersistance();
				var interceptor = new InterfaceInterceptor(interfaceStorage);

				var proxyGenerator = new ProxyGenerator();
				var proxy = proxyGenerator.CreateInterfaceProxyWithoutTarget(@interface, interceptor);

				try
				{
					kernel.AddComponentInstance(@interface.Name, @interface, proxy);
				}
				catch 
				{
				}
				
				//_kernel.AddComponent(@interface.Name, @interface, proxy.GetType(), LifestyleType.Transient);
			}
			//kernel.Resolve<IReflection>().BuildProxyAssemblyForContracts(toRegister, true);
		}

		private static void RegisterConcreteMessages(IKernel kernel)
		{
			var files = Directory.GetFiles(System.AppDomain.CurrentDomain.BaseDirectory, "*.dll");

			foreach (var file in files)
			{
				try
				{
					var asm = Assembly.LoadFile(file);

					kernel.Register(
						AllTypes.FromAssembly(asm).Where(x => x.IsClass && x.IsAbstract == false && typeof (IMessage).IsAssignableFrom(x)));

				}
				catch
				{
					continue;
				}
			}
		}

		private static Type GetGenericElement(Type @interface)
		{
			Type element = null;

			if (@interface.IsGenericType)
			{
				element = @interface.GetGenericArguments()[0];
			}

			return element;
		}

		private static void WriteEvents(SagaStage sagaStage, TextWriterTraceListener trace,
			ISagaEventTriggerCondition condition)
		{

			foreach (var messageAction in condition.MessageActions)
			{

				/* Initially when the [message] arrives, it will 
				*		[do, publish, send, reply, delay, execute]  
				*	 then  transition to [state]	|| then complete				
				*/

				if (sagaStage == SagaStage.Initially &&
					messageAction.ActionType == SagaMessageActionType.When)
				{
					trace.IndentLevel = 0;
					trace.WriteLine(string.Format("Initially when the '{0}' message arrives, it will ",
						ScrubMessageName(messageAction.Message.GetType())));

				}

				if (sagaStage == SagaStage.While &&
					 messageAction.ActionType == SagaMessageActionType.When)
				{

					/* While in state {state},  when (message arrives) it will 
					*		[do, publish, send, reply, delay, execute]  
					*	 then  transition to [state]	|| then complete				
					*/

					trace.IndentLevel = 0;
					trace.WriteLine(string.Format("While in state '{0}', when the '{1}' message arrives, it will ",
												  condition.State.Name,
												ScrubMessageName(messageAction.Message.GetType())));

				}

				if (sagaStage == SagaStage.Also &&
					 messageAction.ActionType == SagaMessageActionType.When)
				{

					/* Also for any state, when (message arrives) it will 
					*		[do, publish, send, reply, delay, execute]  
					*	 then  transition to [state] || then complete				
					*/

					trace.IndentLevel = 0;
					trace.WriteLine(string.Format("Also for any state, when the '{0}' message arrives, it will ",
												 ScrubMessageName(messageAction.Message.GetType())));

				}

				switch (messageAction.ActionType)
				{
					// these are actions that can be taken for the current event (as defined by the message):
					case (SagaMessageActionType.Do):
						trace.IndentLevel = 1;

						if(string.IsNullOrEmpty(messageAction.Note) == false)
						{
							trace.WriteLine(messageAction.Note);
						}
						else
						{
							trace.WriteLine("execute some custom code");	
						}
						
						trace.IndentLevel = 0;

						break;

					case (SagaMessageActionType.Publish):
						trace.IndentLevel = 1;
						trace.WriteLine(string.Format("publish the message '{0}' ",
							ScrubMessageName(messageAction.Message.GetType())));
						trace.IndentLevel = 0;

						break;

					case (SagaMessageActionType.Send):
						trace.IndentLevel = 1;
						trace.WriteLine(string.Format("send the message '{0}' ",
							ScrubMessageName(messageAction.Message.GetType())));
						trace.IndentLevel = 0;

						break;

					case (SagaMessageActionType.SendToEndpoint):
						trace.IndentLevel = 1;
						trace.WriteLine(string.Format("send the message '{0}' to endpoint '{1}'",
													  ScrubMessageName(messageAction.Message.GetType()),
													  messageAction.Endpoint));
						trace.IndentLevel = 0;

						break;

					case (SagaMessageActionType.Reply):
						trace.IndentLevel = 1;
						trace.WriteLine(string.Format("reply with the message '{0}' ",
							ScrubMessageName(messageAction.Message.GetType())));
						trace.IndentLevel = 0;

						break;

					case (SagaMessageActionType.Delay):
						trace.IndentLevel = 1;
						trace.WriteLine(string.Format("delay publishing of the message '{0}' by '{1}' days, '{2}' hours, '{3}' minutes, and '{4}' seconds",
													  ScrubMessageName(messageAction.Message.GetType()),
													  messageAction.Delay.Days,
													  messageAction.Delay.Hours,
													  messageAction.Delay.Minutes,
													  messageAction.Delay.Seconds
											));
						trace.IndentLevel = 0;

						break;

					// transition and complete are finalization markers for the current event:
					case (SagaMessageActionType.Transition):

						trace.IndentLevel = 0;

						var transistionStatement = string.Empty;

						if (condition.MessageActions.Count == 2)
						{
							transistionStatement = string.Format("transition to state '{0}' ", messageAction.State.Name);
						}
						else
						{
							transistionStatement = string.Format("then transition to state '{0}' ", messageAction.State.Name);
						}

						trace.WriteLine(transistionStatement);

						break;

					case (SagaMessageActionType.Complete):

						trace.IndentLevel = 0;

						var completionStatement = string.Empty;

						if (condition.MessageActions.Count == 2)
						{
							completionStatement = "complete";
						}
						else
						{
							completionStatement = "then complete";
						}

						trace.WriteLine(completionStatement);

						break;

				
				}

			}
		}

		private static string ScrubMessageName(Type message)
		{
			string result = message.Name;

			if(message.Name.EndsWith("Proxy"))
			{
				result = message.Name.Replace("Proxy", string.Empty);
			}

			return result;
		}
	}
}