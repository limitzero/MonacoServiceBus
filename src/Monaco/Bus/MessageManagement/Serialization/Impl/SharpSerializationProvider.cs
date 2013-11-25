using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Polenter.Serialization;

namespace Monaco.Bus.MessageManagement.Serialization.Impl
{
	public class SharpSerializationProvider : ISerializationProvider
	{
		private SharpSerializer _serializer;
		private SharpSerializerXmlSettings _settings;

		public SharpSerializationProvider()
		{
			_settings = new SharpSerializerXmlSettings();
			_settings.IncludeAssemblyVersionInTypeName = true;
			_settings.IncludeCultureInTypeName = true;
			_settings.IncludePublicKeyTokenInTypeName = true;
			_settings.Encoding = Encoding.ASCII;

			_settings.AdvancedSettings.RootName = "message";
			_settings.AdvancedSettings.PropertiesToIgnore.Add(typeof (IServiceBus), "Bus");

			// custom support for creating proxies for interface-based messages (add-on):
			_settings.AdvancedSettings.DeserializerTypeResolver = new SharpSerializerInterfaceProxyDeserializerSupport();
			_serializer = new SharpSerializer(_settings);
		}

		#region ISerializationProvider Members

		public void Dispose()
		{
			if (_serializer != null)
			{
				_serializer = null;
			}

			_settings = null;
		}

		public void Initialize(IEnumerable<Type> types)
		{
		}

		public object Deserialize(string instance)
		{
			byte[] bytes = Encoding.ASCII.GetBytes(instance);
			var stream = new MemoryStream(bytes);
			stream.Seek(0, SeekOrigin.Begin);
			return Deserialize(stream);
		}

		public object Deserialize(Stream stream)
		{
			return _serializer.Deserialize(stream);
		}

		public object Deserialize(byte[] bytes)
		{
			var stream = new MemoryStream(bytes);
			stream.Seek(0, SeekOrigin.Begin);
			return Deserialize(stream);
		}

		public TMessage Deserialize<TMessage>(byte[] message) where TMessage : class
		{
			return Deserialize(message) as TMessage;
		}

		public TMessage Deserialize<TMessage>(Stream message) where TMessage : class
		{
			return Deserialize(message) as TMessage;
		}

		public TMessage Deserialize<TMessage>(string message) where TMessage : class
		{
			byte[] bytes = Encoding.ASCII.GetBytes(message);
			var stream = new MemoryStream(bytes);
			stream.Seek(0, SeekOrigin.Begin);

			return Deserialize(stream) as TMessage;
		}

		public string Serialize(object message)
		{
			string contents = string.Empty;

			var stream = new MemoryStream();
			_serializer.Serialize(message, stream);
			stream.Seek(0, SeekOrigin.Begin);

			using (TextReader reader = new StreamReader(stream))
			{
				contents = reader.ReadToEnd();
			}

			return contents;
		}

		public byte[] SerializeToBytes(object message)
		{
			var stream = new MemoryStream();
			_serializer.Serialize(message, stream);
			stream.Seek(0, SeekOrigin.Begin);
			return stream.ToArray();
		}

		public Stream SerializeToStream(object message)
		{
			var stream = new MemoryStream();
			_serializer.Serialize(message, stream);
			stream.Seek(0, SeekOrigin.Begin);
			return stream;
		}

		public void AddType(Type newType)
		{
		}

		public void AddTypes(ICollection<Type> newTypes)
		{
		}

		public void Initialize()
		{
		}

		#endregion
	}
}