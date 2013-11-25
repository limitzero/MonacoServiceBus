using System;
using System.Collections.Generic;
using System.IO;
using Monaco.Bus;
using Monaco.Bus.Internals.Disposable;
using Monaco.Bus.MessageManagement.Serialization;

namespace Monaco.Tests.Sandbox.Pipeline
{
	public enum PipelineDirection
	{
		/// <summary>
		/// Process mesasge from message store to service bus for consumption.
		/// </summary>
		Inbound,

		/// <summary>
		/// Process message from service bus to message store for persistance.
		/// </summary>
		Outbound
	}

	public interface IPipeline
	{
		event EventHandler<PipelineStartedEventArgs> OnPipelineStarted;
		event EventHandler<PipelineCompletedEventArgs> OnPipelineCompleted;
		event EventHandler<PipelineErrorEventArgs> OnPipelineError;
		event EventHandler<PipelineAbortedEventArgs> OnPipelineAborted;
		ICollection<IPipelineFilter> InboundPipeline { get; set; }
		ICollection<IPipelineFilter> OutboundPipeline { get; set; }

		bool Name { get; set; }
		bool IsHalted { get; set; }

		void ConfigurePipeline(PipelineDirection pipelineDirection, params IPipelineFilter[] filters);
		void ConfigurePipeline(PipelineDirection pipelineDirection, params Type[] filters);

		IEnvelope Execute(PipelineDirection directions, IEnvelope envelope);
	}

	public class PipelineStartedEventArgs : EventArgs
	{
		public PipelineDirection PipelineDirection { get; set; }
		public IPipeline Pipeline { get; private set; }

		public PipelineStartedEventArgs(PipelineDirection pipelineDirection, IPipeline pipeline)
		{
			PipelineDirection = pipelineDirection;
			Pipeline = pipeline;
		}
	}

	public class PipelineCompletedEventArgs : EventArgs
	{
		public PipelineDirection PipelineDirection { get; set; }
		public IPipeline Pipeline { get; private set; }

		public PipelineCompletedEventArgs(PipelineDirection pipelineDirection, IPipeline pipeline)
		{
			PipelineDirection = pipelineDirection;
			Pipeline = pipeline;
		}
	}

	public class PipelineErrorEventArgs : EventArgs
	{
		public PipelineDirection PipelineDirection { get; private set; }
		public IPipeline Pipeline { get; private set; }
		public Exception Exception { get; private set; }

		public PipelineErrorEventArgs(PipelineDirection pipelineDirection, IPipeline pipeline, Exception exception)
		{
			PipelineDirection = pipelineDirection;
			Pipeline = pipeline;
			Exception = exception;
		}
	}

	public class PipelineAbortedEventArgs : EventArgs
	{
		public PipelineDirection PipelineDirection { get; private set; }
		public IPipeline Pipeline { get; private set; }
		public Exception Exception { get; private set; }

		public PipelineAbortedEventArgs(PipelineDirection pipelineDirection, IPipeline pipeline, Exception exception = null)
		{
			PipelineDirection = pipelineDirection;
			Pipeline = pipeline;
			Exception = exception;
		}
	}

	public interface IPipelineFilter
	{
		string Name { get; set; }
		bool HaltOnError { get; set; }
		event EventHandler<PipelineFilterStartedEventArgs> OnPipelineFilterStarted;
		event EventHandler<PipelineFilterCompletedEventArgs> OnPipelineFilterCompleted;
		event EventHandler<PipelineFilterErrorEventArgs> OnPipelineFilterError;
		IEnvelope Execute(IEnvelope envelope);
	}

	/// <summary>
	/// Base class for managing the transformation of the message from the 
	/// the transport layer to the service bus via custom filters that will perform 
	/// and action on the message. The pipeline is custom to the transport
	/// for properly constructing and deconstructing the message.
	/// </summary>
	public abstract class BasePipeline : IPipeline
	{
		public event EventHandler<PipelineStartedEventArgs> OnPipelineStarted;
		public event EventHandler<PipelineCompletedEventArgs> OnPipelineCompleted;
		public event EventHandler<PipelineErrorEventArgs> OnPipelineError;
		public event EventHandler<PipelineAbortedEventArgs> OnPipelineAborted;

