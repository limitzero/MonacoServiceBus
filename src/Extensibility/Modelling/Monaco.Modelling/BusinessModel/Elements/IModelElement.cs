namespace Monaco.Modelling.BusinessModel.Elements
{
	/// <summary>
	/// Basic contract for all model elements on the business process model.
	/// </summary>
	public interface IModelElement
	{
		/// <summary>
		/// Gets or sets the name of the model element.
		/// </summary>
		string Name { get; set; }

		/// <summary>
		/// Gets or sets the description of the model element.
		/// </summary>
		string Description { get; set; }
	}
}