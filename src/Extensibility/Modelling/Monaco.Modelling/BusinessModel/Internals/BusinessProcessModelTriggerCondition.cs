using System.Collections.Generic;
using Monaco.Modelling.BusinessModel.Actions;
using Monaco.Modelling.BusinessModel.Elements;

namespace Monaco.Modelling.BusinessModel.Internals
{
	public class BusinessProcessModelTriggerCondition
	{
		private Message _currentMessageContext;

		/// <summary>
		/// Gets the <seealso cref="Capability">capability</seealso> to 
		/// be assigned to the process.
		/// </summary>
		public Capability Capability { get; private set; }

		/// <summary>
		/// Gets the set of model actions that will be taken by the process model.
		/// </summary>
		public ICollection<BusinessProcessModelAction> ModelActions { get; private set; }

		/// <summary>
		/// Gets the current message that will be passed into the capability 
		/// for the business process in order for subsequent actions, tasks or processes
		/// to take place.
		/// </summary>
		public Message Message { get; private set; }

		public ICollection<ExecuteTaskAction> Tasks { get; private set; }
		public ICollection<WaitForActivityAction> Activities { get; private set; }
		public bool IsComplete { get; private set; }
		public BusinessProcessModelStages Stage { get; set; }

		public BusinessProcessModelTriggerCondition(Capability capability)
		{
			this.Capability = capability;

			this.ModelActions = new List<BusinessProcessModelAction>();

			this.Tasks = new List<ExecuteTaskAction>();
			this.Activities = new List<WaitForActivityAction>();
		}

		/// <summary>
		/// Marker in the business process for a task, activity or process to begin as a result of an event being triggered and a resulting message is produced.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public BusinessProcessModelTriggerCondition When(Message message)
		{
			var action = new BusinessProcessModelAction
							{
								Action = new WhenAction(message),
								ModelActionType = BusinessProcessModelActionType.When
							};

			this.Message = message;
			this._currentMessageContext = this.Message;

			this.ModelActions.Add(action);

			return this;
		}

		/// <summary>
		/// This will denote the point in the model where a task or series of tasks are carried out by an <seealso cref="Actor">actor</seealso>
		/// for the given <seealso cref="Message">message</seealso> received.
		/// </summary>
		/// <param name="tasks">Listing of tasks to be executed on behalf of the <seealso cref="Actor">actor</seealso></param>
		/// <returns></returns>
		public BusinessProcessModelTriggerCondition ExecuteTasks(params Task[] tasks)
		{
			var executeTaskAction = new ExecuteTaskAction(this._currentMessageContext, tasks);

			var action = new BusinessProcessModelAction
			{
				Action = executeTaskAction,
				ModelActionType = BusinessProcessModelActionType.ExecuteTask
			};

			this.ModelActions.Add(action);

			this.Tasks.Add(executeTaskAction);

			return this;
		}

		/// <summary>
		/// This will denote the point in the model where a task or series of tasks are carried out by an <seealso cref="Actor">actor</seealso>
		/// for the given <seealso cref="Message">message</seealso> received.
		/// </summary>
		/// <param name="response">Optional message that is returned after the series of tasks are completed</param>
		/// <param name="tasks">Listing of tasks to be executed on behalf of the <seealso cref="Actor">actor</seealso></param>
		/// <returns></returns>
		public BusinessProcessModelTriggerCondition ExecuteTasksAndReturnResponse(Message response, params Task[] tasks)
		{
			var executeTaskAction = new ExecuteTaskAction(this._currentMessageContext, tasks, response, null);

			var action = new BusinessProcessModelAction
			{
				Action = executeTaskAction,
				ModelActionType = BusinessProcessModelActionType.ExecuteTask
			};

			this.Tasks.Add(executeTaskAction);

			this.ModelActions.Add(action);

			return this;
		}