		public ICollection<IPipelineFilter> InboundPipeline { get; set; }
		public ICollection<IPipelineFilter> OutboundPipeline { get; set; }

		public bool Name { get; set; }
		public bool IsHalted { get; set; }

		public void ConfigurePipeline(PipelineDirection pipelineDirection, params IPipelineFilter[] filters)
		{
			var pipelineFilters = new List<IPipelineFilter>(filters);

			switch (pipelineDirection)
			{
				case PipelineDirection.Inbound:
					this.InboundPipeline = pipelineFilters;
					break;
				case PipelineDirection.Outbound:
					this.OutboundPipeline = pipelineFilters;
					break;
			}
		}

		public void ConfigurePipeline(PipelineDirection directions, params Type[] filters)
		{
			throw new NotImplementedException();
		}

		public IEnvelope Execute(PipelineDirection pipelineDirection, IEnvelope envelope)
		{

			if (pipelineDirection == PipelineDirection.Inbound)
				envelope = this.ExecutePipeline(pipelineDirection, this.InboundPipeline, envelope);
			else
			{
				envelope = this.ExecutePipeline(pipelineDirection, this.OutboundPipeline, envelope);
			}

			return envelope;
		}

		private IEnvelope ExecutePipeline(PipelineDirection pipelineDirection, 
			IEnumerable<IPipelineFilter> filters, IEnvelope envelope)
		{
			foreach (var filter in filters)
			{
				try
				{
					OnStarted(pipelineDirection);
					envelope = filter.Execute(envelope);
				}
				catch (Exception exception)
				{
					if (filter.HaltOnError == true)
					{
						OnAborted(pipelineDirection, exception);
						break;
					}

					if (!OnError(pipelineDirection, exception))
						throw;
				}
				finally
				{
					OnCompleted(pipelineDirection);
				}
			}

			return envelope;
		}

		private void OnStarted(PipelineDirection pipelineDirection)
		{
			EventHandler<PipelineStartedEventArgs> evt = this.OnPipelineStarted;

			if (evt != null)
			{
				evt(this, new PipelineStartedEventArgs(pipelineDirection, this));
			}
		}

		private void OnCompleted(PipelineDirection pipelineDirection)
		{
			EventHandler<PipelineCompletedEventArgs> evt = this.OnPipelineCompleted;

			if (evt != null)
			{
				evt(this, new PipelineCompletedEventArgs(pipelineDirection, this));
			}
		}

		private void OnAborted(PipelineDirection pipelineDirection, Exception exception = null)
		{
			EventHandler<PipelineAbortedEventArgs> evt = this.OnPipelineAborted;

			if(evt != null)
			{
				evt(this, new PipelineAbortedEventArgs(pipelineDirection, this, exception));
			}
		}

		private bool OnError(PipelineDirection pipelineDirection, Exception exception)
		{
			EventHandler<PipelineErrorEventArgs> evt = this.OnPipelineError;
			bool isHandlerAttached = (evt != null);

			if (evt != null)
			{
				evt(this, new PipelineErrorEventArgs(pipelineDirection, this, exception));
			}

			return isHandlerAttached;
		}
	}


	/// <summary>
	/// Base class for implementing custom pipeline filters for processing the 
	/// message as in travels from the transport to the service bus for component 
	/// consumption and vice versa. This represents a single-step in the pipeline
	/// for moving messages from the transport to the bus and vice versa.
	/// </summary>
	public abstract class BasePipelineFilter : BaseDisposable, IPipelineFilter
	{
		public string Name { get; set; }

		public bool HaltOnError { get; set; }

		public event EventHandler<PipelineFilterStartedEventArgs> OnPipelineFilterStarted;
		public event EventHandler<PipelineFilterCompletedEventArgs> OnPipelineFilterCompleted;
		public event EventHandler<PipelineFilterErrorEventArgs> OnPipelineFilterError;

