using System;

namespace Monaco.Bus.Agents.Scheduler.Tasks.Configuration.Impl
{
	public class TaskConfiguration : ITaskConfiguration
	{
		public TaskConfiguration()
		{
			TaskName = string.Format("TASK-{0}", Guid.NewGuid());
		}

		#region ITaskConfiguration Members

		public string TaskName { get; set; }
		public Type Component { get; set; }
		public object ComponentInstance { get; set; }
		public string MethodName { get; set; }
		public string Interval { get; set; }
		public bool HaltOnError { get; set; }
		public bool ForceStart { get; set; }

		#endregion
	}
}