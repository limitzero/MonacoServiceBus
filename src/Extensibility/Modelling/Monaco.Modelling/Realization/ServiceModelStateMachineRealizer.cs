using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Monaco.Extensions;
using Monaco.Modelling.BusinessModel;
using Monaco.Modelling.BusinessModel.Actions;
using Monaco.Modelling.BusinessModel.Elements;
using Monaco.Modelling.BusinessModel.Internals;
using Monaco.StateMachine;

namespace Monaco.Modelling.Realization
{
	public class ServiceModelStateMachineRealizer : IRealizer
	{
		private const string SAGA_STATE_NAME_PREFIX = "WaitingFor";
		private const string TAB = "\t";
		private const string DOUBLE_TAB = "\t\t";
		private string _concreteModel = string.Empty;

		public string Realize(IBusinessProcessModel processModel,
			Capability capability,
			IEnumerable<BusinessServiceDefinition> definitions)
		{
			var results = string.Empty;

			using (var stream = new MemoryStream())
			{
				var trace = new TextWriterTraceListener(stream);

				RealizeCapability(processModel, capability, definitions, trace);

				trace.Flush();
				stream.Seek(0, SeekOrigin.Begin);

				using (TextReader reader = new StreamReader(stream))
				{
					 results = reader.ReadToEnd();
				}

			}

			_concreteModel = this.CreateConcreteModel(processModel, results);
			return results;
		}

		public string GetConcreteModel()
		{
			return _concreteModel;
		}

		private string CreateConcreteModel(IBusinessProcessModel model, string contents)
		{
			var builder = new StringBuilder();

			builder.Append("using System;").Append(Environment.NewLine);
			builder.Append("using System.Collections.Generic;").Append(Environment.NewLine);
			builder.Append("using System.Text;").Append(Environment.NewLine);
			builder.Append("using Monaco;").Append(Environment.NewLine);
			builder.Append("using Monaco.Sagas;").Append(Environment.NewLine);
			builder.Append("using Monaco.Sagas.StateMachine;").Append(Environment.NewLine);
			builder.Append(Environment.NewLine);

			builder.Append("namespace ")
				.Append(string.Concat(model.GetType().Assembly.GetName().Name, ".ServiceModel"))
				.Append(Environment.NewLine)
				.Append("{").Append(Environment.NewLine)
				.Append(contents)
				.Append("}").Append(Environment.NewLine);

			return builder.ToString();
		}

		private void RealizeCapability(
			IBusinessProcessModel processModel,
			Capability capability,
			IEnumerable<BusinessServiceDefinition> definitions,
			TextWriterTraceListener trace)
		{
			trace.IndentLevel = 1;

			// create the saga state machine data:
			trace.WriteLine("[Serializable]");

			trace.WriteLine(
				string.Format("public class {0}SagaStateMachineData : {1}",
							  capability.Name,
							  typeof(IStateMachineData).Name));

			trace.WriteLine("{");

			trace.IndentLevel = 2;

			var properties = typeof (IStateMachineData).GetProperties(BindingFlags.Public | BindingFlags.Instance);
			new List<PropertyInfo>(properties)
				.ForEach(p => trace.WriteLine(string.Format("public virtual {0} {1} {{get; set;}}", p.PropertyType.Name,  p.Name)));

			trace.IndentLevel = 1;

			trace.WriteLine("}");

			trace.WriteLine(string.Empty);

			trace.WriteLine(string.Format("// Business capability of '{0}' for role(s)/actor(s) '{1}' " +
										  "realized as '{2}SagaStateMachine' from business process model '{3}'",
										  capability.Name,
										  capability.GetActors(),
										  capability.Name,
										  processModel.GetType().Name));

			if (!string.IsNullOrEmpty(capability.Description))
			{

				if (!string.IsNullOrEmpty(capability.Description))
				{
					trace.WriteLine("/// <summary> ");
					trace.WriteLine(string.Format("/// {0}", capability.Description));
					trace.WriteLine("/// </summary> ");
				}
			}

			trace.WriteLine(string.Format("public class {0}SagaStateMachine :", capability.Name));

			trace.IndentLevel = 2;

			trace.WriteLine(string.Format("SagaStateMachine<{0}SagaStateMachineData>,",
										  capability.Name));
			trace.IndentLevel = 1; 

			var messages = RealizeMessageOrchestrations(processModel, definitions, trace);

			trace.WriteLine("{");

			RealizePrivateVariables(messages, trace);

			RealizeEvents(messages, trace);
			RealizeStates(processModel, definitions, trace);

			RealizeMessageConsumers(messages, trace);

			RealizeTasks(capability, definitions, trace);

			trace.WriteLine("public override void Define()");
			trace.WriteLine("{");

			// provide the definition of the saga state machine:
			RealizeStateMachine(capability, definitions, trace);

			trace.WriteLine("}");
			trace.WriteLine(string.Empty);

			// end of the saga state machine:
			trace.WriteLine("}");
		}

