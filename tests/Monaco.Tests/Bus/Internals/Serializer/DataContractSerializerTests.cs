using System;
using System.Collections.Generic;
using Monaco.Bus.MessageManagement.Serialization;
using Monaco.Configuration;
using Xunit;

namespace Monaco.Tests.Bus.Internals.Serializer
{
    public class DataContractSerializerTests : IDisposable
    {
        private IConfiguration configuration;

        public DataContractSerializerTests()
        {
			configuration = Monaco.Configuration.Configuration.Create();
			configuration
				.WithContainer(c => c.UsingWindsor());
			((Monaco.Configuration.Configuration)this.configuration).Configure();
        }

		public void Dispose()
		{
			if(this.configuration != null)
			{
				if(this.configuration.Container != null)
				{
					this.configuration.Container.Dispose();
				}
			}
			this.configuration = null;
		}

    	[Fact(Skip = "Using SharpSerializer over DataContractSerializer")]
        public void can_serialize_proxy_from_interface()
        {
            var contracts = new List<Type>();
            contracts.Add(typeof (ILoanApplication));

    		var serializer = this.configuration.Container.Resolve<ISerializationProvider>();

			serializer.AddTypes(contracts);
			serializer.Initialize();

            var application = configuration.Container.Resolve<ILoanApplication>();
            application.ApplicationNumber = Guid.NewGuid().ToString();
            application.ReceivedOn = DateTime.Now;

			var results = serializer.Serialize(application);
            Assert.NotEqual(results, string.Empty);
            Assert.True(results.Contains("LoanApplication"));
            System.Console.WriteLine(results);

            // turn the serialized results into an object:
			var loanApplication = serializer.Deserialize(results);
            Assert.IsAssignableFrom<ILoanApplication>(loanApplication);
        }

        #region Nested type: IApplication

        public interface IApplication : IMessage
        {
            string ApplicationNumber { get; set; }
            DateTime ReceivedOn { get; set; }
        }

        #endregion

        #region Nested type: ILoanApplication

        public interface ILoanApplication : IApplication
        {
        }

        #endregion
    }
}