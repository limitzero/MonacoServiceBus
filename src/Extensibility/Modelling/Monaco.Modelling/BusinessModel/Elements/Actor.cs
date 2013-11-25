using System;

namespace Monaco.Modelling.BusinessModel.Elements
{
	/// <summary>
	///  An actor is a specific entity or role that will be responsible for the execution of <seealso cref="Task">tasks</seealso>.
	/// </summary>
	[Serializable]
	public class Actor : IModelElement
	{
		public string Name { get; set; }
		public string Description { get; set; }
	}
}