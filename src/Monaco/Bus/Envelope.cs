using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Monaco.Extensions;

namespace Monaco.Bus
{
	public class Envelope : IEnvelope
	{
		public EnvelopeHeader Header { get; set; }
		public EnvelopeBody Body { get; set; }
		public EnvelopeFooter Footer { get; set; }

		public Envelope()
		{
			Header = new EnvelopeHeader();
			Body = new EnvelopeBody(Header);
			Footer = new EnvelopeFooter();
		}

		public Envelope(params object[] payload)
			: this()
		{
			Body.Payload = payload;
		}		

		public IEnvelope Clone()
		{
			var envelope = new Envelope();
			envelope.Header = this.Header;
			envelope.Body = this.Body;
			envelope.Footer = this.Footer;
			return envelope;
		}

		public IEnvelope Clone(object message)
		{
			var envelope = this.Clone();
			envelope.Body.Payload = new object[] {message};
			return envelope;
		}
	}

	[XmlRoot(ElementName = "Header")]
	public class EnvelopeHeader : IMessage
	{
		public EnvelopeHeader()
		{
			Stages = new List<string>();
		}

		public ICollection<string> Stages { get; set; }

		/// <summary>
		/// Gets or sets the endpoint currently processing the message
		/// </summary>
		public string LocalEndpoint { get; set; }

		/// <summary>
		/// Gets or sets the endpoint that has issued a "Send" request
		/// for a corresponding "Reply" on the same or remote bus instance.
		/// </summary>
		public string ReplyEndpoint { get; set; }

		/// <summary>
		/// Gets or sets the endpoint that will process the "Send" request
		/// and issue a "Reply" back to the sender endpoint.
		/// </summary>
		public string RemoteEndpoint { get; set; }

		/// <summary>
		/// Gets or sets the message identifier from the underlying persistance store.
		/// </summary>
		public string MessageId { get; set; }

		public object CorrelationId { get; set; }

		public void RecordStage(object component, params object[] message)
		{
			RecordStage(component, message, string.Empty);
		}

		public void RecordStage(object component, IEnumerable<object> messages, string action)
		{
			string msg = string.Format("Time:{0} - Component:{1} - Message(s):{2} - Action:{3}",
			                           DateTime.UtcNow,
			                           component.GetType().FullName,
			                           messages.ToItemList(),
			                           string.IsNullOrEmpty(action) ? "N/A" : action);
			Stages.Add(msg);
		}
	}

	[XmlRoot(ElementName = "Body")]
	public class EnvelopeBody : IMessage
	{
		private readonly EnvelopeHeader header;
		private List<object> payload;
		private byte[] stream;

		public EnvelopeBody()
		{
			this.payload = new List<object>();
			this.stream = new byte[] {};
		}

		public EnvelopeBody(EnvelopeHeader header)
		{
			this.header = header;
		}

		private string label;
		public string Label
		{
			get { return label= this.Payload.ToItemList(); }
			private set { label = value; }
		}

		public IEnumerable<object> Payload
		{
			get { return payload; }
			set
			{
				if (value is Envelope)
				{
					value = ((Envelope) value).Body.Payload;
				}

				payload = new List<object>(value);

				if (value != null)
				{
					CreateLabel(value);
					CreateCorrelation(value);
				}
			}
		}

		[XmlIgnore]
		public byte[] PayloadStream { get; set; }

		public void BuildPayload<TMessage>(Action<TMessage> action)
			where TMessage : class, new()
		{
			var message = new TMessage();
			action(message);
			Payload = new object[] {message};
		}

		private void CreateCorrelation(object message)
		{
			if (header == null) return;

			if (typeof (CorrelatedBy<>).IsAssignableFrom(message.GetType()))
			{
				try
				{
					object retval = message.GetType().GetProperty("CorrelationId").GetValue(message, null);

					if (retval != null)
					{
						header.CorrelationId = retval.ToString();
					}
				}
				catch
				{
				}
			}
		}

		private void CreateLabel(params object[] messages)
		{
			var label = messages.ToItemList();
			Label = label.Replace("+", ".");
		}

		public void SetStream(byte[] bytes)
		{
			stream = bytes;
		}

		public byte[] GetStream()
		{
			return stream;
		}

		public T GetPayload<T>()
		{
			T result = default(T);
			try
			{
				result = (T) this.Payload;
			}
			catch
			{
			}
			return result;
		}
	}

	[XmlRoot(ElementName = "Payload")]
	public class EnvelopePayload
	{
		public List<object> Messages { get; set; }

		public EnvelopePayload()
			: this(null)
		{
			this.Messages = new List<object>();
		}

		public EnvelopePayload(params object[] messages)
		{
			this.Messages.AddRange(messages);
		}
	}

	[XmlRoot(ElementName = "Footer")]
	public class EnvelopeFooter : IMessage
	{
		public EnvelopeFooter()
		{
			this.exceptions = new List<string>();
		}

		private List<string> exceptions;
		public IEnumerable<string> Exceptions
		{
			get { return exceptions; }
			set { exceptions = new List<string>(value); }
		}

		public void RecordException(Exception exception)
		{
			if (exception == null) return;

			RecordException(exception.ToString());
		}

		public void RecordException(string exception)
		{
			if (string.IsNullOrEmpty(exception)) return;

			string msg = string.Format("{0}: {1} - {2}",
			                           DateTime.UtcNow,
			                           Exceptions.Count() + 1, exception);
			this.exceptions.Add(msg);
		}
	}
}