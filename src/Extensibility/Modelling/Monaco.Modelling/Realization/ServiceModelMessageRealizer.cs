using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Monaco.Modelling.BusinessModel;
using Monaco.Modelling.BusinessModel.Elements;

namespace Monaco.Modelling.Realization
{
	/// <summary>
	/// Realizer to create all of the messages that are defined or 
	/// in need for the state machine via the process model library
	/// </summary>
	public class ServiceModelMessageRealizer : IRealizer
	{
		private string concreteModel = string.Empty;

		public string Realize(IBusinessProcessModel processModel, 
			Capability capability, IEnumerable<BusinessServiceDefinition> definitions)
		{
			string results = string.Empty;

			using (var stream = new MemoryStream())
			{
				var trace = new TextWriterTraceListener(stream);

				trace.IndentLevel = 1;

				var library = processModel.GetLibrary();

				var messages = (from property in library.GetType().GetProperties()
				                where property.PropertyType == typeof (Message)
				                select property).Distinct().ToList();

				string messageFormat = "public class {0} : ISagaMessage {{ public Guid CorrelationId {{get; set}} }}";

				trace.WriteLine(string.Format("// Message(s) from business process model '{0}':",
				                              processModel.GetType().Name));
				trace.WriteLine(string.Empty);

				foreach (var message in messages)
				{
					var theMessage = message.GetValue(library, null) as Message;
				
					var statement = string.Format(messageFormat, theMessage.Name);

					if(!string.IsNullOrEmpty(theMessage.Description))
					{
						trace.WriteLine("/// <summary> ");
						trace.WriteLine(string.Format("/// {0}", theMessage.Description));
						trace.WriteLine("/// </summary> ");
					}

					trace.WriteLine(string.Concat(statement, Environment.NewLine));
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

		private string CreateConcreteModel(string contents)
		{
			var builder = new StringBuilder();

			builder.Append("using System;").Append(Environment.NewLine);
			builder.Append("using System.Collections.Generic;").Append(Environment.NewLine);
			builder.Append("using System.Text;").Append(Environment.NewLine);
			builder.Append("using Monaco;").Append(Environment.NewLine);
			builder.Append("using Monaco.StateMachine;").Append(Environment.NewLine);
			builder.Append(Environment.NewLine);

			builder.Append("namespace ")
				.Append(string.Concat(GetType().Assembly.GetName().Name, ".ServiceModel.Messages"))
				.Append(Environment.NewLine)
				.Append("{").Append(Environment.NewLine)
				.Append(contents)
				.Append("}").Append(Environment.NewLine);

			return builder.ToString();
		}

		public string GetConcreteModel()
		{
			return concreteModel;
		}
	}




}