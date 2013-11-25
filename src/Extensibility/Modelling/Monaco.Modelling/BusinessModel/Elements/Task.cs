using System;

namespace Monaco.Modelling.BusinessModel.Elements
{
	/// <summary>
	/// A task is a process or series of processes that carry out a specific business function.
	/// </summary>
	[Serializable]
	public class Task : IModelElement
	{
		public string Name { get; set; }
		public string Description { get; set; }
	}
}