		private static void RealizePrivateVariables(IEnumerable<string> messages, TextWriterTraceListener trace)
		{
			trace.IndentLevel = 2;

			foreach (var message in messages)
			{
				trace.WriteLine(string.Format("private {0} {1} = new {0}();", message, message.ToLower()));
				trace.WriteLine(string.Empty);
			}

			trace.IndentLevel = 1;
		}

		private List<string> RealizeMessageOrchestrations(IBusinessProcessModel processModel,
														 IEnumerable<BusinessServiceDefinition> definitions,
														 TextWriterTraceListener trace)
		{
			var messages = new List<string>();
			var statement = string.Empty;
			var delimeter = string.Concat(",", Environment.NewLine);
			var segments = new List<string>();

			trace.IndentLevel = 2;

			foreach (var definition in definitions)
			{
				if (definition.Stage == BusinessServiceProcessStage.Start)
				{
					messages.AddUnique(definition.Message.Name);

					statement = string.Format("StartedBy<{0}>", definition.Message.Name);
					segments.AddUnique(statement);
				}

				if (definition.Stage == BusinessServiceProcessStage.Next)
				{
					messages.AddUnique(definition.Message.Name);

					statement = string.Format("OrchestratedBy<{0}>", definition.Message.Name);
					segments.AddUnique(statement);
				}

				foreach (var modelAction in definition.ModelActions)
				{
					if (modelAction.Action is WaitForActivityAction)
					{
						var action = modelAction.Action as WaitForActivityAction;

						if (action.Message != null)
						{
							messages.AddUnique(action.Message.Name);

							statement = string.Format("OrchestratedBy<{0}>", action.Message.Name);
							segments.AddUnique(statement);
						}
					}
				}
			}

			for(int index = 0; index < segments.Count-1; index++)
			{
				trace.WriteLine(string.Concat(segments[index], ", "));
			}
			trace.WriteLine(segments[segments.Count -1]);

			//var conditions = segments.Aggregate(string.Empty, (current, segment) => current + segment);
			//trace.WriteLine(conditions.TrimEnd(delimeter.ToCharArray()));

			trace.IndentLevel = 1;

			return messages;
		}

		private static void RealizeEvents(IEnumerable<string> messages, TextWriterTraceListener trace)
		{
			trace.IndentLevel = 2;

			foreach (var message in messages)
			{
				trace.WriteLine(string.Format("// Event created for receiving message '{0}'", message));
				trace.WriteLine(string.Format("public Event<{0}> {0} {{get; set;}}", message));
				trace.WriteLine(string.Empty);
			}

			trace.IndentLevel = 1;
		}

		private static void RealizeStates(IBusinessProcessModel processModel,
								   IEnumerable<BusinessServiceDefinition> definitions,
								   TextWriterTraceListener trace)
		{
			trace.IndentLevel = 2;

			var waitActivityActions = new List<WaitForActivityAction>();

			foreach (var definition in definitions)
			{
				var activities = (from activity in definition.ModelActions
								  where activity.Action is WaitForActivityAction
								  select activity.Action as WaitForActivityAction)
								  .Distinct().ToList();

				if (activities != null & activities.Count > 0)
					waitActivityActions.AddRange(activities);
			}

			foreach (var waitActivity in waitActivityActions)
				foreach (var activity in waitActivity.Activities)
				{
					if (activity == null) continue;

					trace.WriteLine(string.Format("// State created to wait for activity '{0}':", activity.Name));
					trace.WriteLine(string.Format("public State " + SAGA_STATE_NAME_PREFIX + "{0} {{get; set;}}", activity.Name));
					trace.WriteLine(string.Empty);
				}

			trace.IndentLevel = 1;
		}

		private static void RealizeMessageConsumers(IEnumerable<string> messages, TextWriterTraceListener trace)
		{
			trace.IndentLevel = 2;

			foreach (var message in messages)
			{
				trace.WriteLine(string.Format("public void Consume({0} message)", message));
				trace.WriteLine("{");

				trace.IndentLevel = 3; 
				trace.WriteLine(string.Format("this.{0} = message;", message.ToLower()));
				trace.IndentLevel = 2; 
				
				trace.WriteLine("}");
				trace.WriteLine(string.Empty);				
			}

			trace.IndentLevel = 1;
		}

