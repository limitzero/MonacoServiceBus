namespace Monaco.Modelling.BusinessModel.Elements
{
	/// <summary>
	/// An activity is a point in the process where external intervention is required in order for the processes 
	/// for a given actor to move toward completion.
	/// </summary>
	public class Activity : IModelElement
	{
		public string Name { get; set; }

		public string Description { get; set; }
	}

	/// <summary>
	/// An activity is a point in the process where external intervention is required in order for the processes 
	/// for a given actor to move toward completion. For this definition, a message will be produced as a
	/// result of completing the activity.
	/// </summary>
	public class Activity<TMessage> : Activity  where TMessage : Message
	{}
}