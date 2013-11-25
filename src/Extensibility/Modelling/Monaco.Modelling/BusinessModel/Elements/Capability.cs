using System.Collections.Generic;

namespace Monaco.Modelling.BusinessModel.Elements
{
	/// <summary>
	/// A capability is a defined business function or service that has to be 
	/// realized in order to gain a benefit within the organization.
	/// </summary>
	public class Capability : IModelElement
	{
		public string Name { get; set; }

		public string Description { get; set; }

		/// <summary>
		/// Gets the <seealso cref="Actors">role and/or group</seealso> responsible for 
		/// carrying out the business function.
		/// </summary>
		public IList<Actor> Actors { get; private set; }

		/// <summary>
		/// Gets the set of defined costs associated with the capability.
		/// </summary>
		public IList<Cost> Costs { get; private set; }

		/// <summary>
		/// Gets the set of capabilities that this one depends on.
		/// </summary>
		public IList<Capability> Dependencies { get; private set; }

		public IList<Message> InputMessages { get; private set; }

		public IList<Message> OutputMessages { get; private set; }

		public IList<Message> ExceptionMessages { get; private set; }

		public CapabilityScore PerformanceScore { get; private set; }

		public CapabilityScore BusinessValueScore { get; private set; }

		public Capability()
		{
			this.Actors = new List<Actor>();
			this.Costs = new List<Cost>();
			this.InputMessages = new List<Message>();
			this.OutputMessages = new List<Message>();
			this.ExceptionMessages = new List<Message>();
			this.Dependencies = new List<Capability>();
		}

		public string GetActors()
		{
			var actors = string.Empty;
			var separator = ", ";

			foreach (var actor in this.Actors)
			{
				actors = string.Concat(actors, actor.Name, separator);
			}

			if (!string.IsNullOrEmpty(actors))
				actors = actors.Substring(0, actors.Length - separator.Length);

			return actors;
		}

		/// <summary>
		/// This will assign a <seealso cref="Actors">role</seealso> to carry 
		/// out the capability.
		/// </summary>
		/// <param name="actor"></param>
		public void AssignRole(Actor actor)
		{
			this.Actors.Add(actor);
		}

		public void AssignCosts(params Cost[] costs)
		{
			this.Costs = costs;
		}

		public void AssignInputMessages(params Message[] messages)
		{
			this.InputMessages = messages;
		}

		public void AssignOutputMessages(params Message[] messages)
		{
			this.OutputMessages = messages;
		}

		public void AssignExceptionMessages(params Message[] messages)
		{
			this.ExceptionMessages = messages;
		}

		public void AssignPerformanceScoreOf(CapabilityScore score)
		{
			this.PerformanceScore = score;
		}

		public void AssignBusinessValueScoreOf(CapabilityScore score)
		{
			this.BusinessValueScore = score;
		}

		public void AssignDependencies(params Capability[] capabilities)
		{
			foreach (var capability in capabilities)
			{
				this.Dependencies.Add(capability);
			}
		}



	}
}