		public IEnvelope Execute(IEnvelope envelope)
		{
			IEnvelope retval = envelope;

			try
			{
				OnFilterStarted();
				retval = this.DoExecute(envelope);
			}
			catch (Exception exception)
			{
				if (!OnFilterError(exception))
					throw;
			}
			finally
			{
				OnFilterCompleted();
			}

			return retval;
		}

		/// <summary>
		/// This will implement the custom action to process the message.
		/// </summary>
		/// <param name="envelope"></param>
		/// <returns></returns>
		public abstract IEnvelope DoExecute(IEnvelope envelope);

		private void OnFilterStarted()
		{
			EventHandler<PipelineFilterStartedEventArgs> evt = this.OnPipelineFilterStarted;

			if (evt != null)
			{
				evt(this, new PipelineFilterStartedEventArgs(this));
			}
		}

		private void OnFilterCompleted()
		{
			EventHandler<PipelineFilterCompletedEventArgs> evt = this.OnPipelineFilterCompleted;

			if (evt != null)
			{
				evt(this, new PipelineFilterCompletedEventArgs(this));
			}
		}

		private bool OnFilterError(Exception exception)
		{
			EventHandler<PipelineFilterErrorEventArgs> evt = this.OnPipelineFilterError;
			bool isHandlerAttached = (evt != null);

			if (evt != null)
			{
				evt(this, new PipelineFilterErrorEventArgs(this, exception));
			}

			return isHandlerAttached;
		}
	}

	public class PipelineFilterStartedEventArgs : EventArgs
	{
		public IPipelineFilter Filter { get; private set; }

		public PipelineFilterStartedEventArgs(IPipelineFilter filter)
		{
			Filter = filter;
		}
	}

	public class PipelineFilterCompletedEventArgs : EventArgs
	{
		public IPipelineFilter Filter { get; private set; }

		public PipelineFilterCompletedEventArgs(IPipelineFilter filter)
		{
			Filter = filter;
		}
	}

	public class PipelineFilterErrorEventArgs : EventArgs
	{
		public IPipelineFilter Filter { get; private set; }
		public Exception Exception { get; private set; }

		public PipelineFilterErrorEventArgs(IPipelineFilter filter, Exception exception)
		{
			Filter = filter;
			Exception = exception;
		}
	}

	
	public interface ITransport
	{
		event Action<IEnvelope> OnTransportMessageReceived;

		IPipelineManager PipelineManager { get; }
		Stream Receive(TimeSpan? timeout);
		void Send(Stream stream);
	}

	public interface IPipelineManager
	{
		ICollection<IPipeline> Pipelines { get; set; }
		event EventHandler<PipelineManagerStartedEventArgs> OnPipelineManagerStarted;
		event EventHandler<PipelineManagerCompletedEventArgs> OnPipelineManagerCompleted;
		event EventHandler<PipelineManagerErrorEventArgs> OnPipelineManagerError;
		IEnvelope Execute(PipelineDirection pipelineDirection, Stream stream);
	}

	public class PipelineManagerStartedEventArgs : EventArgs
	{
		public PipelineDirection PipelineDirection { get; private set; }
		public IPipelineManager PipelineManager { get; private set; }

		public PipelineManagerStartedEventArgs(PipelineDirection pipelineDirection, 
			IPipelineManager pipelineManager)
		{
			PipelineDirection = pipelineDirection;
			PipelineManager = pipelineManager;
		}
	}

	public class PipelineManagerCompletedEventArgs : EventArgs
	{
		public PipelineDirection PipelineDirection { get; private set; }
		public IPipelineManager PipelineManager { get; private set; }

		public PipelineManagerCompletedEventArgs(PipelineDirection pipelineDirection,
			IPipelineManager pipelineManager)
		{
			PipelineDirection = pipelineDirection;
			PipelineManager = pipelineManager;
		}
	}

	public class PipelineManagerErrorEventArgs : EventArgs
	{
		public PipelineDirection PipelineDirection { get; private set; }
		public IPipelineManager PipelineManager { get; private set; }
		public Exception Exception { get; private set; }

