using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Castle.MicroKernel.Registration;
using Monaco.Bus.Internals;
using Monaco.Configuration.Bootstrapper.Roles;
using Monaco.Extensibility.Storage.Subscriptions;
using Monaco.Subscriptions;
using Monaco.Subscriptions.Impl;

namespace Monaco.Configuration.Bootstrapper.Impl
{
	public class LocalSubscriptionsBootstrapper : BaseInternalBootstrapper
	{
		public LocalSubscriptionsBootstrapper()
		{
			IsActive = false;
		}

		public override void Configure()
		{
			string[] files = Directory.GetFiles(Environment.CurrentDirectory, "*.dll");

			foreach (string file in files)
			{
				try
				{
					Assembly asm = Assembly.LoadFile(file);

					//Container.Register(AllTypes.FromAssembly(asm)
					//                    .Where(
					//                        x =>
					//                        x.IsClass && x.IsAbstract == false &&
					//                        typeof (IConsumer).IsAssignableFrom(x)));
				}
				catch
				{
					continue;
				}
			}

			//ICollection<IConsumer> theConsumers = Kernel.ResolveAll<IConsumer>().ToList();

			//if (theConsumers.Count > 0)
			//{
			//    BuildSubscriptionsForConsumers(theConsumers);
			//}
		}

		private void BuildSubscriptionsForConsumers(IEnumerable<IConsumer> consumers)
		{
			var repository = Container.Resolve<ISubscriptionRepository>();

			foreach (IConsumer handler in consumers)
			{
				ISubscription subscription = new Subscription();
				subscription.Component = handler.GetType().FullName;
				//subscription.Uri = endpoint.Address.Uri;

				ICollection<Type> consumedMessages = GetConsumedMessages(handler);

				foreach (Type message in consumedMessages)
				{
					subscription.IsActive = true;
					subscription.Message = message.FullName;
					repository.Register(subscription);
				}
			}
		}

		private ICollection<Type> GetConsumedMessages(object consumer)
		{
			var theMessages = new List<Type>();

			ICollection<Type> theInterfaces = (from theInterface in consumer.GetType().GetInterfaces()
			                                   where theInterface.FullName.StartsWith(typeof (StartedBy<>).FullName)
			                                         || theInterface.FullName.StartsWith(typeof (OrchestratedBy<>).FullName)
			                                         || theInterface.FullName.StartsWith(typeof (Consumes<>).FullName)
			                                   select theInterface).Distinct().ToList();

			foreach (Type theInterface in theInterfaces)
			{
				Type theMessage = theInterface.GetGenericArguments()[0];

				if (theMessages.Contains(theMessage) == false)
				{
					theMessages.Add(theMessage);
				}
			}

			return theMessages;
		}
	}
}