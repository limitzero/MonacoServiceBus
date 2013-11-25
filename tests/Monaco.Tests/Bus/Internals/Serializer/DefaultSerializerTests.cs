using System.IO;
using System.Xml.Linq;

namespace Monaco.Tests.Bus.Internals.Serializer
{
	// crazy attempt to write my own xml serializer...
	public class DefaultSerializerTests
	{
		
	}

	public class DefaultSerializer
	{
		public string Serialize(object[] messages)
		{
			XDocument document = new XDocument();

			foreach (var message in messages)
			{
				SerializeInternal(document, message);
			}

			return null;
		}

		public object[] Deserialize(string contents)
		{
			return null;
		}

		public object[] Deserialize(Stream stream)
		{
			return null;
		}

		private void SerializeInternal(XDocument document, object message)
		{
			//document.Element(new XName{
		}


		private XElement GetXElement(object ele)
		{
			return null;
		}


	}
}