		private static void RealizeTasks(Capability capability,
										 IEnumerable<BusinessServiceDefinition> definitions,
										 TextWriterTraceListener trace)
		{

			trace.IndentLevel = 2;

			string taskFormat = string.Concat("\t// Realization of task '{0}' from capability '{1}'",
											  System.Environment.NewLine,
											  "\tpublic void {0}()",
											  System.Environment.NewLine,
											  "\t{{",
											  System.Environment.NewLine,
											  "\t\t{2}",
											  System.Environment.NewLine,
											  "\t}}",
											  System.Environment.NewLine,
											  System.Environment.NewLine);


			var taskActions = new List<ExecuteTaskAction>();


			foreach (var definition in definitions)
			{
				var actions = (from activity in definition.ModelActions
							   where activity.Action is ExecuteTaskAction
							   select activity.Action as ExecuteTaskAction)
								  .Distinct().ToList();

				if (actions != null & actions.Count > 0)
					taskActions.AddRange(actions);
			}

			foreach (var task in taskActions)
			{

				foreach (var theTask in task.Tasks)
				{

					string methodBody = string.Empty;
					string completionBody = string.Empty;

					if(task.CanMarkAsCompleted)
					{
						completionBody = "MarkAsCompleted();";
					}

					var tryBlock = new StringBuilder();
					tryBlock
						.Append("try")
						.Append(Environment.NewLine)
						.Append(DOUBLE_TAB)
						.Append("{{")
						.Append(Environment.NewLine)
						.Append(DOUBLE_TAB).Append(TAB)
						.Append("// business logic goes here and raise exception if conditions are not satisfied:")
						.Append(Environment.NewLine)
						.Append(DOUBLE_TAB).Append(TAB)
						.Append("{0}")
						.Append(Environment.NewLine)
						.Append(DOUBLE_TAB)
						.Append("}}").Append(Environment.NewLine);

					var catchBlock = new StringBuilder();
					catchBlock.Append(DOUBLE_TAB)
						.Append("catch(System.Exception exception)")
						.Append(Environment.NewLine)
						.Append(DOUBLE_TAB)
						.Append("{{")
						.Append(Environment.NewLine)
						.Append(DOUBLE_TAB).Append(TAB)
						.Append("// raise the exception message to all concerned parties (if necessary):")
						.Append(Environment.NewLine)
						.Append(DOUBLE_TAB).Append(TAB)
						.Append("{0}")
						.Append(Environment.NewLine)
						.Append(DOUBLE_TAB)
						.Append("}}").Append(Environment.NewLine);

					if (task.OutputMessage == null & task.ExceptionMessage != null)
					{
						methodBody = string.Concat(methodBody,
												   string.Format(tryBlock.ToString(), string.Empty, completionBody));

						methodBody = string.Concat(methodBody,
						   string.Format(catchBlock.ToString(),
							string.Concat("this.Bus.Publish(new ", task.ExceptionMessage.Name, "());")));
					}

					if (task.OutputMessage != null & task.ExceptionMessage == null)
					{
						methodBody = string.Concat(methodBody,
						   string.Format(tryBlock.ToString(),
							string.Concat("this.Bus.Publish(new ", task.OutputMessage.Name, "());"),
							completionBody));

						methodBody = string.Concat(methodBody,
							string.Format(catchBlock.ToString(), string.Empty));
					}

					if (task.OutputMessage != null & task.ExceptionMessage != null)
					{
						methodBody = string.Concat(methodBody,
						   string.Format(tryBlock.ToString(),
							string.Concat("this.Bus.Publish(new ", task.OutputMessage.Name, "());"),
							completionBody));

						methodBody = string.Concat(methodBody,
						   string.Format(catchBlock.ToString(),
							string.Concat("this.Bus.Publish(new ", task.ExceptionMessage.Name, "());")));
					}

					trace.Write(string.Format(taskFormat, theTask.Name, capability.Name, methodBody));
				}
			}

			trace.IndentLevel = 1;
		}

