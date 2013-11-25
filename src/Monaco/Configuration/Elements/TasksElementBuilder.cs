using System;
using System.Collections;
using Castle.Core.Configuration;
using Monaco.Bus.Agents.Scheduler;
using Monaco.Bus.Agents.Scheduler.Tasks.Configuration;
using Monaco.Bus.Agents.Scheduler.Tasks.Configuration.Impl;
using Monaco.Bus.Internals.Reflection;

namespace Monaco.Configuration.Elements
{
	public class TasksElementBuilder : BaseElementBuilder
	{
		private const string _elementname = "tasks";

		public override bool IsMatchFor(string name)
		{
			return name.Trim().ToLower() == _elementname.Trim().ToLower();
		}

		public override void Build(Castle.Core.Configuration.IConfiguration configuration)
		{
			for (int index = 0; index < configuration.Children.Count; index++)
			{
				Castle.Core.Configuration.IConfiguration taskElement = configuration.Children[index];
				string theName = taskElement.Attributes["name"];
				string theReference = taskElement.Attributes["ref"];
				string theType = taskElement.Attributes["type"];
				string theMethod = taskElement.Attributes["method"] ?? string.Empty;
				string theInterval = taskElement.Attributes["interval"];
				string theHaltFlag = taskElement.Attributes["haltOnError"];
				string theForceStartFlag = taskElement.Attributes["forceStart"];

				Guard(theReference, theType, theMethod, theInterval);

				object component = null;

				if (string.IsNullOrEmpty(theReference) == false)
				{
					component = Container.Resolve<IReflection>().BuildInstance(theReference.Trim());
				}
				else
				{
					component = Container.Resolve<IReflection>().BuildInstance(theType);
				}

				ITaskConfiguration taskConfiguration = new TaskConfiguration();

				taskConfiguration.TaskName = theName;
				taskConfiguration.ComponentInstance = component;
				taskConfiguration.MethodName = theMethod == string.Empty ? typeof (Produces<>).GetMethods()[0].Name : theMethod;
				taskConfiguration.Interval = theInterval;
				taskConfiguration.HaltOnError = theHaltFlag.Trim().ToLower() == "true" ? true : false;
				taskConfiguration.ForceStart = theForceStartFlag.Trim().ToLower() == "true" ? true : false;

				var scheduler = Container.Resolve<IScheduler>();

				scheduler.CreateFromConfiguration(taskConfiguration);
			}
		}

		private void Guard(string theReference, string theType, string theMethod, string theInterval)
		{
			// inspect the values for the scheduled task:
			if (string.IsNullOrEmpty(theReference) && string.IsNullOrEmpty(theType))
				throw new Exception(
					"For the scheduled task, either a reference to the component must be given [attribute = @ref] " +
					"or the fully qualified type of the component {attribute =@type] must be supplied.");

			if (!string.IsNullOrEmpty(theReference) && !string.IsNullOrEmpty(theType))
				throw new Exception(
					"The scheduled task can not have both a component reference [attribute = @ref] and a type [attribute =@typer] defined.");

			//if (string.IsNullOrEmpty(theMethod))
			//    throw new Exception("The method that will be called on the scheduled component must be supplied [attribute = @method].");

			if (string.IsNullOrEmpty(theInterval))
				throw new Exception("An interval must be suppled for the scheduled task in the form of  dd:hh:mm:ss");

			if (theInterval.Split(new[] {':'}).Length != 3) // hours:minutes:seconds
				throw new Exception("An interval must be suppled for the scheduled task in the form of  dd:hh:mm:ss => " +
				                    theInterval);
		}
	}
}