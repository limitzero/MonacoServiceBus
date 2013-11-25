using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Monaco.Bus.Internals.Collections;
using Monaco.Bus.MessageManagement.Callbacks;
using Monaco.Bus.Repositories;

namespace Monaco.Bus.Persistance.Callbacks
{
	/// <summary>
	/// Repository for callbacks that represent the request-reply scenario 
	/// with both the request and reply being optionally correlated on a given piece
	/// of information.
	/// </summary>
	public class LocalCallBackRepository : ICallBackRepository, IDisposable
	{
		private static readonly object _callbacksLock = new object();
		private static IThreadSafeDictionary<object, CorrelatedCallbackDataStructure> _callbacks;
		private readonly ReaderWriterLockSlim _callbacks_slim_lock = new ReaderWriterLockSlim();
		private bool _disposed;

		public LocalCallBackRepository()
		{
			if (_callbacks == null)
			{
				_callbacks = new ThreadSafeDictionary<object, CorrelatedCallbackDataStructure>();
			}
		}

		#region ICallBackRepository Members

		public void Register(ICallback callback)
		{
			CorrelatedCallbackDataStructure newDataStructure;
			CorrelatedCallbackDataStructure aDataStructure = null;
			object request = callback.RequestMessage;

			if (_disposed) return;

			if (IsCorrelatedMessage(request))
			{
				object correlationId = GetCorrelationIdentifier(request);
				newDataStructure = new CorrelatedCallbackDataStructure
				                   	{
				                   		Callback = callback,
				                   		CorrelationId = correlationId,
				                   		Request = request
				                   	};
			}
			else
			{
				newDataStructure = new CorrelatedCallbackDataStructure
				                   	{
				                   		Callback = callback,
				                   		Request = request
				                   	};
			}

			// read:
			_callbacks_slim_lock.EnterReadLock();
			try
			{
				_callbacks.TryGetValue(request, out aDataStructure);
			}
			catch
			{
			}
			finally
			{
				_callbacks_slim_lock.ExitReadLock();
			}

			// write
			if (aDataStructure == null)
			{
				_callbacks_slim_lock.EnterWriteLock();
				try
				{
					_callbacks.Add(request, newDataStructure);
				}
				catch
				{
				}
				finally
				{
					_callbacks_slim_lock.ExitWriteLock();
				}
			}
		}

		public void UnRegister(ICallback callback)
		{
			if (_disposed) return;

			lock (_callbacksLock)
			{
				if (_callbacks.ContainsKey(callback.RequestMessage))
				{
					_callbacks.Remove(callback.RequestMessage);
				}
			}
		}

		public ICallback Correlate(object request, object response)
		{
			ICallback callback = null;

			if (_disposed) return callback;

			// just return the data structure corresponding to the  type of the 
			// request message as a default request/response callback:
			KeyValuePair<object, CorrelatedCallbackDataStructure> theCallbackListing =
				_callbacks.Where(x => x.Key.GetType() == request.GetType()).FirstOrDefault();

			if (theCallbackListing.Value != null)
				callback = theCallbackListing.Value.Callback;

			// check the correlation of the request and response 
			// and make sure to coordinate the two for a successfull conversation:
			if (IsCorrelatedMessage(request) && IsCorrelatedMessage(response))
			{
				object requestCorrelationId = GetCorrelationIdentifier(request);
				object responseCorrelationId = GetCorrelationIdentifier(response);

				if (responseCorrelationId.Equals(requestCorrelationId))
				{
					CorrelatedCallbackDataStructure dataStructure = (from item in _callbacks
					                                                 let aDataStructure = item.Value
					                                                 where
					                                                 	aDataStructure.CorrelationId.ToString() ==
					                                                 	requestCorrelationId.ToString()
					                                                 select aDataStructure).FirstOrDefault();

					if (dataStructure != null)
					{
						callback = dataStructure.Callback;
					}
				}
			}

			return callback;
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

		private void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					if (_callbacks != null)
					{
						_callbacks.Clear();
					}

					_callbacks = null;
				}
				_disposed = true;
			}
		}

		private static bool IsCorrelatedMessage(object message)
		{
			Type correlatedInstance = (from @interface in message.GetType().GetInterfaces()
			                           where @interface.FullName.StartsWith(typeof (CorrelatedBy<>).FullName)
			                           select @interface).FirstOrDefault();
			return correlatedInstance != null;
		}

		private static object GetCorrelationIdentifier(object message)
		{
			object correlationIdentifier = null;

			PropertyInfo propertyType = (from property in message.GetType().GetProperties()
			                             where property.Name == typeof (CorrelatedBy<>).GetProperties()[0].Name
			                             select property).FirstOrDefault();

			if (propertyType != null)
			{
				correlationIdentifier = propertyType.GetValue(message, null);
			}

			return correlationIdentifier;
		}
	}

	//public class CallBackRepository2 : ICallBackRepository
	//{
	//    private static readonly object _callbacksLock = new object();
	//    private static IThreadSafeDictionary<Type, ICallback> _callbacks;

	//    public CallBackRepository2()
	//    {
	//        if (_callbacks == null)
	//        {
	//            _callbacks = new ThreadSafeDictionary<Type, ICallback>();
	//        }
	//    }

	//    ~CallBackRepository()
	//    {
	//        if(_callbacks != null)
	//        {
	//            _callbacks.Clear();
	//            _callbacks = null;
	//        }
	//    }

	//    public void Register(ICallback callback)
	//    {
	//        if(!_callbacks.ContainsKey(callback.RequestMessage))
	//        {
	//            lock(_callbacksLock)
	//            {
	//                _callbacks.Add(callback.RequestMessage, callback);
	//            }
	//        }
	//    }

	//    public ICallback Find(Type theMessageType)
	//    {
	//        ICallback callback = null;

	//        try
	//        {
	//            _callbacks.TryGetValue(theMessageType, out callback);
	//        }
	//        catch
	//        {
	//        }

	//        return callback;
	//    }
	//}
}