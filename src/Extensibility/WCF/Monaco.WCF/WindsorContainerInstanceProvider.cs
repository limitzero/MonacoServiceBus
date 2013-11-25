using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using Castle.MicroKernel;

namespace Monaco.WCF
{
	/// <summary>
	/// Class that will retrieve the instance of the component from the underlying 
	/// container base on requesting type from service host behavior.
	/// </summary>
	internal class WindsorContainerInstanceProvider : IInstanceProvider
	{
		private readonly Type _contractType;
		private readonly IKernel _kernel;

		public WindsorContainerInstanceProvider(IKernel kernel, Type contractType)
		{
			_kernel = kernel;
			_contractType = contractType;
		}

		public object GetInstance(InstanceContext instanceContext)
		{
			return GetInstance(instanceContext, null);
		}

		public object GetInstance(InstanceContext instanceContext, Message message)
		{
			return _kernel.Resolve(_contractType);
		}

		public void ReleaseInstance(InstanceContext instanceContext, object instance)
		{
			_kernel.ReleaseComponent(instance);
		}
	}
}