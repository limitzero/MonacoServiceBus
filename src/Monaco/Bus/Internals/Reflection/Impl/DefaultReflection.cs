using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Castle.Core.Interceptor;
using Castle.DynamicProxy;
using Castle.MicroKernel;
using Monaco.Bus.Internals.Reflection.Internals.Impl;
using Monaco.Bus.MessageManagement.Dispatcher.Internal;
using Monaco.Configuration;
using Monaco.Extensibility.Logging;
using Monaco.StateMachine;
using Monaco.StateMachine.Roles;

namespace Monaco.Bus.Internals.Reflection.Impl
{
	public class DefaultReflection : IReflection
	{
		private readonly IContainer container;

		public DefaultReflection(IContainer container)
		{
			this.container = container;
		}

		/// <summary>
		/// This will construct an assembly representing all of the interface-based only 
		/// message implementations primarily for the serialization engine.
		/// </summary>
		/// <param name="contracts">Collection of interfaces representing messages</param>
		/// <param name="registerInContainer">Flag to indicate whether or not to add these components to the container.</param>
		public ICollection<Type> BuildProxyAssemblyForContracts(ICollection<Type> contracts, bool registerInContainer)
		{
			// this is adapted from http://msdn.microsoft.com/en-us/library/system.reflection.emit.propertybuilder.aspx
			var theProxiedTypes = new List<Type>();

			var assemblyName = new AssemblyName("proxyAssembly");
			AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName,
			                                                                                AssemblyBuilderAccess.RunAndSave);
			ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("proxyModule", Constants.PROXY_ASSEMBLY_NAME);
			TypeBuilder typeBuilder = null;

			foreach (Type contract in contracts)
			{
				var methods = new List<MethodInfo>(contract.GetMethods());

				if (contract.Name.StartsWith("I"))
				{
					string proxyName = contract.Name.Substring(1, contract.Name.Length - 1);
					typeBuilder = moduleBuilder.DefineType(proxyName, TypeAttributes.Public);
				}
				else
				{
					typeBuilder = moduleBuilder.DefineType(contract.Name + "Proxy", TypeAttributes.Public);
				}

				typeBuilder.AddInterfaceImplementation(contract);

				// for inheritance chains, get all of the methods for full implementation and subsequent interfaces:
				foreach (Type @interface in contract.GetInterfaces())
				{
					typeBuilder.AddInterfaceImplementation(@interface);

					foreach (MethodInfo method in @interface.GetMethods())
					{
						if (!methods.Contains(method))
						{
							methods.Add(method);
						}
					}
				}

				ConstructorBuilder ctorBuilder = typeBuilder.DefineConstructor(
					MethodAttributes.Public,
					CallingConventions.Standard,
					new Type[] {});

				ILGenerator ilGenerator = ctorBuilder.GetILGenerator();
				//ilGenerator.EmitWriteLine("Creating Proxy instance");
				ilGenerator.Emit(OpCodes.Ret);

				var fieldNames = new List<string>();

				// cycle through all of the methods and create the "set" and "get" methods:
				foreach (MethodInfo methodInfo in methods)
				{
					// build the backing field for the property based on the property name:
					string fieldName = string.Concat("m_", methodInfo.Name
					                                       	.Replace("set_", string.Empty)
					                                       	.Replace("get_", string.Empty))
						.Trim();

					// create the property name based on the set_XXX and get_XXX matching methods:
					string propertyName = fieldName.Replace("m_", string.Empty);

					if (!fieldNames.Contains(fieldName))
					{
						fieldNames.Add(fieldName);
						FieldBuilder fieldBuilder = typeBuilder.DefineField(fieldName.ToLower(),
						                                                    methodInfo.ReturnType,
						                                                    FieldAttributes.Private);

						// The last argument of DefineProperty is null, because the
						// property has no parameters. (If you don't specify null, you must
						// specify an array of Type objects. For a parameterless property,
						// use an array with no elements: new Type[] {})
						PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(propertyName,
						                                                             PropertyAttributes.HasDefault,
						                                                             methodInfo.ReturnType,
						                                                             null);

						// The property set and property get methods require a special
						// set of attributes.
						MethodAttributes getSetAttr =
							MethodAttributes.Public |
							MethodAttributes.HideBySig |
							MethodAttributes.SpecialName |
							MethodAttributes.NewSlot |
							MethodAttributes.Virtual |
							MethodAttributes.Final;

						// Define the "get" accessor method for the property.
						MethodBuilder getPropMethodBuilder =
							typeBuilder.DefineMethod(string.Concat("get_", propertyName),
							                         getSetAttr,
							                         methodInfo.ReturnType,
							                         Type.EmptyTypes);

						ILGenerator getPropMethodIL = getPropMethodBuilder.GetILGenerator();

						getPropMethodIL.Emit(OpCodes.Ldarg_0);
						getPropMethodIL.Emit(OpCodes.Ldfld, fieldBuilder);
						getPropMethodIL.Emit(OpCodes.Ret);

						// Define the "set" accessor method for the property.
						MethodBuilder setPropMethodBuilder =
							typeBuilder.DefineMethod(string.Concat("set_", propertyName),
							                         getSetAttr,
							                         null,
							                         new[] {methodInfo.ReturnType});

						ILGenerator setPropMethodIL = setPropMethodBuilder.GetILGenerator();

						setPropMethodIL.Emit(OpCodes.Ldarg_0);
						setPropMethodIL.Emit(OpCodes.Ldarg_1);
						setPropMethodIL.Emit(OpCodes.Stfld, fieldBuilder);
						setPropMethodIL.Emit(OpCodes.Ret);

						// Last, we must map the two methods created above to our PropertyBuilder to 
						// their corresponding behaviors, "get" and "set" respectively. 
						propertyBuilder.SetGetMethod(getPropMethodBuilder);
						propertyBuilder.SetSetMethod(setPropMethodBuilder);
					}
				}

				Type constructedType = typeBuilder.CreateType();
				theProxiedTypes.Add(constructedType);
			}

