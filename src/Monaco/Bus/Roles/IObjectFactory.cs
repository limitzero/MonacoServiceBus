using System;
using System.Collections.Generic;

namespace Monaco.Bus.Roles
{
	public interface IObjectFactory<TContainer>
	{
		/// <summary>
		/// Gets the underlying object container implementation
		/// </summary>
		TContainer Container { get; set; }

		/// <summary>
		/// This will resolve an instance of a component from the underlying container.
		/// </summary>
		/// <typeparam name="TComponent">Type to resolve.</typeparam>
		/// <returns></returns>
		TComponent Resolve<TComponent>();

		/// <summary>
		/// This will resolve an instance of a component from the underlying container.
		/// </summary>
		/// <typeparam name="TComponent">Type to resolve.</typeparam>
		/// <returns></returns>
		TComponent Resolve<TComponent>(string key);

		/// <summary>
		/// This will resolve an instance of a component from the underlying container.
		/// </summary>
		/// <param name="component">The type of the component to find in the container.</param>
		/// <returns></returns>
		object Resolve(Type component);

		/// <summary>
		/// This will find all of the components of the well-known 
		/// type from the underlying container used by the message bus.
		/// </summary>
		/// <typeparam name="TComponent">Type to find all occurrences for.</typeparam>
		/// <returns></returns>
		ICollection<TComponent> ResolveAll<TComponent>();
	}
}