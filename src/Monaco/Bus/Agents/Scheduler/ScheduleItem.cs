using System;
using System.Threading;
using Monaco.Bus.Agents.Scheduler.EventArgs;
using Monaco.Bus.Internals.Eventing;
using Monaco.Extensions;

namespace Monaco.Bus.Agents.Scheduler
{
	public class ScheduleItem : IScheduledItem
	{
		private bool _disposed;
		private Timer _timer;

		public ScheduleItem()
		{
			Name = string.Format("TASK-{0}", Guid.NewGuid());
		}

		#region IScheduledItem Members

		public event EventHandler<ComponentNotificationEventArgs> ComponentNotificationEvent;
		public event EventHandler<ScheduledItemMessageCreatedEventArgs> ScheduledItemMessageCreated;
		public event EventHandler<ScheduledItemErrorEventArgs> ScheduledItemError;

		public string Name { get; set; }

		public bool HaltOnError { get; set; }

		public bool ForceStart { get; set; }

		public IScheduledTask Task { get; set; }

		public bool IsRunning { get; private set; }

		public void Start()
		{
			if (IsRunning) return;

			if (string.IsNullOrEmpty(Task.Interval)) return;

			if (string.IsNullOrEmpty(Name))
			{
				Name = string.Format("TASK-{0}", Guid.NewGuid());
			}

			HookupEventHandlers(Task, true);

			if (ForceStart)
			{
				ExecuteTask(null);
			}

			var timespan = new TimeSpan().CreateFromInterval(Task.Interval);

			if (timespan.HasValue == true)
			{
				_timer = new Timer(ExecuteTask, null, 100,
								 timespan.Value.Seconds * 1000);

				IsRunning = true;
			}
			else
			{
				OnScheduledItemError(
					new Exception(
						string.Format(
							"The current interval of '{0}' for the task '{1}' could not be converted to an measure of time for invocation. The task will not be run.",
							Task.Interval, 
							Name)));
				this.Stop();
			}
		}

		public void Stop()
		{
			IsRunning = false;

			if (_timer != null)
			{
				_timer.Dispose();
			}
			_timer = null;

			HookupEventHandlers(Task, false);
			Task = null;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

		private void HookupEventHandlers(IScheduledTask task, bool enlistOnly)
		{
			if (enlistOnly)
			{
				Task.ComponentErrorEvent += OnTaskError;
				Task.ComponentNotificationEvent += OnTaskNotification;
				Task.ScheduledTaskMessageCreated += OnTaskMessageCreated;
			}
			else
			{
				Task.ComponentErrorEvent -= OnTaskError;
				Task.ComponentNotificationEvent -= OnTaskNotification;
				Task.ScheduledTaskMessageCreated -= OnTaskMessageCreated;
			}
		}

		private void ExecuteTask(object state)
		{
			if (_disposed) return;

			try
			{
				OnScheduledItemStarted();

				OnScheduledItemNotification(this,
				                            new ComponentNotificationEventArgs(NotificationLevel.Info,
				                                                               string.Format(
				                                                               	"Executing task '{0}' scheduled for interval '{1}'",
				                                                               	Name, Task.Interval)));

				Task.Execute();
			}
			catch (Exception exception)
			{
				if (!OnScheduledItemError(exception))
					throw;
			}
			finally
			{
				OnScheduledItemCompleted();
			}
		}

		public virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					Stop();
				}

				_disposed = true;
			}
		}

		private void OnTaskMessageCreated(object sender, ScheduledTaskMessageCreatedEventArgs e)
		{
			OnScheduledItemMessageCreated(e.Message);
		}

		private void OnTaskNotification(object sender, ComponentNotificationEventArgs e)
		{
			OnScheduledItemNotification(sender, e);
		}

		private void OnTaskError(object sender, ComponentErrorEventArgs e)
		{
			if (HaltOnError)
			{
				Stop();
				string message = string.Format("Scheduled task '{0}' forcibly stopped. Reason: [{1}], Stack Trace: {2}.'", Name,
				                               e.ErrorMessage, e.Exception.StackTrace);
				OnScheduledItemNotification(this, new ComponentNotificationEventArgs(NotificationLevel.Warn, message));
			}
		}

		private void OnScheduledItemMessageCreated(IMessage message)
		{
			if (_disposed || IsRunning == false) return;

			EventHandler<ScheduledItemMessageCreatedEventArgs> evt = ScheduledItemMessageCreated;

			if (evt != null)
			{
				evt(this, new ScheduledItemMessageCreatedEventArgs(message));
			}
		}

		private void OnScheduledItemNotification(object sender, ComponentNotificationEventArgs e)
		{
			EventHandler<ComponentNotificationEventArgs> evt = ComponentNotificationEvent;

			if (evt != null)
			{
				evt(this, new ComponentNotificationEventArgs(e.Level, e.Message));
			}
		}

		private bool OnScheduledItemError(Exception exception)
		{
			EventHandler<ScheduledItemErrorEventArgs> evt = ScheduledItemError;

			bool isEventHandlerAttached = (evt != null);

			if (isEventHandlerAttached)
				evt(this, new ScheduledItemErrorEventArgs(this, exception));

			return isEventHandlerAttached;
		}

		private void OnScheduledItemStarted()
		{
			EventHandler<ComponentNotificationEventArgs> evt = ComponentNotificationEvent;
			if (evt != null)
			{
				evt(this, new ComponentNotificationEventArgs(string.Format("Scheduled item {0} started.", Name)));
			}
		}

		private void OnScheduledItemCompleted()
		{
			EventHandler<ComponentNotificationEventArgs> evt = ComponentNotificationEvent;
			if (evt != null)
			{
				evt(this, new ComponentNotificationEventArgs(string.Format("Scheduled item {0} completed", Name)));
			}
		}
	}
}