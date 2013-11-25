using System;

namespace Monaco.Modelling.BusinessModel.Elements
{
	/// <summary>
	/// This will denote the current state of the business process after or before a series of actions have been executed.
	/// </summary>
	[Serializable]
	public class ProcessingState : IModelElement
	{
		public string Name { get; set; }
		public string Description { get; set; }
	}
}