			try
			{
				assemblyBuilder.Save(Constants.PROXY_ASSEMBLY_NAME, PortableExecutableKinds.Required32Bit, ImageFileMachine.I386);
			}
			catch
			{
			}

			if (registerInContainer)
			{
				//Assembly asm = Assembly.LoadFile(filename);
				//_kernel.Register(AllTypes.FromAssembly(asm).Where(x => x.IsClass == true && typeof(IMessage).IsAssignableFrom(x))
				//            .WithService.FirstInterface());

				foreach (Type theProxiedType in theProxiedTypes)
				{
					try
					{
						object message = container.Resolve(theProxiedType);
					}
					catch
					{
						// add the message to the container with first interface:
						try
						{
							Type theInterface = theProxiedType.GetInterfaces()[0];
							container.Register(theInterface, theProxiedType);
						}
						catch
						{
							// redundant adding to container (?):
						}
					}
				}
			}

			return theProxiedTypes;
		}

		/// <summary>
		/// This will build a concrete proxy object based on the interface contract.
		/// </summary>
		/// <typeparam name="TCONTRACT">Interface for building a concrete instance</typeparam>
		/// <returns>
		///  A concrete instance of the {TCONTRACT}.
		/// </returns>
		public TCONTRACT BuildProxyFor<TCONTRACT>()
		{
			return GetProxy<TCONTRACT>();
		}

		public object BuildInstance(Type currentType)
		{
			object instance = null;

			try
			{
				instance = currentType.Assembly.CreateInstance(currentType.FullName);
			}
			catch (Exception)
			{
				string msg = string.Format("Could create the instance from the assembly '{0}' to create type '{1}'.",
				                           currentType.Assembly.FullName,
				                           currentType.FullName);
				throw;
			}

			return instance;
		}

		public object BuildInstance(string typeName)
		{
			object instance = null;

			string[] typeParts = typeName.Split(new[] {','});

			Assembly asm = LoadAssembly(typeParts[1].Trim());

			if (asm == null) return instance;

			try
			{
				instance = asm.CreateInstance(typeParts[0].Trim());
			}
			catch
			{
				string msg = string.Format("Could not create the type {0}.", typeParts[0]);
				//m_logger.Error(msg, exception);
				return instance;
			}

