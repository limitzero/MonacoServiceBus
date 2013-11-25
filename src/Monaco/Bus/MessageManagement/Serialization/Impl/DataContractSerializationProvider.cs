using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using Monaco.Configuration;

namespace Monaco.Bus.MessageManagement.Serialization.Impl
{
	public class DataContractSerializationProvider : ISerializationProvider
	{
		private static DataContractSerializer _serializer;
		private static List<Type> _types;
		private readonly IContainer container;
		private readonly object _types_lock = new object();

		public DataContractSerializationProvider(IContainer container)
		{
			this.container = container;
			if (_types == null)
				_types = new List<Type>();
		}

		#region ISerializationProvider Members

		public void Dispose()
		{
			if (_serializer != null)
			{
				_serializer = null;
			}

			if (_types != null)
			{
				_types.Clear();
				_types = null;
			}
		}

		public object Deserialize(string instance)
		{
			byte[] bytes = Encoding.ASCII.GetBytes(instance);
			return Deserialize(bytes);
		}

		public object Deserialize(Stream stream)
		{
			try
			{
				stream.Seek(0, SeekOrigin.Begin);
			}
			catch
			{
			}

			return _serializer.ReadObject(stream);
		}

		public object Deserialize(byte[] bytes)
		{
			object retval = null;

			using (var stream = new MemoryStream(bytes))
			{
				retval = _serializer.ReadObject(stream);
			}

			return retval;
		}

		public TMessage Deserialize<TMessage>(byte[] message) where TMessage : class
		{
			return (TMessage) Deserialize(message);
		}

		public TMessage Deserialize<TMessage>(Stream message) where TMessage : class
		{
			return Deserialize(message) as TMessage;
		}

		public TMessage Deserialize<TMessage>(string message) where TMessage : class
		{
			return (TMessage) Deserialize(message);
		}

		public string Serialize(object message)
		{
			string retval = string.Empty;

			if (_types.Exists(x => x.FullName == message.GetType().FullName) == false)
			{
				AddType(message.GetType());
				Initialize(_types);
			}

			using (var stream = new MemoryStream())
			{
				_serializer.WriteObject(stream, message);
				stream.Seek(0, SeekOrigin.Begin);

				var textconverter = new UTF8Encoding();
				retval = textconverter.GetString(stream.ToArray());
			}

			return retval;
		}

		public byte[] SerializeToBytes(object message)
		{
			string retval = Serialize(message);
			return Encoding.ASCII.GetBytes(retval);
		}

		public Stream SerializeToStream(object message)
		{
			var stream = new MemoryStream(SerializeToBytes(message));
			stream.Seek(0, SeekOrigin.Begin);
			return stream;
		}

		public void Initialize()
		{
			lock (_types_lock)
			{
				_serializer = new DataContractSerializer(typeof (object), _types.ToArray());
			}
		}

		public void Initialize(IEnumerable<Type> types)
		{
			lock (_types_lock)
			{
				_types = new List<Type>(types);
				_serializer = new DataContractSerializer(typeof (object), _types.ToArray());
			}
		}

		public void AddType(Type newType)
		{
			lock (_types_lock)
			{
				if (!_types.Contains(newType))
				{
					if (newType.IsInterface)
					{
						Type concreteType = null;

						try
						{
							concreteType = container.Resolve(newType).GetType();
						}
						catch
						{
						}

						if (_types.Contains(concreteType) == false)
						{
							_types.Add(concreteType);
						}
					}
					else
					{
						_types.Add(newType);
					}
				}
			}
		}

		public void AddTypes(ICollection<Type> newTypes)
		{
			foreach (Type type in newTypes)
			{
				AddType(type);
			}
		}

		#endregion
	}
}