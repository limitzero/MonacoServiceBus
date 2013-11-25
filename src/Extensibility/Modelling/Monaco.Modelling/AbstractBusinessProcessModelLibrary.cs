using System.Linq;
using Monaco.Modelling.BusinessModel.Elements;

namespace Monaco.Modelling
{
	/// <summary>
	/// Abstract class that will hold the re-usable assets of the business process model.
	/// </summary>
	public abstract class AbstractBusinessProcessModelLibrary
	{
		protected AbstractBusinessProcessModelLibrary()
		{
			this.PrepareModelElement<Capability>();
			this.PrepareModelElement<Actor>();
			this.PrepareModelElement<Activity>();
			this.PrepareModelElement<Message>();
			this.PrepareModelElement<Cost>();
			this.PrepareModelElement<Task>();
			this.Define();
		}

		/// <summary>
		/// User-defined option to add any descriptive information to the model elements.
		/// </summary>
		public virtual void Define()
		{
		}

		private void PrepareModelElement<TElement>() where TElement : IModelElement, new()
		{
			var properties = (from property in this.GetType().GetProperties()
			                  where property.PropertyType == typeof(TElement)
			                  select property).Distinct().ToList();

			foreach (var property in properties)
			{
				var element = new TElement();
				element.Name = property.Name;
				property.SetValue(this, element, null);
			}
		}
	}
}