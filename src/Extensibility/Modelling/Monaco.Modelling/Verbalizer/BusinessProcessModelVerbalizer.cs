using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Monaco.Modelling.BusinessModel;
using Monaco.Modelling.BusinessModel.Actions;
using Monaco.Modelling.BusinessModel.Elements;

namespace Monaco.Modelling.Verbalizer
{
	/// <summary>
	/// Facility for verbalizing constructed business process models.
	/// </summary>
	public class BusinessProcessModelVerbalizer
	{
		public string Verbalize<TBusinessProcessModel>()
			where TBusinessProcessModel : class, IBusinessProcessModel, new()
		{
			var model = new TBusinessProcessModel();
			return Verbalize(model);
		}

		public string Verbalize(IBusinessProcessModel model)
		{
			string results = string.Empty;
			model.Define();

			using (var stream = new MemoryStream())
			{

				string name = model.Name ?? model.GetType().Name;
				var trace = new System.Diagnostics.TextWriterTraceListener(stream);

				var preamble = "Business process model for : " + name;

				if (string.IsNullOrEmpty(model.Description) == false)
				{
					preamble = string.Concat(preamble, System.Environment.NewLine, "Description:",
											 System.Environment.NewLine, model.Description);
				}

				var separator = string.Empty;
				foreach (var c in preamble)
					separator += "=";

				trace.IndentSize = 2;
				trace.IndentLevel = 0;

				trace.WriteLine(preamble);
				trace.WriteLine(separator);

				foreach (var capabilityServiceDefinition in model.CapabilityServiceDefinitions)
				{
					VerbalizeCapabilityServiceDefinition(capabilityServiceDefinition, trace);
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

		private static void VerbalizeCapabilityServiceDefinition(KeyValuePair<Capability, List<BusinessServiceDefinition>> capabilityServiceDefinitions, 
			TextWriterTraceListener trace)
		{
			var capability = capabilityServiceDefinitions.Key;

			var definitions = capabilityServiceDefinitions.Value;

			var start = (from match in definitions
			             where match.Stage == BusinessServiceProcessStage.Start
			             select match).Distinct().ToList();

			var next = (from match in definitions
						where match.Stage == BusinessServiceProcessStage.Next
						select match).Distinct().ToList();

			foreach (var serviceDefinition in start)
			{

				trace.IndentLevel = 0;
				trace.WriteLine(string.Format("Initially, for the capability of '{0}' realized by '{1}'",
											  capability.Name,
											  capability.GetActors()));
				trace.IndentLevel = 1;
				trace.WriteLine(string.Format("when the '{0}' message is received, the '{1}' will",
											  serviceDefinition.Message.Name,
											  capability.GetActors()));

				trace.IndentLevel = 0;

				WriteServiceDefinition(capabilityServiceDefinitions.Key, serviceDefinition,  trace);

				trace.WriteLine(string.Empty);

			}

			foreach (var serviceDefinition in next)
			{
				trace.IndentLevel = 0;
				trace.WriteLine(string.Format("Next, for the capability of '{0}' realized by '{1}'",
											  capability.Name,
											  capability.GetActors()));
				trace.IndentLevel = 1;
				trace.WriteLine(string.Format("when the '{0}' message is received, the '{1}' will",
											  serviceDefinition.Message.Name,
											  capability.GetActors()));

				trace.IndentLevel = 0;

				WriteServiceDefinition(capabilityServiceDefinitions.Key, serviceDefinition, trace);

				trace.WriteLine(string.Empty);
			}

		}

		private static void WriteServiceDefinition(Capability capability, BusinessServiceDefinition serviceDefinition, TextWriterTraceListener trace)
		{
			foreach (var modelAction in serviceDefinition.ModelActions)
			{

				//if (serviceDefinition.Stage == BusinessServiceProcessStage.Start)
				//{
				//    trace.IndentLevel = 0;
				//    trace.WriteLine(string.Format("Initially, for the capability of '{0}' realized by '{1}'",
				//                                  capability.Name,
				//                                  capability.GetActors()));
				//    trace.IndentLevel = 1;
				//    trace.WriteLine(string.Format("when the '{0}' message is sent, the '{1}' will",
				//                                  capability.Name,
				//                                  capability.GetActors()));

				//    trace.IndentLevel = 0;
				//}

				//if (serviceDefinition.Stage == BusinessServiceProcessStage.Next)
				//{
				//    trace.IndentLevel = 0;
				//    trace.WriteLine(string.Format("Next, for the capability of '{0}' realized by '{1}'",
				//                                  capability.Name,
				//                                  capability.GetActors()));
				//    trace.IndentLevel = 1;
				//    trace.WriteLine(string.Format("when the '{0}' message is sent, the '{1}' will",
				//                                  capability.Name,
				//                                  capability.GetActors()));

				//    trace.IndentLevel = 0;
				//}

				if (modelAction.Action is ExecuteTaskAction)
				{
					var task = modelAction.Action as ExecuteTaskAction;
					string theTasks = string.Empty;

					trace.IndentLevel = 2;
					theTasks = task.Tasks.Aggregate(theTasks,
													(current, aTask) => string.Concat(current, "'", aTask.Name, "', "));
					theTasks = theTasks.TrimEnd(", ".ToCharArray());

					string statment = "execute the task(s) " + theTasks;

					if (task.OutputMessage != null)
						statment = string.Format(statment + " and send the message '{0}'", task.OutputMessage.Name);

					trace.WriteLine(statment);

					trace.IndentLevel = 0;
				}

				if (modelAction.Action is WaitForActivityAction)
				{
					var waitForActivity = modelAction.Action as WaitForActivityAction;
					string theActivities = string.Empty;

					theActivities = waitForActivity.Activities.Aggregate(theActivities,
													(current, aTask) => string.Concat(current, aTask.Name, ", "));
					theActivities = theActivities.TrimEnd(", ".ToCharArray());

					trace.IndentLevel = 2;
					string statement = string.Format("wait for the process(es) '{0}' to finish", theActivities);
					trace.WriteLine(statement);

					trace.IndentLevel = 3;

					if (waitForActivity.Message != null)
						trace.WriteLine(string.Format("which will produce the message '{0}'",
							waitForActivity.Message.Name));

					trace.IndentLevel = 0;
				}

				if (modelAction.Action is CompleteAction)
				{
					trace.IndentLevel = 0;
					trace.WriteLine("then complete");
					trace.WriteLine(string.Empty);
				}
			}
		}
	}
}