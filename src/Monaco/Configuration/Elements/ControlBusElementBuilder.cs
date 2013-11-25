using Castle.Core.Configuration;
using Monaco.Bus.Internals.Reflection;

namespace Monaco.Configuration.Elements
{
	public class ControlBusElementBuilder : BaseElementBuilder
	{
		private const string _elementname = "control-bus";

		public override bool IsMatchFor(string name)
		{
			return name.Trim().ToLower() == _elementname.Trim().ToLower();
		}

		public override void Build(Castle.Core.Configuration.IConfiguration configuration)
		{
			string uri = configuration.Attributes["uri"] ?? string.Empty;

			if (string.IsNullOrEmpty(uri) == false)
			{
				var reflection = Container.Resolve<IReflection>();

				//IEndpointBuilderSubscriptionRepository repository =
				//    Kernel.Resolve<IEndpointBuilderSubscriptionRepository>();

				//IEndpointBuilderSubscription subscription = repository.Find(uri);

				//object builder = reflection.BuildInstance(subscription.Builder);

				//BaseEndpoint endpoint = reflection.InvokeBuildForEndpointBuilder(builder, uri);

				//endpoint.Name = Constants.CONTROL_BUS_ENDPONT_NAME;
				//endpoint.MaxRetries = 5;
				//Kernel.Resolve<IEndpointRegistry>().Register(endpoint);
			}
		}
	}
}