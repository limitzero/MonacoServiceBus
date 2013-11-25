using Monaco.Modelling.BusinessModel.Actions;
using Monaco.Modelling.BusinessModel.Elements;

namespace Monaco.Modelling.BusinessModel.Internals
{
	public class BusinessProcessModelAction
	{
		/// <summary>
		/// Gets the type of action to be taken on the proccess by the actor.
		/// </summary>
		public BusinessProcessModelActionType ModelActionType { get; set; }

		/// <summary>
		/// Gets or sets the action to take place on the process for the actor.
		/// </summary>
		public IModelAction Action { get; set; }

		/// <summary>
		/// Gets or sets the state that the proces should be in after the action has occurred.
		/// </summary>
		public ProcessingState State { get; set; }
	}
}