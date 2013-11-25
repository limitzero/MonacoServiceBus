namespace Monaco.Modelling.BusinessModel.Actions
{
	public class CompleteAction : IModelAction
	{
		public bool IsComplete { get; private set; }

		public CompleteAction(bool isComplete)
		{
			IsComplete = isComplete;
		}
	}
}