			return instance;
		}

		public void SetProperty(object theObject, string propertyName, object value)
		{
			PropertyInfo property = theObject.GetType().GetProperties().FirstOrDefault(x => x.Name == propertyName);

			if (property == null) return;

			property.SetValue(theObject, value, null);
		}

		public TData GetProperty<TData>(object consumer, string propertyName)
		{
			TData data = default(TData);

			PropertyInfo property = consumer.GetType()
				.GetProperties().FirstOrDefault(x => x.Name == propertyName);

			if (property == null) return data;

			try
			{
				data = (TData) property.GetValue(consumer, null);
			}
			catch
			{
			}

			return data;
		}

		public Type FindType(string typeName)
		{
			Type theType = null;
			string[] files = Directory.GetFiles(Environment.CurrentDirectory, "*.dll");

			foreach (string file in files)
			{
				Assembly asm = LoadAssemblyFromFile(file);

				if (asm == null) continue;

				List<Type> types = GetAssemblyTypes(asm);

				if (types.Count() == 0) continue;

				theType = types
					.Where(x => x.IsClass)
					.Where(x => x.IsAbstract == false)
					//.Where(x => x.FullName.Trim().ToLower() == typeName.Trim().ToLower())
					.Where(x => x.FullName.Trim().ToLower().StartsWith(typeName.Trim().ToLower()))
					.FirstOrDefault();

				if (theType != null)
				{
					break;
				}
			}

			return theType;
		}

		public Type FindConcreteTypeImplementingInterface(Type interfaceType, Assembly assemblyToScan)
		{
			Type retval = null;

			foreach (Type type in GetAssemblyTypes(assemblyToScan))
			{
				if (type.IsClass & !type.IsAbstract)
					if (interfaceType.IsAssignableFrom(type))
					{
						retval = type;
						break;
					}
			}

			return retval;
		}

		public Type[] FindConcreteTypesImplementingInterface(Type interfaceType, Assembly assemblyToScan)
		{
			var retval = new List<Type>();

			foreach (Type type in GetAssemblyTypes(assemblyToScan))
			{
				if (type.IsClass & !type.IsAbstract)
					if (interfaceType.IsAssignableFrom(type))
					{
						if (!retval.Contains(type))
							retval.Add(type);
					}
			}

			return retval.ToArray();
		}

		public object[] FindConcreteTypesImplementingInterfaceAndBuild(Type interfaceType, Assembly assemblyToScan)
		{
			var objects = new List<object>();
			Type[] types = FindConcreteTypesImplementingInterface(interfaceType, assemblyToScan);

			foreach (Type type in types)
			{
				if (type.IsAbstract) continue;
				objects.Add(BuildInstance(type.AssemblyQualifiedName));
			}

			return objects.ToArray();
		}

		public Type FindConcreteTypeForEndpointFactoryDefinition(string theFullyQualifiedType)
		{
			Type theType = null;

			string[] typeParts = theFullyQualifiedType.Split(new[] {','});

			Assembly asm = LoadAssembly(typeParts[1].Trim());

			if (asm == null) return theType;

			List<Type> theTypes = GetAssemblyTypes(asm);

			if (theTypes.Count() == 0) return theType;

			theType = (from type in theTypes
			           where type.FullName.StartsWith(typeParts[1].Trim())
			                 && type.IsClass
			                 && type.IsAbstract == false
			           select type).FirstOrDefault();

			return theType;
		}

		public object InvokeEndpointFactoryCreate(object factory, string endpointName, string endpointUri)
		{
			MethodInfo theMethod = factory.GetType().GetMethod("CreateEndpointWithName");
			object theEndpoint = theMethod.Invoke(factory, new object[] {endpointName, endpointUri});
			return theEndpoint;
		}

		public void InvokeRemoveForSagaDataRepository(object sagaFinder, Guid instanceId)
		{
			MethodInfo method = sagaFinder.GetType().GetMethod("Remove");
			method.Invoke(sagaFinder, new object[] {instanceId});
		}

