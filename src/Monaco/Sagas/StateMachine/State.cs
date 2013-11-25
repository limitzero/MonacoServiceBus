using System;

namespace Monaco.Sagas.StateMachine
{
	/// <summary>
	///  Class that represents a state in the saga where it is reflecting the occurence of an event (i.e. message receipt and processing).
	/// </summary>
	[Serializable]
	public class State
	{
		public string Name { get; set; }

		public State()
			:this(string.Empty)
		{
		}

		public State(string name)
		{
			this.Name = name;
		}
	}
}