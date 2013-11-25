using Castle.Core.Configuration;
using Monaco.Bus.Internals.Reflection;

namespace Monaco.Configuration.Elements
{
	public class SubscriptionManagerElementBuilder : BaseElementBuilder
	{
		private const string _element_name = "subscription-manager";

		public override bool IsMatchFor(string name)
		{
			return _element_name.Trim().ToLower() == name.Trim().ToLower();
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

				//endpoint.Name = Constants.SUBSCRIPTION_MANAGER_ENDPOINT_NAME;
				//endpoint.MaxRetries = 5;
				//Kernel.Resolve<IEndpointRegistry>().Register(endpoint);
			}
		}
	}
}