		private static void RealizeStateMachine(Capability capability,
												IEnumerable<BusinessServiceDefinition> definitions,
												TextWriterTraceListener trace)
		{

			var segments = new List<string>();

			int waitEncountered = 0;
			bool isCompleteDefined = false;
			WaitForActivityAction lastTransition = null;
			var transitions = new Dictionary<int, WaitForActivityAction>();

			foreach (var definition in definitions)
			{
				StringBuilder builder = new StringBuilder();
				var stage = definition.Stage; 

				// build the "Initially" part from the defined message acceptance stages:
				if (stage == BusinessServiceProcessStage.Start)
				{
					builder.Append(TAB).Append("Initially(")
							.Append(System.Environment.NewLine)
							.Append(string.Concat(DOUBLE_TAB, TAB))
							.Append("When(").Append(definition.Message.Name).Append(")")
							.Append(System.Environment.NewLine);
				}

				// build the "Also" part from the defined message acceptance stages
				// (parallel message acceptance after the start point that does not have a 
				// state transition to trigger message acceptance):
				if (definition.Stage == BusinessServiceProcessStage.Next)
				{
					if (transitions.Count > 0)
					{
						var currentTransition = transitions.Last();
						BuildWhileSegments(definition.ModelActions.ToList(), segments,
							currentTransition.Value, 1, definition.Message.Name);
					}
					else
					{
						stage = BusinessServiceProcessStage.Also;
					}

					// need to skip the segment definitions because they will be handled
					// by code above to compute the "While" condition:
					// continue;
				}

				// build the "Also" part from the defined message acceptance stages:
				if (stage == BusinessServiceProcessStage.Also)
				{
					builder.Append(DOUBLE_TAB).Append("Also(")
							.Append(System.Environment.NewLine)
							.Append(string.Concat(DOUBLE_TAB, TAB))
							.Append("When(").Append(definition.Message.Name).Append(")")
							.Append(System.Environment.NewLine);
				}

				

				// build the subsequent message actions from the model actions on the service definition:
				foreach (var modelAction in definition.ModelActions)
				{
					if (lastTransition != null) break;

					if (modelAction.ModelActionType == BusinessProcessModelActionType.ExecuteTask)
					{
						var executeTaskAction = modelAction.Action as ExecuteTaskAction;

						// Do(() => expression)
						builder.Append(string.Concat(DOUBLE_TAB, DOUBLE_TAB))
							.Append(BuildDoAction(executeTaskAction))
							.Append(System.Environment.NewLine);
					}

					if (modelAction.ModelActionType == BusinessProcessModelActionType.WaitForActivity)
					{
						var waitForActivityAction = modelAction.Action as WaitForActivityAction;

						if (waitForActivityAction.Message != null)
						{
							// TransitionTo<State>()
							builder.Append(string.Concat(DOUBLE_TAB, DOUBLE_TAB))
								.Append(BuildTransitionToAction(waitForActivityAction))
								.Append(System.Environment.NewLine);

							lastTransition = waitForActivityAction;
						}
					}

					if (modelAction.ModelActionType == BusinessProcessModelActionType.Complete)
					{
						// Complete()
						builder.Append(string.Concat(DOUBLE_TAB, DOUBLE_TAB))
							.Append(BuildCompleteAction())
							.Append(System.Environment.NewLine);

						isCompleteDefined = true;
						break;
					}

					waitEncountered++;
				}

				// complete the state machine since no message on the transition was found:
				if (lastTransition == null)
				{
					if (!isCompleteDefined)
					{
						builder.Append(string.Concat(DOUBLE_TAB, DOUBLE_TAB))
							.Append(BuildCompleteAction())
							.Append(System.Environment.NewLine);
					}

					isCompleteDefined = true;
				}

				// complete the "Initially" or "Also" segment(s):
				builder.Append(DOUBLE_TAB).Append(");")
					.Append(System.Environment.NewLine)
					.Append(System.Environment.NewLine);

				segments.AddUnique(builder.ToString());

				// build the state transistions from the current transition that has a message:
				if (!isCompleteDefined && lastTransition != null && lastTransition.Message != null)
				{
					transitions.Add(waitEncountered, lastTransition);
					BuildWhileSegments(definition.ModelActions.ToList(), segments, lastTransition, waitEncountered);
				}

				lastTransition = null;
			}

			var conditions = segments.Aggregate(string.Empty, (current, segment) => current + segment);

			trace.IndentLevel = 1;
			trace.Write(conditions);
		}