		/// <summary>
		/// This will denote the point in the model where a task or series of tasks are carried out by an <seealso cref="Actor">actor</seealso>
		/// for the given <seealso cref="Message">message</seealso> received.
		/// </summary>
		/// <param name="response">Optional message that is returned after the series of tasks are completed</param>
		/// <param name="exception">Optional message that is returned when the tasks can not be completed</param>
		/// <param name="tasks">Listing of tasks to be executed on behalf of the <seealso cref="Actor">actor</seealso></param>
		/// <returns></returns>
		public BusinessProcessModelTriggerCondition ExecuteTaskAndReturnException(Message exception, params Task[] tasks)
		{
			var executeTaskAction = new ExecuteTaskAction(this._currentMessageContext, tasks, null, exception);

			var action = new BusinessProcessModelAction
			{
				Action = executeTaskAction,
				ModelActionType = BusinessProcessModelActionType.ExecuteTask
			};

			this.Tasks.Add(executeTaskAction);

			this.ModelActions.Add(action);

			return this;
		}



		/// <summary>
		/// This will denote a portion in the process where an external activity needs to be completed in order for the process to 
		/// move forward to completion for the given <seealso cref="Actor">actor</seealso>
		/// </summary>
		/// <param name="activity">External or internal activity to be executed outside the bounds of the process</param>
		/// <param name="message">Message to be produced after the activity has completed.</param>
		/// <returns></returns>
		public BusinessProcessModelTriggerCondition WaitForActivity(Activity activity, Message message)
		{
			var waitForActivityAction = new WaitForActivityAction(message, activity);
			
			// switch the current context of the message being processed from the When() 
			// condition so that subsequent steps work on the most recent message:
			this._currentMessageContext = message;

			var action = new BusinessProcessModelAction
			{
				Action = waitForActivityAction,
				ModelActionType = BusinessProcessModelActionType.WaitForActivity
			};

			this.ModelActions.Add(action);
			this.Activities.Add(waitForActivityAction);

			return this;
		}

		/// <summary>
		/// This will denote a portion in the process where an external activity needs to be completed in order for the process to 
		/// move forward to completion for the given <seealso cref="Actor">actor</seealso>
		/// </summary>
		/// <param name="activities"></param>
		/// <returns></returns>
		public BusinessProcessModelTriggerCondition WaitForActivity(params Activity[] activities)
		{
			var waitForActivityAction = new WaitForActivityAction(activities);

			var action = new BusinessProcessModelAction
			{
				Action = waitForActivityAction,
				ModelActionType = BusinessProcessModelActionType.WaitForActivity
			};

			this.ModelActions.Add(action);

			this.Activities.Add(waitForActivityAction);

			return this;
		}

		/// <summary>
		/// This will denote a portion in the process where an external activity needs to be completed in order for the process to 
		/// move forward to completion for the given <seealso cref="Actor">actor</seealso>
		/// </summary>
		/// <param name="message">Message to return after the activities are completed</param>
		/// <param name="activities">Series of activities to invoke via an external party</param>
		/// <returns></returns>
		public BusinessProcessModelTriggerCondition WaitForActivity(Message message, params Activity[] activities)
		{
			var waitForActivityAction = new WaitForActivityAction(message, activities);

			var action = new BusinessProcessModelAction
			{
				Action = waitForActivityAction,
				ModelActionType = BusinessProcessModelActionType.WaitForActivity
			};

			this.ModelActions.Add(action);

			this.Activities.Add(waitForActivityAction);

			return this;
		}

		/// <summary>
		/// This will signal the completion of the processes, activities and tasks for the given <seealso cref="Actor">actor</seealso>
		/// </summary>
		public BusinessProcessModelTriggerCondition Complete()
		{
			var completeAction = new CompleteAction(true);

			var action = new BusinessProcessModelAction
			{
				Action = completeAction,
				ModelActionType = BusinessProcessModelActionType.Complete
			};

			this.ModelActions.Add(action);

			return this;
		}


	}
}