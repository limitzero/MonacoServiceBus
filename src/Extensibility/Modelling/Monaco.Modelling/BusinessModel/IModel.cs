namespace Monaco.Modelling.BusinessModel
{
	public interface IModel<TModelLibrary> where TModelLibrary : AbstractBusinessProcessModelLibrary
	{
		/// <summary>
		/// Gets the underlying library of re-usable resources to construct the models.
		/// </summary>
		TModelLibrary ModelLibrary { get; }
	}
}