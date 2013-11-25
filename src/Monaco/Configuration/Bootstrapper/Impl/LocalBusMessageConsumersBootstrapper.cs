using System;
using System.Collections.Generic;
using System.Linq;
using Monaco.Bus.Internals;
using Monaco.Configuration.Bootstrapper.Roles;
using Monaco.Configuration.Registration;

namespace Monaco.Configuration.Bootstrapper.Impl
{
	public class LocalBusMessageConsumersBootstrapper : BaseInternalBootstrapper
	{
		public override void Configure()
		{
			IRegisterConsumer consumerRegistrar = null;

			IEnumerable<Type> consumers = (from consumer in GetType().Assembly.GetTypes()
			                               where consumer.IsClass
			                                     && consumer.IsAbstract == false
			                                     && typeof (IConsumer).IsAssignableFrom(consumer)
			                               select consumer).ToList().Distinct();

			if (consumers.Count() > 0)
			{
				consumerRegistrar = Container.Resolve<IRegisterConsumer>();
			}

			if (consumerRegistrar != null)
			{
				consumers.ToList().ForEach(consumer => consumerRegistrar.RegisterType(consumer));
			}
		}
	}
}