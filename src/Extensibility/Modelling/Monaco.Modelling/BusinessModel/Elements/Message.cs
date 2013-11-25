namespace Monaco.Modelling.BusinessModel.Elements
{
	/// <summary>
	/// A message is an single unit of information that is used for transmission between processes, tasks, or activities.
	/// </summary>
	public class Message : IModelElement
	{
		public string Name { get; set; }
		public string Description { get; set; }
	}
}