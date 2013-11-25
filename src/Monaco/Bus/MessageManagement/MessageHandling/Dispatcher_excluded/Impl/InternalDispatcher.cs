using System;
using System.Linq;
using System.Reflection;
using Monaco.Exceptions;
using Monaco.Internals.Logging;
using Monaco.Internals.Reflection;
using Monaco.Persistance.Repositories;
using Monaco.Sagas;

namespace Monaco.Internals.Dispatcher.Impl
{
	public class SagaMessageDispatcher : IDispatcher
	{
		private IServiceBus _bus;
		private readonly ILogger _logger;
		private readonly ISagaRepository _sagaRepository;
		private readonly IReflection _reflection;

		public SagaMessageDispatcher(
			ILogger logger,
			ISagaRepository sagaRepository,
			IReflection reflection)
		{
			_logger = logger;
			_sagaRepository = sagaRepository;
			_reflection = reflection;
		}

		public void Dispatch(IServiceBus bus, IConsumer handler, IMessage message)
		{
			object theComponent = handler;
			_bus = bus;

			try
			{
				_logger.LogDebugMessage(string.Format("Start: dispatching message '{0}' to component '{1}'.",
					message.GetType().FullName, handler.GetType().FullName));

				if (theComponent != null)
				{
					if (typeof(ISaga).IsAssignableFrom(theComponent.GetType()))
					{
						ManageSagaInvocation(theComponent, message);
					}
					else
					{
						this.DispatchMessage(theComponent, message);
					}
				}
			}
			catch (Exception e)
			{
				var ex = new DispatcherDispatchException(message.GetType().FullName, handler.GetType().FullName, e);
				throw ex;
			}
			finally
			{
				_logger.LogDebugMessage(string.Format("Complete: dispatching message '{0}' to component '{1}'.",
						message.GetType().FullName, handler.GetType().FullName));
			}

		}

		private void ManageSagaInvocation(object theSaga, IMessage message)
		{
			if (IsStartOfNewSaga(theSaga, message) == true)
			{
				this.InitializeSaga(theSaga, message);
			}
			else
			{
				//NOTE: this works, but I am not satisfied with this...
				ISaga sagaFromPersistance = null; //_sagaRepository.Find(((ISaga)theSaga).InstanceId);

				if (sagaFromPersistance != null)
				{
					theSaga = sagaFromPersistance;
				}

				CheckForSagaMessageCorrelation(message, theSaga as Saga);
				this.ContinueSaga(theSaga, message);
			}

			this.DispatchMessage(theSaga, message);
			this.PersistSaga(theSaga);

		}

		private static bool IsStartOfNewSaga(object theSaga, IMessage message)
		{
			bool isStartingNewSaga = false;

			Type theStartedByInterfaceType = (from contract in theSaga.GetType().GetInterfaces()
											  where contract.IsGenericType == true
													&&
													contract.GetGenericArguments()[0] == message.GetType()
													&&
													contract.FullName.StartsWith(typeof(StartedBy<>).FullName)
											  select contract).FirstOrDefault();

			if (theStartedByInterfaceType != null)
			{
				isStartingNewSaga = true;
			}

			return isStartingNewSaga;
		}

		private void InitializeSaga(object theSaga, IMessage message)
		{
			Guid instanceId = Guid.NewGuid();
			((ISaga)theSaga).InstanceId = instanceId;

			if (ContainsInterface(theSaga, typeof(ISaga<>)) != null)
			{
				// need to instantiate new data for saga and sync the instance 
				// identifier with the instance identifier of the saga:
				PropertyInfo theSagaDataProperty = GetSagaDataProperty(theSaga);

				if (theSagaDataProperty != null)
				{
					object theSagaDataRepository =
						this.CreateSagaDataRepositoryFromDataType(theSagaDataProperty.PropertyType);

					// set the new data in the saga for use:
					ISagaData theSagaData = _reflection.InvokeCreateForSagaDataRepository(theSagaDataRepository,
																						  instanceId);
					theSagaData.CorrelationId = instanceId;
					theSagaDataProperty.SetValue(theSaga, theSagaData, null);
				}
			}
		}

		private void ContinueSaga(object theSaga, IMessage message)
		{
			// saga should be initialized at this point with
			// instance identifier and any optional saga data:
			ISagaData theSagaData = null;
			Guid instanceId = ((ISaga)theSaga).InstanceId;

			if (ContainsInterface(theSaga, typeof(ISaga<>)) != null)
			{
				// need to instantiate new data for saga and sync the instance 
				// identifier with the instance identifier of the saga:
				PropertyInfo theSagaDataProperty = GetSagaDataProperty(theSaga);

				if (theSagaDataProperty != null)
				{
					object theSagaDataRepository =
						this.CreateSagaDataRepositoryFromDataType(theSagaDataProperty.PropertyType);

					// set the data in the saga for use from the persistance store:
					theSagaData = _reflection.InvokeFindForSagaDataRepository(theSagaDataRepository, instanceId) as ISagaData;

					// create the data again if it can not be found:
					if (theSagaData == null)
					{
						theSagaData = _reflection.InvokeCreateForSagaDataRepository(theSagaDataRepository, instanceId);
						theSagaData.CorrelationId = instanceId;
					}

					theSagaDataProperty.SetValue(theSaga, theSagaData, null);
				}
			}

		}

