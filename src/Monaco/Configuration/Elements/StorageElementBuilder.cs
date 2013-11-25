using System;
using System.Reflection;
using Castle.Core.Configuration;
using Monaco.Extensibility.Storage.StateMachines;
using Monaco.Extensibility.Storage.Subscriptions;
using Monaco.Extensibility.Storage.Timeouts;

namespace Monaco.Configuration.Elements
{
	public class StorageElementBuilder : BaseElementBuilder
	{
		private const string _element_name = "storage";

		public override bool IsMatchFor(string name)
		{
			return name.Trim().ToLower() == _element_name.Trim().ToLower();
		}

		public override void Build(Castle.Core.Configuration.IConfiguration configuration)
		{
			for (int index = 0; index < configuration.Children.Count; index++)
			{
				Castle.Core.Configuration.IConfiguration element = configuration.Children[index];

				switch (element.Name)
				{
					case "subscriptions":
						ConfigureSubscriptionStorage(element);
						break;

					case "saga-data":
						ConfigureSagaDataStorage(element);
						break;

					case "timeouts":
						ConfigureTimeoutsStorage(element);
						break;
				}
			}
		}

		private void ConfigureSubscriptionStorage(Castle.Core.Configuration.IConfiguration configuration)
		{
			string storageName = "Subscription Storage";

			string provider = InspectStorageProvider(storageName, configuration);
			Type providerType = ExtractProviderType(storageName, provider);

			try
			{
				this.Container.Register(typeof(ISubscriptionRepository), providerType);
			}
			catch (Exception exception)
			{
				throw CouldNotRegisterStorageProviderException(storageName, provider, exception);
			}
		}

		private void ConfigureSagaDataStorage(Castle.Core.Configuration.IConfiguration configuration)
		{
			string storageName = "Saga Data Storage";

			string provider = InspectStorageProvider(storageName, configuration);
			Type providerType = ExtractProviderType(storageName, provider);

			try
			{
				this.Container.Register(typeof(IStateMachineDataRepository<>), providerType);
			}
			catch (Exception exception)
			{
				throw CouldNotRegisterStorageProviderException(storageName, provider, exception);
			}
		}

		private void ConfigureTimeoutsStorage(Castle.Core.Configuration.IConfiguration configuration)
		{
			string storageName = "Timeouts Storage";

			string provider = InspectStorageProvider(storageName, configuration);
			Type providerType = ExtractProviderType(storageName, provider);

			try
			{
				this.Container.Register(typeof(ITimeoutsRepository), providerType);
			}
			catch (Exception exception)
			{
				throw CouldNotRegisterStorageProviderException(storageName, provider, exception);
			}
		}

		private string InspectStorageProvider(string storageType, Castle.Core.Configuration.IConfiguration configuration)
		{
			string provider = configuration.Attributes["provider"];

			if (string.IsNullOrEmpty(provider))
			{
				throw new Exception("The provider type was not defined for storage " + storageType);
			}

			return provider;
		}

		private Type ExtractProviderType(string storageProviderName, string typeName)
		{
			Assembly asm = null;
			Type theType = null;

			string[] parts = typeName.Split(new[] {","}, StringSplitOptions.RemoveEmptyEntries);

			if (parts.Length != 2)
			{
				throw StorageProviderNotFullyQualifiedException(storageProviderName);
			}

			try
			{
				asm = Assembly.Load(parts[1]);
			}
			catch
			{
				throw CouldNotLoadProviderAssemblyException(storageProviderName, parts[1]);
			}

			object instance = asm.CreateInstance(parts[0]);
			theType = instance.GetType();

			return theType;
		}

		private Exception CouldNotLoadProviderAssemblyException(string provider, string providerAssembly)
		{
			return new Exception(string.Format("The provider '{0}' could not be loaded from assembly '{1}",
			                                   provider, providerAssembly));
		}

		private Exception StorageProviderNotFullyQualifiedException(string storageProviderName)
		{
			return
				new Exception(
					string.Format(
						"For the storage provider '{0}', please specify the fully qualified type of the object providing the storage.",
						storageProviderName));
		}

		private Exception CouldNotRegisterStorageProviderException(string storageName, string provider, Exception exception)
		{
			return new Exception(string.Format("Could not register provider '{0}' for storage '{1}'. Reason: {2}",
			                                   provider, storageName), exception);
		}
	}
}