using System;
using System.Collections.Generic;
using System.Text;

namespace Monaco.Distributor.Internals.Fabric.Impl
{
	public class FabricWorker : IDisposable, IRecyclable
	{
		private bool _disposed;

		/// <summary>
		/// Event sent by the worker pool manager to clear all stored session information for request selection.
		/// </summary>
		//public event Action OnClearSession;

		/// <summary>
		/// Gets or sets the endpoint that the worker represents.
		/// </summary>
		public Uri Endpoint { get; set; }

		/// <summary>
		/// Gets or sets the weight for this worker relative to the workers in the pool.
		/// </summary>
		public double Weight { get; set; }

		/// <summary>
		/// Gets or sets the number of requests that this worker can handle.
		/// </summary>
		public int Requests { get; set; }

		/// <summary>
		/// Gets or sets the number of handled requests for this worker.
		/// </summary>
		public int HandledRequests { get; set; }

		/// <summary>
		/// Gets or sets the number of times this worker was selected.
		/// </summary>
		public int SelectionTotal { get; set; }

		/// <summary>
		/// Gets the current session history of selection of this fabric worker.
		/// </summary>
		public List<FabricWorker> Sessions { get; private set; }

		/// <summary>
		/// Endpoint for the load balanced reference for the worker:
		/// </summary>
		public Uri LoadBalancedEndpoint { get; set; }
		
		public FabricWorker()
		{
			this.Sessions = new List<FabricWorker>();
		}

		~FabricWorker()
		{
			Disposing(true);
		}

		public void Recycle()
		{
			this.SelectionTotal = 0;
			this.Sessions.Clear();
		}

		public void Initialize(int workers, Random optimizer = null)
		{
			var localOptimizer = optimizer ?? new Random(workers);

			if (this.Weight == 0 && this.Requests == 0)
			{
				var choice = (double)localOptimizer.Next(1, workers);
				this.Weight = choice / ((double)workers) * 100;
				this.Requests =(int) (this.Weight*10);
			}
		}

		public void AppendToSession()
		{
			this.GuardForDispose();
			this.SelectionTotal++;
			this.Sessions.Add(this);
		}

		public int GetNumberOfRequestsForWeighting(int currentMessageCount)
		{
			var range = (int) this.Weight - this.Requests;
			var percentage = this.Requests/this.Weight;
			var handledRequests = (int) (range*percentage);
			return handledRequests;
		}

		public void Dispose()
		{
			Disposing(true);
			GC.SuppressFinalize(this);
		}

		private void GuardForDispose()
		{
			if (this._disposed == true)
				throw new ObjectDisposedException("Can not access a disposed fabric worker.");
		}

		private void Disposing(bool disposing)
		{
			if (disposing == true)
			{
				if (this.Sessions != null)
				{
					this.Sessions.Clear();
				}
				this.Sessions = null;
			}

			this._disposed = true;
		}

		private static void DisplayWorkerInformation(StringBuilder sb, FabricWorker worker, int tabCount = 0)
		{
			string tabs = string.Empty;

			for (int index = 1; index < tabCount; index++)
				tabs += "\t";

			sb.Append(tabs).AppendFormat("Worker [{0}]", worker.Endpoint.OriginalString).AppendLine()
				.Append(tabs).AppendFormat("Weight : {0}", worker.Weight).AppendLine()
				.Append(tabs).AppendFormat("Requests : {0}", worker.Requests).AppendLine()
				.Append(tabs).AppendFormat("Handled Requests : {0}", worker.HandledRequests).AppendLine()
				.Append(tabs).AppendFormat("Selection Count : {0}", worker.SelectionTotal).AppendLine();
		}

		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();
			DisplayWorkerInformation(builder, this);

			// TODO: this is really not needed as the full history should be logged elsewhere
			//if (this.Sessions.Count > 0)
			//{
			//    builder.Append("\t\t");
			//    builder.AppendLine("Session History:");

			//    this.Sessions.ForEach(session =>
			//                            {
			//                                DisplayWorkerInformation(builder, session, 3);
			//                            });
			//}

			return builder.ToString();
		}
	}
}