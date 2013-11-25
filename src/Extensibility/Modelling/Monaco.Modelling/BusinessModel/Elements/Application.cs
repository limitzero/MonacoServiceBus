namespace Monaco.Modelling.BusinessModel.Elements
{
	/// <summary>
	/// The Application model element represents a point in the system 
	/// where the user sends or receives data as a result of an action.
	/// </summary>
	public class Application : IModelElement
	{
		public string Name { get; set; }
		public string Description { get; set; }
		public Message InputMessage { get; set; }
		public Message OutputMessage { get; set; }
	}
}