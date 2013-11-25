using System.Collections.Generic;
using Monaco.Modelling.BusinessModel.Elements;

namespace Monaco.Modelling.BusinessModel.Actions
{
	public class WaitForActivityAction : IModelAction
	{
		public ICollection<Activity> Activities { get; private set; }
		public Message Message { get; private set; }

		public WaitForActivityAction(params Activity[] activities)
		{
			this.Activities = activities;
		}

		public WaitForActivityAction(Message message, params Activity[] activities)
		{
			this.Activities = activities;
			this.Message = message;
		}

	}
}