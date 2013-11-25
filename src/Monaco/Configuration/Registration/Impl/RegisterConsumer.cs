using System;
using System.Collections.Generic;
using System.Linq;
using Castle.MicroKernel;
using Monaco.Bus.Internals;
using Monaco.Extensions;
using Monaco.StateMachine;

namespace Monaco.Configuration.Registration.Impl
{
	public class RegisterConsumer : IRegisterConsumer
	{
		private readonly IContainer container;

		public RegisterConsumer(IContainer container)
		{
			this.container = container;
		}

		#region IRegisterConsumer Members

		public IEnumerable<string> Register(object consumer)
		{
			return RegisterType(consumer.GetType());
		}

		public IEnumerable<string> RegisterType(Type consumer)
		{
			var keys = new List<string>();

			IEnumerable<Type> consumerInterfaces = consumer.GetInterfaces()
				   .Where(i => typeof(IConsumer).IsAssignableFrom(i))
				   .Select(i => i).ToList().Distinct();

			IEnumerable<Type> consumingMessageInterfaces =
					consumerInterfaces.Where(i => i.IsGenericType == true)
						.Distinct().ToList();

			container.Register(consumer);
			
			
			// TODO: this is bad but i think the container is only registering the first instance of the interface and consumer...

			// pull all interface message consumption actions and register 
			// them indenpendently in the underlying container by interface type:
			//if (consumingMessageInterfaces.Count() > 0)
			//{
			//    // register all implementations in the container, interface by interface for the consumer:
			//    foreach (Type @interface in consumingMessageInterfaces)
			//    {
			//        string key = string.Format("{0}-{1}", CombGuid.NewGuid(), @interface.FullName);
			//        container.Register(@interface, consumer);
			//        keys.AddUnique(key);
			//    }
			//}

			return keys;
		}

		#endregion
	}
}