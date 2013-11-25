using System;
using System.Collections.Generic;
using System.Linq;
using Castle.MicroKernel;
using Monaco.Bus.Agents.Scheduler.EventArgs;
using Monaco.Bus.Agents.Scheduler.Tasks.Configuration;
using Monaco.Bus.Agents.Scheduler.Tasks.MethodInvoker;
using Monaco.Bus.Exceptions;
using Monaco.Bus.Internals.Eventing;
using Monaco.Configuration;

namespace Monaco.Bus.Agents.Scheduler
{
	/// <summary>
	/// The scheduler is the point in the system where all pollable tasks are registered for execution 
	/// according to their defined intervals.
	/// </summary>
	public class Scheduler : IScheduler
	{
		private readonly IContainer container;
		private bool _disposed;
		private List<IScheduledItem> _scheduledItems;

		public Scheduler(IContainer container)
		{
			this.container = container;

			if (_scheduledItems == null)
			{
				_scheduledItems = new List<IScheduledItem>();
			}
		}

		#region IScheduler Members

		public Action<IMessage> OnMessageReceived { get; set; }

		public event EventHandler<ComponentErrorEventArgs> ComponentErrorEvent;

		public event EventHandler<ComponentNotificationEventArgs> ComponentNotificationEvent;

		public ICollection<IScheduledItem> RegisteredItems
		{
			get { return _scheduledItems; }
			set { _scheduledItems = new List<IScheduledItem>(value); }
		}

		public bool IsRunning { get; private set; }

		public void Start()
		{
			if (_disposed) return;

			foreach (IScheduledItem item in RegisteredItems)
			{
				HookUpEventHandlers(item, true);
				item.Start();
			}

			IsRunning = true;
			OnSchedulerStarted();
		}

