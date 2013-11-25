using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Monaco.Modelling.BusinessModel;
using Monaco.Modelling.BusinessModel.Elements;
using Monaco.StateMachine;

namespace Monaco.Modelling.Realization
{
	/// <summary>
	/// Realizer component to create the basic saga state machine data 
	/// needed for the saga state machine when a concrete instance is 
	/// realized.
	/// </summary>
	public class ServiceModelSagaDataRealizer : IRealizer
	{
		private string concreteModel = string.Empty;

		public string Realize(IBusinessProcessModel processModel, Capability capability,
			IEnumerable<BusinessServiceDefinition> definitions)
		{
			string results = string.Empty;

			using (var stream = new MemoryStream())
			{
				var trace = new TextWriterTraceListener(stream);

				trace.IndentLevel = 1;

				trace.WriteLine(string.Format("// Persistance data that is kept for each service (implemented as a state machine) " +
					"from business process model '{0}':",
					processModel.GetType().Name));
				trace.WriteLine(string.Empty);

				// parse all capabilities and their service definitions:
				foreach (var theCapability in processModel.CapabilityServiceDefinitions.Keys)
				{
					var theDefinitions = processModel.CapabilityServiceDefinitions[theCapability];
					if (theDefinitions == null || theDefinitions.Count == 0) continue;

					// create the saga state machine data:
					trace.WriteLine("[Serializable]");

					trace.WriteLine(
						string.Format("public class {0}SagaStateMachineData : {1}",
						              theCapability.Name,
						              typeof (IStateMachineData).Name));
						
					trace.WriteLine("{");

					trace.IndentLevel = 2;

					var properties = typeof (IStateMachineData).GetProperties();

					foreach (var propertyInfo in properties)
					{
						trace.WriteLine(string.Format("public virtual {0} {1} {{ get; set; }}", 
							propertyInfo.PropertyType.Name, propertyInfo.Name));
					}

					trace.IndentLevel = 1;

					trace.WriteLine("}");

					trace.WriteLine(string.Empty);
				}

				trace.Flush();
				stream.Seek(0, SeekOrigin.Begin);

				using (TextReader reader = new StreamReader(stream))
				{
					results = reader.ReadToEnd();
				}
			}

			concreteModel = this.CreateConcreteModel(results);

			return results;

		}

		public string GetConcreteModel()
		{
			return concreteModel;
		}

		private string CreateConcreteModel(string contents)
		{
			var builder = new StringBuilder();

			builder.Append("using System;").Append(Environment.NewLine);
			builder.Append("using System.Collections.Generic;").Append(Environment.NewLine);
			builder.Append("using System.Text;").Append(Environment.NewLine);
			builder.Append("using Monaco;").Append(Environment.NewLine);
			builder.Append("using Monaco.StateMachines;").Append(Environment.NewLine);
			builder.Append(Environment.NewLine);

			builder.Append("namespace ")
				.Append(string.Concat(GetType().Assembly.GetName().Name, ".ServiceModel"))
				.Append(Environment.NewLine)
				.Append("{").Append(Environment.NewLine)
				.Append(contents)
				.Append("}").Append(Environment.NewLine);

			return builder.ToString();
		}
	}
}