		public void InvokeSaveForSagaDataRepository(object sagaFinder, IStateMachineData data)
		{
			MethodInfo method = sagaFinder.GetType().GetMethod("Save");
			method.Invoke(sagaFinder, new object[] {data});
		}

		public void InvokeHandleFaultForFaultHandler(object faultHandler, object faultMessage, IEnvelope envelope,
		                                             Exception exception = null)
		{
			SetProperty(faultHandler, "Envelope", envelope);
			SetProperty(faultHandler, "Exception", exception);

			MethodInfo method = new MessageToMethodMapper().Map(faultHandler, faultMessage);

			if (method != null)
			{
				new MessageMethodInvoker().Invoke(faultHandler, method, faultMessage);
			}
		}

		/// <summary>
		/// This will invoke the Find(...) method on the indicated <seealso cref="ISagaDataFinder{TSaga, TMessage}"/>
		/// and return the instance of the saga data for the indicated saga instance.
		/// </summary>
		/// <param name="sagaDataFinder">Object representing the saga data finder.</param>
		/// <param name="sagaMessage"></param>
		/// <returns></returns>
		public IStateMachineData InvokeFindForSagaDataFinder(object sagaDataFinder, IMessage sagaMessage)
		{
			MethodInfo method = sagaDataFinder.GetType().GetMethod("Find", new[] {sagaMessage.GetType()});
			return method.Invoke(sagaDataFinder, new object[] {sagaMessage}) as IStateMachineData;
		}

		public IEnumerable<IStateMachineData> InvokeStateMachineDataRepositoryFindAll(object persister)
		{
			MethodInfo method = persister.GetType().GetMethod("FindAll");
			return method.Invoke(persister, null) as IEnumerable<IStateMachineData>;
		}

		/// <summary>
		/// This will invoke the FindById(...) method on the indicated <seealso cref="ISagaDataFinder{TSaga, TMessage}"/>
		/// and return the instance of the saga data for the indicated saga instance by indentifier.
		/// </summary>
		/// <param name="persister">Object representing the saga data finder.</param>
		/// <param name="correlationId"></param>
		/// <returns></returns>
		public IStateMachineData InvokeStateMachineDataRepositoryFindById(object persister, Guid correlationId)
		{
			MethodInfo method = persister.GetType().GetMethod("Find", new[] {typeof (Guid)});
			return method.Invoke(persister, new object[] {correlationId}) as IStateMachineData;
		}

		public IStateMachineData InvokeStateMachineDataRepositoryFindByMessage(object persister, IMessage message)
		{
			MethodInfo method = persister.GetType().GetMethod("Find", new[] {message.GetType()});
			return method.Invoke(persister, new object[] {message}) as IStateMachineData;
		}

		public void InvokeStateMachineDataRepositoryRemove(object persister, IStateMachineData stateMachineData)
		{
			MethodInfo method = persister.GetType().GetMethod("Remove", new[] {stateMachineData.GetType()});
			method.Invoke(persister, new object[] {stateMachineData});
		}

		public void InvokeStateMachineDataRepositorySave(object persister, IStateMachineData stateMachineData)
		{
			MethodInfo method = persister.GetType().GetMethod("Save", new[] {stateMachineData.GetType()});
			method.Invoke(persister, new object[] {stateMachineData});
		}

		public IStateMachineData InvokeStateMachineDataMerge(object merger, IStateMachineData currentStateMachineData,
		                                                     IStateMachineData retreivedStateMachineData, IMessage sagaMessage)
		{
			MethodInfo method = merger.GetType().GetMethod("Merge", new[]
			                                                        	{
			                                                        		currentStateMachineData.GetType(),
			                                                        		retreivedStateMachineData.GetType(),
			                                                        		sagaMessage.GetType()
			                                                        	});

			object data = method.Invoke(merger, new object[] {currentStateMachineData, retreivedStateMachineData, sagaMessage});

			return data as IStateMachineData;
		}

