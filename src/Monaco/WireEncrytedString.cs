using System;
using System.Xml.Serialization;

namespace Monaco
{
	[Serializable]
	public class WireEncryptedString
	{
		[XmlIgnore]
		public string Value { get; set; }

		public string EncrytedValue { get; set; }
	}
}