		private static void CheckForSagaMessageCorrelation(IMessage message, Saga saga)
		{
			if (saga.Correlations.Count == 0) return;

			var correlation = saga.Correlations[message.GetType()];

			if (correlation == null) return;

			var result = correlation(message);

			if (result == false)
				throw new SagaMessageCouldNotBeCorrelatedToOngoingSagaException(message.GetType(),
																				saga.GetType());
		}

		[Obsolete]
		private static void CheckForSagaMessageCorrelation2(IMessage theMessage, Saga theSaga)
		{
			if (theSaga.Correlations.Count == 0) return;

			var correlation = theSaga.Correlations[theMessage.GetType()];

			if (correlation == null) return;

			var result = correlation(theMessage);

			if (result == false)
				throw new SagaMessageCouldNotBeCorrelatedToOngoingSagaException(theMessage.GetType(),
																				theSaga.GetType());

			// need to check for correlated messages to the saga:
			var theCorrelationInterface = (from theInterface in theSaga.GetType().GetInterfaces()
										   where
											   theInterface.FullName.StartsWith(
												   typeof(Correlated.By<>).FullName)
											   &&
											   theInterface.GetGenericArguments()[0] == theMessage.GetType()
										   select theInterface).FirstOrDefault();

			if (theCorrelationInterface == null) return;

			// must have method to conform to interface, proceed to call the "Correlate" function: 
			MethodInfo correlateMethod =
				new MessageToMethodMapper().Map(theSaga, theMessage, "Correlate");

			bool isCorrelatedForMessage = new MessageMethodInvoker()
				.Invoke<bool>(theSaga, correlateMethod, theMessage);

			if (isCorrelatedForMessage == false)
				throw new SagaMessageCouldNotBeCorrelatedToOngoingSagaException(theMessage.GetType(),
																				theSaga.GetType());
		}

		private void PersistSaga(object theSaga)
		{
			Guid instanceId = ((ISaga)theSaga).InstanceId;
			bool isCompleted = ((ISaga)theSaga).IsCompleted;

			PropertyInfo theSagaDataProperty = GetSagaDataProperty(theSaga);
			bool isDataPropertyAvailable = (theSagaDataProperty != null);

			ISagaData theSagaData = null;
			object theSagaFinder = null;

			if (isDataPropertyAvailable == true)
			{
				theSagaData = theSagaDataProperty.GetValue(theSaga, null) as ISagaData;
				theSagaFinder = this.CreateSagaDataRepositoryFromDataType(theSagaDataProperty.PropertyType);
			}

			if (isCompleted == true)
			{
				_sagaRepository.Remove(instanceId);

				if (isDataPropertyAvailable == true)
				{
					_reflection.InvokeRemoveForSagaDataRepository(theSagaFinder, instanceId);
				}
			}
			else
			{
				_sagaRepository.Save(theSaga as ISaga);

				if (isDataPropertyAvailable == true)
				{
					_reflection.InvokeSaveForSagaDataRepository(theSagaFinder, theSagaData);
				}
			}
		}

		private void DispatchMessage(object component, IMessage message)
		{
			this.SetServiceBus(component, false);

			MethodInfo consumerMethod =
				new MessageToMethodMapper().Map(component, message);

			if (consumerMethod != null)
			{
				new MessageMethodInvoker().Invoke(component, consumerMethod, message);
			}

			this.SetServiceBus(component, true);
		}

		private static PropertyInfo GetSagaDataProperty(object theSaga)
		{
			PropertyInfo theDataProperty = null;

			if (ContainsInterface(theSaga, typeof(ISaga<>)) != null)
			{
				theDataProperty = (from property in theSaga.GetType().GetProperties()
								   where property.Name.Trim().ToLower() == "data"
								   select property).FirstOrDefault();
			}
			return theDataProperty;
		}

		private void SetServiceBus(object component, bool canRemove)
		{
			PropertyInfo serviceBusProperty;

			if (typeof(Saga).IsAssignableFrom(component.GetType()))
			{
				if (canRemove == false)
				{
					((Saga)component).Bus = _bus;
				}
				else
				{
					((Saga)component).Bus = null;
				}
			}
			else
			{
				// setter injected bus instance:
				serviceBusProperty = (from property in component.GetType().GetProperties()
									  where property.PropertyType == typeof(IServiceBus)
									  select property).FirstOrDefault();

				if (serviceBusProperty != null)
				{
					if (canRemove == false)
					{
						serviceBusProperty.SetValue(_bus as IServiceBus, component, null);
					}
					else
					{
						serviceBusProperty.SetValue(null, component, null);
					}
				}
			}

		}

		private object CreateSagaDataRepositoryFromDataType(Type theSagaDataType)
		{
			object theSagaDataRepository = null;

			Type theType = typeof(ISagaDataRepository<>).MakeGenericType(theSagaDataType);

			if (theType != null)
			{
				theSagaDataRepository = _bus.Find(theType);
			}

			return theSagaDataRepository;
		}

		private static Type ContainsInterface(object theComponent, Type interfaceType)
		{
			Type theInterface = (from contract in theComponent.GetType().GetInterfaces()
								 where contract.IsGenericType == true
									   &&
									   contract.FullName.StartsWith(interfaceType.FullName)
								 select contract).FirstOrDefault();

			return theInterface;
		}
	}
}