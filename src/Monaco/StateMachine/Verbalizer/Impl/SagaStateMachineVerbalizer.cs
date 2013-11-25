using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Castle.MicroKernel;
using Monaco.StateMachine.Internals;
using Monaco.StateMachine.Internals.Impl;
using Monaco.Extensions;
using Moq;

namespace Monaco.StateMachine.Verbalizer.Impl
{
	/// <summary>
	/// Class to inspect the current saga state machine and create the scenario text describing its behavior
	/// </summary>
	public class SagaStateMachineVerbalizer : ISagaStateMachineVerbalizer
	{
		#region ISagaStateMachineVerbalizer Members

		public string Verbalize<TSagaStateMachine>() where TSagaStateMachine : SagaStateMachine, new()
		{
			return Verbalize(new TSagaStateMachine());
		}

		public string Verbalize<TSagaStateMachine>(TSagaStateMachine sagaStateMachine)
			where TSagaStateMachine : SagaStateMachine
		{
			string results = string.Empty;

			// make sure to use a mock of the service bus to avoid actually running the defined conditions:
			var bus = new Mock<IServiceBus>().Object;
				//MockFactory.CreateServiceBusMock(new DefaultKernel());

			sagaStateMachine.Bus = bus;
			sagaStateMachine.Define();

			using (var stream = new MemoryStream())
			{
				string name = sagaStateMachine.Name ?? sagaStateMachine.GetType().Name;
				var trace = new TextWriterTraceListener(stream);
				string preamble = "Configuration for saga state machine : " + name;
				string separator = string.Empty;
				foreach (char c in preamble)
					separator += "=";

				trace.IndentSize = 2;
				trace.IndentLevel = 0;

				trace.WriteLine(preamble);
				trace.WriteLine(separator);

				// define the portion for "Initially" segment:
				var initially = sagaStateMachine.TriggerConditions
					.Where(x => x.Stage == SagaStateMachineStageType.Initially).SingleOrDefault();

				//WriteInitiallyPart(trace, initially);
				WriteEvents(SagaStateMachineStageType.Initially, trace, initially.Condition);

				trace.WriteLine(string.Empty);

				// define the portions for the "While" and "Also" segments:
				var segments = sagaStateMachine.TriggerConditions
					.Where(x => x.Stage != SagaStateMachineStageType.Initially).ToList();

				foreach (var segment in segments)
				{
					WriteEvents(segment.Stage, trace, segment.Condition);
					trace.WriteLine(string.Empty);
					//WriteWhilePart(trace, part.Value);
				}

				trace.Flush();
				stream.Seek(0, SeekOrigin.Begin);

				using (TextReader reader = new StreamReader(stream))
				{
					results = reader.ReadToEnd();
				}
			}

			return results;
		}

		#endregion

		private static void WriteEvents(SagaStateMachineStageType sagaStage, 
			TextWriterTraceListener trace,
		    ISagaEventTriggerCondition condition)
		{
			foreach (var messageAction in condition.MessageActions)
			{
				/* Initially when the [message] arrives, it will 
				*		[do, publish, send, reply, delay, execute]  
				*	 then  transition to [state]	|| then complete				
				*/

				if (sagaStage == SagaStateMachineStageType.Initially &&
				    messageAction.ActionType == SagaStateMachineMessageActionType.When)
				{
					trace.IndentLevel = 0;
					trace.WriteLine(string.Format("Initially when the '{0}' message arrives, it will ",
												 messageAction.Message.GetImplementationFromProxy().Name));
				}

				if (sagaStage == SagaStateMachineStageType.While &&
					messageAction.ActionType == SagaStateMachineMessageActionType.When)
				{
					/* While in state {state},  when (message arrives) it will 
					*		[do, publish, send, reply, delay, execute]  
					*	 then  transition to [state]	|| then complete				
					*/

					trace.IndentLevel = 0;
					trace.WriteLine(string.Format("While in state '{0}', when the '{1}' message arrives, it will ",
					                              condition.State.Name,
												  messageAction.Message.GetImplementationFromProxy().Name));
				}

				if (sagaStage == SagaStateMachineStageType.Also &&
					messageAction.ActionType == SagaStateMachineMessageActionType.When)
				{
					/* Also for any state, when (message arrives) it will 
					*		[do, publish, send, reply, delay, execute]  
					*	 then  transition to [state] || then complete				
					*/

					trace.IndentLevel = 0;
					trace.WriteLine(string.Format("Also for any state, when the '{0}' message arrives, it will ",
					                              messageAction.Message.GetImplementationFromProxy().Name));
				}

				switch (messageAction.ActionType)
				{
						// these are actions that can be taken for the current event (as defined by the message):
					case (SagaStateMachineMessageActionType.Do):
						trace.IndentLevel = 1;

						if (string.IsNullOrEmpty(messageAction.Note) == false)
						{
							trace.WriteLine(messageAction.Note);
						}
						else
						{
							trace.WriteLine("execute some custom code");
						}

						trace.IndentLevel = 0;

						break;

					case (SagaStateMachineMessageActionType.Publish):
						trace.IndentLevel = 1;
						trace.WriteLine(string.Format("publish the message '{0}' ",
													messageAction.Message.GetImplementationFromProxy().Name));
						trace.IndentLevel = 0;

						break;

					case (SagaStateMachineMessageActionType.Send):
						trace.IndentLevel = 1;
						trace.WriteLine(string.Format("send the message '{0}' ",
													  messageAction.Message.GetImplementationFromProxy().Name));
						trace.IndentLevel = 0;

						break;

					case (SagaStateMachineMessageActionType.SendToEndpoint):
						trace.IndentLevel = 1;
						trace.WriteLine(string.Format("send the message '{0}' to endpoint '{1}'",
													  messageAction.Message.GetImplementationFromProxy().Name,
						                              messageAction.Endpoint));
						trace.IndentLevel = 0;

						break;

					case (SagaStateMachineMessageActionType.Reply):
						trace.IndentLevel = 1;
						trace.WriteLine(string.Format("reply with the message '{0}' ",
							messageAction.Message.GetImplementationFromProxy().Name));
						trace.IndentLevel = 0;

						break;

					case (SagaStateMachineMessageActionType.Delay):
						trace.IndentLevel = 1;
						trace.WriteLine(
							string.Format(
								"delay publishing of the message '{0}' by '{1}' days, '{2}' hours, '{3}' minutes, and '{4}' seconds",
								messageAction.Message.GetImplementationFromProxy().Name,
								messageAction.Delay.Days,
								messageAction.Delay.Hours,
								messageAction.Delay.Minutes,
								messageAction.Delay.Seconds
								));
						trace.IndentLevel = 0;

						break;

						// transition and complete are finalization markers for the current event:
					case (SagaStateMachineMessageActionType.Transition):

						trace.IndentLevel = 0;

						string transistionStatement = string.Empty;

						if (condition.MessageActions.Count == 2)
						{
							transistionStatement = string.Format("transition to state '{0}' ", messageAction.State.Name);
						}
						else
						{
							transistionStatement = string.Format("then transition to state '{0}' ", messageAction.State.Name);
						}

						trace.WriteLine(transistionStatement);

						break;

					case (SagaStateMachineMessageActionType.Complete):

						trace.IndentLevel = 0;

						string completionStatement = string.Empty;

						if (condition.MessageActions.Count == 2)
						{
							completionStatement = "complete";
						}
						else
						{
							completionStatement = "then complete";
						}

						trace.WriteLine(completionStatement);

						break;
				}
			}
		}

	}
}