		public PipelineManagerErrorEventArgs(PipelineDirection pipelineDirection,
			IPipelineManager pipelineManager, 
			Exception exception)
		{
			PipelineDirection = pipelineDirection;
			PipelineManager = pipelineManager;
			Exception = exception;
		}
	}


	public abstract class BasePipelineManager : IPipelineManager
	{
		public ICollection<IPipeline> Pipelines { get; set; }
		public event EventHandler<PipelineManagerStartedEventArgs> OnPipelineManagerStarted;
		public event EventHandler<PipelineManagerCompletedEventArgs> OnPipelineManagerCompleted;
		public event EventHandler<PipelineManagerErrorEventArgs> OnPipelineManagerError;

		public IEnvelope Execute(PipelineDirection pipelineDirection, Stream stream)
		{
			IEnvelope envelope = new Envelope(stream);

			OnManagerStarted(pipelineDirection); 

			foreach (var pipeline in Pipelines)
			{
				try
				{
					Connect(pipeline);
					pipeline.Execute(pipelineDirection, envelope);
				}
				catch
				{
					throw;
				}	
				finally
				{
					Disconnect(pipeline);
				}
			}

			OnManagerCompleted(pipelineDirection); 

			return envelope;
		}

		private void OnManagerStarted(PipelineDirection pipelineDirection)
		{
			EventHandler<PipelineManagerStartedEventArgs> evt = this.OnPipelineManagerStarted; 

			if(evt != null)
			{
				evt(this, new PipelineManagerStartedEventArgs(pipelineDirection, this));
			}
		}

		private void OnManagerCompleted(PipelineDirection pipelineDirection)
		{
			EventHandler<PipelineManagerCompletedEventArgs> evt = this.OnPipelineManagerCompleted;

			if (evt != null)
			{
				evt(this, new PipelineManagerCompletedEventArgs(pipelineDirection, this));
			}
		}

		private bool OnManagerError(PipelineDirection pipelineDirection, IPipeline pipeline, 
			Exception exception = null)
		{
			EventHandler<PipelineManagerErrorEventArgs> evt = this.OnPipelineManagerError;
			bool IsHandlerAttached = (evt != null);

			if(IsHandlerAttached)
			{
				evt(this, new PipelineManagerErrorEventArgs(pipelineDirection, this, exception));
			}

			return IsHandlerAttached;
		}

		private void Connect(IPipeline pipeline)
		{
			//pipeline.OnPipelineAborted += OnAborted;
			//pipeline.OnPipelineCompleted += OnCompleted;
			//pipeline.OnPipelineError += OnError;
		}


		private void Disconnect(IPipeline pipeline)
		{
			//pipeline.OnPipelineAborted -= OnAborted;
			//pipeline.OnPipelineCompleted -= OnCompleted;
			//pipeline.OnPipelineError -= OnError;
		}

		//private void OnError(object sender, PipelineErrorEventArgs e)
		//{
			
		//}

		//private void OnCompleted(object sender, PipelineCompletedEventArgs e)
		//{

		//}

		//private void OnAborted(object sender, PipelineAbortedEventArgs e)
		//{

		//}

	}


	public class DeserializationFilter : BasePipelineFilter
	{
		private readonly ISerializationProvider _serializationProvider;

		public DeserializationFilter(ISerializationProvider serializationProvider)
		{
			_serializationProvider = serializationProvider;
			this.HaltOnError = true;
			this.Name = this.GetType().Name;
		}

		public override IEnvelope DoExecute(IEnvelope envelope)
		{
			var message = _serializationProvider.Deserialize(envelope.Body.PayloadStream);
			envelope.Body.Payload = new object[] {message};
			return envelope;
		}
	}

	public class SerializationFilter : BasePipelineFilter
	{
		private readonly ISerializationProvider _serializationProvider;

		public SerializationFilter(ISerializationProvider serializationProvider)
		{
			_serializationProvider = serializationProvider;
			this.HaltOnError = true;
			this.Name = this.GetType().Name;
		}

		public override IEnvelope DoExecute(IEnvelope envelope)
		{
			var stream = _serializationProvider.SerializeToBytes(envelope.Body.Payload);
			envelope.Body.PayloadStream = stream;
			return envelope;
		}
	}
}