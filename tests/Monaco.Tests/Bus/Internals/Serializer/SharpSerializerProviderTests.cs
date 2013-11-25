using System;
using System.Collections.Generic;
using Castle.DynamicProxy;
using Monaco.Bus.Internals.Reflection;
using Monaco.Bus.MessageManagement.Serialization;
using Monaco.Bus.MessageManagement.Serialization.Impl;
using Monaco.Configuration;
using Monaco.Testing.Internals.Interceptors.Impl;
using Xunit;

namespace Monaco.Tests.Bus.Internals.Serializer
{
	public class SharpSerializerProviderTests : IDisposable
	{
		private IConfiguration configuration;
		private readonly ISerializationProvider _provider;

		public SharpSerializerProviderTests()
		{
			configuration = Monaco.Configuration.Configuration.Create();
			configuration
				.WithContainer(c => c.UsingWindsor())
				.WithStorage(s => s.UsingInMemoryStorage())
				.WithTransport(t => t.UsingInMemory());

			// forcibly configure the current configuration options:
			((Monaco.Configuration.Configuration)this.configuration).Configure();

			_provider = new SharpSerializationProvider();
		}

		public void Dispose()
		{
			if (this.configuration != null)
			{
				if (this.configuration.Container != null)
					this.configuration.Container.Dispose();
			}
			this.configuration = null;
		}

		[Fact]
		public void can_serialize_message_to_string_based_xml()
		{
			var guid = Guid.NewGuid();
			var policy = new AutoPolicy() { Account = "123" };
			policy.Guid = guid;
			var result = _provider.Serialize(policy);
			System.Diagnostics.Debug.WriteLine(result);

			Assert.NotNull(result);
			Assert.True(result.Contains("AutoPolicy"));

			var thePolicy = _provider.Deserialize<AutoPolicy>(result);
			Assert.NotNull(thePolicy);
			Assert.Equal("123", thePolicy.Account);
			Assert.Equal(guid, thePolicy.Guid);
		}

		[Fact]
		public void can_serialize_interface_message()
		{
			var insurancePolicy = CreateProxy<IInsurancePolicy>();
			insurancePolicy.Account = "123";

			// serialize:
			var results = _provider.Serialize(insurancePolicy);

			Assert.NotNull(results);
			Assert.True(results.Contains("InsurancePolicy"));
			System.Diagnostics.Debug.WriteLine(results);

			// deserialize:
			var theInsuracePolicy = _provider.Deserialize<IInsurancePolicy>(results);
			Assert.NotNull(theInsuracePolicy);
			Assert.Equal("123", theInsuracePolicy.Account);
		}


		[Fact]
		public void can_serialize_message_based_on_interface()
		{
			IReflection reflection = this.configuration.Container.Resolve<IReflection>();

			var contracts = new List<Type>();
			contracts.Add(typeof(IInsurancePolicy));

			var proxies = reflection.BuildProxyAssemblyForContracts(contracts, true);

			var insurancePolicy = this.configuration.Container.Resolve<IInsurancePolicy>();
			insurancePolicy.Account = "123";

			// serialize:
			var results = _provider.Serialize(insurancePolicy);

			Assert.NotNull(results);
			Assert.True(results.Contains("InsurancePolicy"));

			// deserialize:
			var theInsuracePolicy = _provider.Deserialize<IInsurancePolicy>(results);
			Assert.NotNull(theInsuracePolicy);
			Assert.Equal("123", theInsuracePolicy.Account);
		}

		private T CreateProxy<T>()
		{
			var storage = new InterfacePersistance();
			var interceptor = new InterfaceInterceptor(storage);
			var generator = new ProxyGenerator();
			return (T)generator.CreateInterfaceProxyWithoutTarget(typeof(T), interceptor);
		}

	}

	public interface IPolicy : IMessage
	{
		string Account { get; set; }
	}

	public interface IInsurancePolicy : IPolicy
	{
	}
	public class AutoPolicy : IPolicy
	{
		public string Account { get; set; }
		public Guid Guid { get; set; }
	}
}