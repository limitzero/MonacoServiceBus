using System;
using System.Collections.Generic;
using System.IO;

namespace Monaco.Bus.MessageManagement.Serialization
{
	/// <summary>
	/// Contract for message serialization/de-serialization
	/// </summary>
	public interface ISerializationProvider : IDisposable
	{
		void Initialize(IEnumerable<Type> types);

		/// <summary>
		/// Deserializes a xml-string based representation of the object into the corresponding concrete type.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		object Deserialize(string instance);

		/// <summary>
		/// Deserializes a <seealso cref="Stream"/> representation of the object into the corresponding concrete type.
		/// </summary>
		/// <param name="stream"><see cref="Stream"/> containing the contents to create into a concrete instance.</param>
		/// <returns></returns>
		object Deserialize(Stream stream);

		/// <summary>
		/// Deserializes a <seealso cref="Stream"/> representation of the object into the corresponding concrete type.
		/// </summary>
		/// <param name="bytes"><see cref="byte"/> containing the contents to create into a concrete instance.</param>
		/// <returns></returns>
		object Deserialize(byte[] bytes);

		TMessage Deserialize<TMessage>(byte[] message) where TMessage : class;

		TMessage Deserialize<TMessage>(Stream message) where TMessage : class;

		TMessage Deserialize<TMessage>(string message) where TMessage : class;

		/// <summary>
		/// Serializes an object representation of the object into the corresponding string representation.
		/// </summary>
		/// <param name="message">Serializable object.</param>
		/// <returns>
		/// <seealso cref="string"/>
		/// </returns>
		string Serialize(object message);

		/// <summary>
		/// Serializes an object representation of the object into the corresponding array of bytes representation.
		/// </summary>
		/// <param name="message">Serializable object.</param>
		/// <returns>
		///  Array of <seealso cref="byte"/>
		/// </returns>
		byte[] SerializeToBytes(object message);

		Stream SerializeToStream(object message);

		void AddType(Type newType);

		void AddTypes(ICollection<Type> newTypes);
		void Initialize();
	}
}