		private static void BuildWhileSegments(List<BusinessProcessModelAction> modelActions,
			List<string> segments,
			WaitForActivityAction lastTransition,
			int waitEncounteredPosition,
			string desiredMessage = null)
		{
			var isWhileDefined = false;
			var isCompleteDefined = false;
			WaitForActivityAction theWaitAction = null;
			StringBuilder builder = new StringBuilder();

			// exit conditions:
			if (waitEncounteredPosition > modelActions.Count
				|| waitEncounteredPosition == 0
				|| waitEncounteredPosition < 0) return;

			// zero-based collections, must step back one from found position
			// in order to extract the proper transition activity:
			waitEncounteredPosition--;

			// build the "While" segment(s) from the last transition:
			for (int index = waitEncounteredPosition; index < modelActions.Count(); index++)
			{
				var modelAction = modelActions[index];

				if (theWaitAction != null) break;

				if (!isWhileDefined)
				{
					var activity = lastTransition.Activities.FirstOrDefault() as Activity;

					var message = desiredMessage == null ? lastTransition.Message.Name : desiredMessage;

					builder.Append(DOUBLE_TAB).Append("While(")
						.Append(string.Concat(SAGA_STATE_NAME_PREFIX, activity.Name)).Append(",")
						.Append(System.Environment.NewLine)
						.Append(string.Concat(DOUBLE_TAB, TAB))
						//.Append("When(").Append(lastTransition.Message.Name).Append(")")
						.Append("When(").Append(message).Append(")")
						.Append(System.Environment.NewLine);

					isWhileDefined = true;
				}

				if (modelAction.ModelActionType == BusinessProcessModelActionType.Complete)
				{
					// Complete()
					builder.Append(string.Concat(DOUBLE_TAB, DOUBLE_TAB))
						.Append(BuildCompleteAction())
						.Append(System.Environment.NewLine);

					isCompleteDefined = true;
					break;
				}

				if (modelAction.ModelActionType == BusinessProcessModelActionType.ExecuteTask)
				{
					var executeTaskAction = modelAction.Action as ExecuteTaskAction;

					// Do(() => expression)
					builder.Append(string.Concat(DOUBLE_TAB, DOUBLE_TAB))
						.Append(BuildDoAction(executeTaskAction))
						.Append(System.Environment.NewLine);
				}

				if (modelAction.ModelActionType == BusinessProcessModelActionType.WaitForActivity)
				{
					var waitForActivityAction = modelAction.Action as WaitForActivityAction;

					if (waitForActivityAction.Message != null &&
						lastTransition.Message.Name != waitForActivityAction.Message.Name)
					{
						// TransitionTo<State>()
						builder.Append(string.Concat(DOUBLE_TAB, DOUBLE_TAB))
							.Append(BuildTransitionToAction(waitForActivityAction))
							.Append(System.Environment.NewLine);

						theWaitAction = waitForActivityAction;
						waitEncounteredPosition = index;
					}
				}

			}

			// complete the state machine since no message on the transition (wait activity) was found:
			if (theWaitAction == null)
			{
				if (!isCompleteDefined)
				{
					builder.Append(string.Concat(DOUBLE_TAB, DOUBLE_TAB))
						.Append(BuildCompleteAction())
						.Append(System.Environment.NewLine);
				}

				isCompleteDefined = true;
			}

			builder.Append(DOUBLE_TAB).Append(");")
				.Append(System.Environment.NewLine)
				.Append(System.Environment.NewLine);

			segments.AddUnique(builder.ToString());

			if (!isCompleteDefined)
				BuildWhileSegments(modelActions, segments, theWaitAction, waitEncounteredPosition);

		}

		private static string BuildDoAction(ExecuteTaskAction executeTaskAction)
		{
			var results = string.Empty;
			var doAction = ".Do(() => {@actions})";

			var taskActions = string.Empty;

			foreach (var task in executeTaskAction.Tasks)
			{
				taskActions = string.Concat(taskActions, "this.",
											task.Name, "();", " ");
			}

			if (string.IsNullOrEmpty(taskActions) == false)
			{
				results = doAction.Replace("@actions", taskActions).TrimEnd(" ".ToCharArray());
			}

			return results;
		}

		private static string BuildTransitionToAction(WaitForActivityAction waitForActivityAction)
		{
			var results = string.Empty;

			var transitionAction = ".TransitionTo({0})";

			var state = (from activity in waitForActivityAction.Activities
						 select activity).First();

			results = string.Format(transitionAction, string.Concat(SAGA_STATE_NAME_PREFIX, state.Name));

			return results;
		}

		private static string BuildCompleteAction()
		{
			var completeAction = ".Complete()";
			return completeAction;
		}


	}
}