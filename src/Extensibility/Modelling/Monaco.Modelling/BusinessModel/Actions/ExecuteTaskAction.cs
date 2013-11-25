using System.Collections.Generic;
using Monaco.Modelling.BusinessModel.Elements;

namespace Monaco.Modelling.BusinessModel.Actions
{
	public class ExecuteTaskAction : IModelAction
	{
		public Message InputMessage { get; private set; }
		public Message OutputMessage { get; private set; }
		public Message ExceptionMessage { get; private set; }
		public bool CanMarkAsCompleted { get; private set; }
		public ICollection<Task> Tasks { get; private set; }

		public ExecuteTaskAction(Message inputMessage, ICollection<Task> tasks, bool canMarkAsCompleted = false)
		{
			InputMessage = inputMessage;
			Tasks = tasks;
			CanMarkAsCompleted = canMarkAsCompleted;
		}

		public ExecuteTaskAction(Message inputMessage, ICollection<Task> tasks, Message outputMessage, Message exceptionMessage, bool canMarkAsCompleted = false)
		{
			InputMessage = inputMessage;
			OutputMessage = outputMessage;
			ExceptionMessage = exceptionMessage;
			Tasks = tasks;
			CanMarkAsCompleted = canMarkAsCompleted;
		}
	}
}