		/// <summary>
		/// This will invoke the Find(...) message on the <see cref="ISagaFinder{TMessage"/>
		/// to return an instance of a state machine from the local persistance store based on the current message information.
		/// </summary>
		/// <param name="aStateMachineSagaFinder"></param>
		/// <param name="sagaMessage"></param>
		/// <returns></returns>
		public IStateMachine InvokeSagaStateMachineFinderByMessage(object aStateMachineSagaFinder, IMessage sagaMessage)
		{
			MethodInfo method = aStateMachineSagaFinder.GetType().GetMethod("Find", new[] {sagaMessage.GetType()});
			return method.Invoke(aStateMachineSagaFinder, new object[] {sagaMessage}) as IStateMachine;
		}

		/// <summary>
		/// This will invoke the Find(...) message on the <see cref="ISagaFinder{TSaga, TMessage"/>
		/// to return an instance of a saga from the local persistance store based on the current message information.
		/// </summary>
		/// <param name="aSagaFinder"></param>
		/// <param name="sagaMessage"></param>
		/// <returns></returns>
		public IStateMachine InvokeSagaFinderByMessage(object aSagaFinder, IMessage sagaMessage)
		{
			MethodInfo method = aSagaFinder.GetType().GetMethod("Find", new[] {sagaMessage.GetType()});
			return method.Invoke(aSagaFinder, new object[] {sagaMessage}) as IStateMachine;
		}

		public IStateMachine InvokeSagaRepositoryFind(object repository, Guid correlationId)
		{
			MethodInfo method = repository.GetType().GetMethod("Find");
			return method.Invoke(repository, new object[] {correlationId}) as IStateMachine;
		}

		public void InvokeSagaRepositorySave(object repository, IStateMachine stateMachine)
		{
			MethodInfo method = repository.GetType().GetMethod("Save");
			method.Invoke(repository, new object[] {stateMachine});
		}

		public void InvokeSagaRepositoryRemove(object repository, Guid correlationId)
		{
			MethodInfo method = repository.GetType().GetMethod("Remove", new[] {typeof (Guid)});
			method.Invoke(repository, new object[] {correlationId});
		}

		public TMessage CreateMessage<TMessage>()
		{
			TMessage message = default(TMessage);

			if (typeof (TMessage).IsInterface)
			{
				message = GetProxy<TMessage>();
			}
			else
			{
				message = (TMessage) typeof (TMessage).Assembly.CreateInstance(typeof (TMessage).FullName);
			}

			return message;
		}

		private static TMessage GetProxy<TMessage>()
		{
			var interfaceStorage = new InterfacePersistance();
			var interceptor = new InterfaceInterceptor(interfaceStorage);

			var proxyGenerator = new ProxyGenerator();
			object proxy = proxyGenerator.CreateInterfaceProxyWithoutTarget(typeof (TMessage), interceptor);
			return (TMessage) proxy;
		}

		private List<Type> GetAssemblyTypes(Assembly assembly)
		{
			var theTypes = new List<Type>();

			try
			{
				theTypes.AddRange(assembly.GetTypes());
			}
			catch
			{
			}

			return theTypes;
		}

		private Assembly LoadAssembly(string assemblyName)
		{
			Assembly target = null;
			var logger = container.Resolve<ILogger>();

			try
			{
				target = Assembly.Load(assemblyName);
			}
			catch (FileNotFoundException fileNotFoundException)
			{
				logger.LogErrorMessage("The following assembly " + assemblyName + " could not be found.", fileNotFoundException);
			}
			catch (FileLoadException fileLoadException)
			{
				logger.LogErrorMessage("The following assembly " + assemblyName + " could not be loaded.", fileLoadException);
			}

			return target;
		}

		private Assembly LoadAssemblyFromFile(string file)
		{
			Assembly target = null;
			var logger = container.Resolve<ILogger>();

			try
			{
				target = Assembly.LoadFile(file);
			}
			catch (FileNotFoundException fileNotFoundException)
			{
				logger.LogErrorMessage("The following assembly " + file + " could not be found.", fileNotFoundException);
			}
			catch (FileLoadException fileLoadException)
			{
				logger.LogErrorMessage("The following assembly " + file + " could not be loaded.", fileLoadException);
			}

			return target;
		}

		public static class Factory
		{
			public static IReflection CreateReflection()
			{
				//TODO: clean this up for static factory creation on mocks
				return new DefaultReflection(null);
			}
		}
	}


}