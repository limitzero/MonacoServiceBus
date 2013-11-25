using Monaco.Modelling.BusinessModel.Elements;

namespace Monaco.Modelling.BusinessModel.Actions
{
	public class WhenAction : IModelAction
	{
		public Message Message { get; private set; }

		public WhenAction(Message message)
		{
			Message = message;
		}
	}
}