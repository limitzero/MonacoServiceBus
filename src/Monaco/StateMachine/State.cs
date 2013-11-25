using System;

namespace Monaco.StateMachine
{
	/// <summary>
	/// Class that represents a state in the state machine where it is reflecting the outcome of an event happening or potential event to happen.
	/// </summary>
	[Serializable]
	public class State
	{
		public State()
			: this(string.Empty)
		{
		}

		public State(string name)
		{
			Name = name;
		}

		public string Name { get; set; }
	}
}