		public void Stop()
		{
			if (IsRunning == false) return;

			foreach (IScheduledItem item in _scheduledItems)
			{
				HookUpEventHandlers(item, false);
				item.Stop();
			}

			IsRunning = false;
			OnSchedulerStopped();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public void RegisterItem(IScheduledItem item)
		{
			try
			{
				if (_scheduledItems.Exists(x => x.Name.Trim().ToLower() == item.Name.Trim().ToLower()) == false)
				{
					RegisteredItems.Add(item);
				}
			}
			catch
			{
			}
		}

		public void CreateScheduledItem(string taskName, string interval, object task,
		                                string taskMethod, bool haltOnError, bool forceStart)
		{
			IMethodInvokerScheduledTask theTask = new MethodInvokerScheduledTask();
			theTask.Instance = task;
			theTask.MethodName = taskMethod;
			theTask.Interval = interval;

			IScheduledItem item = new ScheduleItem();
			item.Name = taskName;
			item.Task = theTask;
			item.HaltOnError = haltOnError;
			item.ForceStart = forceStart;

			RegisterItem(item);
		}

		public void CreateFromConfiguration(ITaskConfiguration configuration)
		{
			IMethodInvokerScheduledTask theTask = new MethodInvokerScheduledTask();
			string theMethodName = typeof (Produces<>).GetMethods()[0].Name;
			Type theType = null;

			if (configuration.Component == null && configuration.ComponentInstance == null)
			{
				throw new MonacoConfigurationException(string.Format("The task '{0}' must supply a component type or " +
				                                                     "component instance for invocation.", configuration.TaskName));
			}

			// check the container to see if the component has already been registered, if not then add it:

			object aTask = null;

			try
			{
				if (configuration.ComponentInstance != null)
				{
					theType = configuration.ComponentInstance.GetType();
					aTask = container.Resolve(theType);
				}
			}
			catch
			{
				if (configuration.ComponentInstance != null)
				{
					this.container.RegisterInstance(configuration.ComponentInstance);
				}
				else
				{
					this.container.Register(configuration.Component);
				}
			}


			if (theType == null && configuration.Component != null)
			{
				try
				{
					if (configuration.ComponentInstance != null)
					{
						theType = configuration.Component;
						aTask = container.Resolve(theType);
					}
				}
				catch
				{
					this.container.RegisterInstance(configuration.ComponentInstance);
				}
			}

			if (aTask == null)
				aTask = container.Resolve(theType);

			Type theInterface = (from type in aTask.GetType().GetInterfaces()
			                     where type.FullName.StartsWith(typeof (Produces<>).FullName)
			                     select type).FirstOrDefault();

			if (theInterface == null)
			{
				if (string.IsNullOrEmpty(configuration.MethodName))
				{
					throw new MonacoConfigurationException(
						string.Format(
							"For the task '{0}' using component '{1}', the method that will be executed must be supplied.",
							configuration.TaskName, theType.FullName));
				}
			}
			else
			{
				configuration.MethodName = theMethodName;
			}

			theTask.Instance = aTask;
			theTask.MethodName = configuration.MethodName;
			theTask.Interval = configuration.Interval;

			IScheduledItem item = new ScheduleItem();
			item.Name = configuration.TaskName;
			item.Task = theTask;
			item.HaltOnError = configuration.HaltOnError;
			item.ForceStart = configuration.ForceStart;

			RegisterItem(item);
		}

		#endregion

		private void Dispose(bool disposing)
		{
			_disposed = disposing;

			if (disposing)
			{
				//clean up resources here:
			}
		}

		private void HookUpEventHandlers(IScheduledItem scheduledItem, bool enlistOnly)
		{
			if (enlistOnly)
			{
				scheduledItem.ScheduledItemError += OnScheduledItemError;
				scheduledItem.ComponentNotificationEvent += OnScheduledItemNotification;
				scheduledItem.ScheduledItemMessageCreated += OnScheduledItemMessageCreated;
			}
			else
			{
				scheduledItem.ScheduledItemError -= OnScheduledItemError;
				scheduledItem.ComponentNotificationEvent -= OnScheduledItemNotification;
				scheduledItem.ScheduledItemMessageCreated -= OnScheduledItemMessageCreated;
			}
		}


		private void OnSchedulerStarted()
		{
			EventHandler<ComponentNotificationEventArgs> evt = ComponentNotificationEvent;

			if (evt != null)
			{
				evt(this, new ComponentNotificationEventArgs("Scheduler Started."));
			}
		}

		private void OnSchedulerStopped()
		{
			EventHandler<ComponentNotificationEventArgs> evt = ComponentNotificationEvent;

			if (evt != null)
			{
				evt(this, new ComponentNotificationEventArgs("Scheduler Stopped."));
			}
		}

		private bool OnSchedulerError(Exception exception)
		{
			EventHandler<ComponentErrorEventArgs> evt = ComponentErrorEvent;

			bool isHandlerAttached = (evt != null);

			if (isHandlerAttached)
				evt(this, new ComponentErrorEventArgs(exception));

			return isHandlerAttached;
		}

		private void OnScheduledItemComponentError(object sender, ComponentErrorEventArgs e)
		{
			var itemException = new Exception(e.ErrorMessage, e.Exception);
			OnSchedulerError(itemException);
		}

		private void OnScheduledItemMessageCreated(object sender, ScheduledItemMessageCreatedEventArgs e)
		{
			if (e.Message != null)
			{
				if (OnMessageReceived != null)
				{
					OnMessageReceived(e.Message);
				}
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

		private void OnScheduledItemError(object sender, ScheduledItemErrorEventArgs e)
		{
			if (e.ScheduledItem.HaltOnError)
			{
				e.ScheduledItem.Stop();

				var notificationEventArgs =
					new ComponentNotificationEventArgs(NotificationLevel.Warn,
					                                   string.Format("Task '{0}' forcibly stopped on error: Reason: {1}",
					                                                 e.ScheduledItem.Name, e.Exception.StackTrace));

				OnScheduledItemNotification(this, notificationEventArgs);
			}
			else
			{
				var notificationEventArgs =
					new ComponentNotificationEventArgs(NotificationLevel.Warn,
					                                   string.Format("Task '{0}' experinced an error: Message: {1}",
					                                                 e.ScheduledItem.Name, e.Exception.StackTrace));
				OnScheduledItemNotification(this, notificationEventArgs);
			}
		}
	}
}