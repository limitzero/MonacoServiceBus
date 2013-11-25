using System;
using System.Collections.Generic;
using Monaco.Modelling.BusinessModel.Actions;
using Monaco.Modelling.BusinessModel.Elements;
using Monaco.Modelling.BusinessModel.Internals;

namespace Monaco.Modelling.BusinessModel
{
	[Serializable]
	public class BusinessServiceDefinition
	{
		public Capability Capability { get; set; }
		public Message Message { get; set; }
		public BusinessServiceProcessStage Stage { get; set; }

		/// <summary>
		/// Gets the set of model actions that will be taken by the process model 
		/// in order to realize the service model.
		/// </summary>
		public IList<BusinessProcessModelAction> ModelActions { get; set; }

		public BusinessServiceDefinition(Message message, BusinessServiceProcessStage stage)
		{
			Message = message;
			Stage = stage;
			this.ModelActions = new List<BusinessProcessModelAction>();
		}

		/// <summary>
		/// This will execute a particular task within the service model for a given <seealso cref="Capability">capability</seealso>
		/// </summary>
		/// <param name="task">Current task to execute</param>
		/// <returns></returns>
		public BusinessServiceDefinition ExecuteTask(Task task)
		{
			var tasks = new List<Task>();
			tasks.Add(task);

			var executeTaskAction = new ExecuteTaskAction(this.Message, tasks);

			var action = new BusinessProcessModelAction
			{
				Action = executeTaskAction,
				ModelActionType = BusinessProcessModelActionType.ExecuteTask
			};

			this.ModelActions.Add(action);

			return this;
		}

		/// <summary>
		/// This will execute a particular task within the service model for a given <seealso cref="Capability">capability</seealso>
		/// and return a <seealso cref="Message">message</seealso> that will be used by other services to realize the defined 
		/// business capability.
		/// </summary>
		/// <param name="task">Current task to execute</param>
		/// <returns></returns>
		public BusinessServiceDefinition ExecuteTaskAndReturnMessage(Task task, Message response, Message exception = null)
		{
			var tasks = new List<Task>();
			tasks.Add(task);

			var executeTaskAction = new ExecuteTaskAction(this.Message, tasks, response, exception, false);

			var action = new BusinessProcessModelAction
			{
				Action = executeTaskAction,
				ModelActionType = BusinessProcessModelActionType.ExecuteTask
			};

			this.ModelActions.Add(action);

			return this;
		}

		/// <summary>
		/// This will set the current state of the business process inside of the service model representing the condition the 
		/// process should be in after the set of tasks are completed.
		/// </summary>
		/// <param name="state"></param>
		/// <returns></returns>
		public BusinessServiceDefinition SetCurrentStateTo(ProcessingState state)
		{
			// always append the state to the last action that was defined
			var actions = new List<BusinessProcessModelAction>(this.ModelActions).ToArray();
			Array.Reverse(actions);
			actions[0].State = state;

			return this;
		}

		/// <summary>
		/// This will denote a portion in the process where an external activity needs to be completed in order for the process to 
		/// move forward to completion for the given <seealso cref="Actor">actor</seealso>
		/// </summary>
		/// <param name="activity">The external human dependent action that will occur outside the bounds of the service model but 
		/// will whose output will be used as input for other service model processes.</param>
		/// <returns></returns>
		public BusinessServiceDefinition WaitForActivity(Activity activity)
		{
			return WaitForActivity(activity, null);
		}

		/// <summary>
		/// This will denote a portion in the process where an external activity needs to be completed in order for the process to 
		/// move forward to completion for the given <seealso cref="Actor">actor</seealso>
		/// </summary>
		/// <param name="activity">External or internal activity to be executed outside the bounds of the process</param>
		/// <param name="response">Message to be produced after the activity has completed.</param>
		/// <returns></returns>
		public BusinessServiceDefinition WaitForActivity(Activity activity, Message response)
		{
			var waitForActivityAction = new WaitForActivityAction(response, activity);

			var action = new BusinessProcessModelAction
			{
				Action = waitForActivityAction,
				ModelActionType = BusinessProcessModelActionType.WaitForActivity
			};

			this.ModelActions.Add(action);
			return this;
		}

		/// <summary>
		/// This will signal the completion of the processes, activities and tasks for the given <seealso cref="Actor">actor</seealso>
		/// </summary>
		public BusinessServiceDefinition ThenComplete()
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