using System;
using System.Diagnostics;
using System.Threading;
using System.Timers;
using System.Transactions;
using Monaco.Bus.Internals.Eventing;
using Monaco.Bus.Internals.Threading;
using Timer = System.Timers.Timer;

namespace Monaco.Bus.Internals.Agent
{
	/// <summary>
	/// Abstract class for all agents that must run in the background.
	/// </summary>
	public abstract class BaseAgent : IAgent
	{
		private volatile bool _disposed;
		private bool m_disposed;
		private WorkerThreadPool m_pool;
		private Timer m_schedule;
		private static object _executeLock = new object();

		public bool Disposed
		{
			get { return _disposed; }
			private set { _disposed = value; }
		}

		/// <summary>
		/// (Read-Write). The name of the agent instance.
		/// </summary>
		public string Name { get; set; }

		#region IAgent Members

		public event EventHandler<ComponentStartedEventArgs> ComponentStartedEvent;
		public event EventHandler<ComponentStoppedEventArgs> ComponentStoppedEvent;

		/// <summary>
		/// (Read-Write). The number of concurrent threads used for processing.
		/// </summary>
		public int Concurrency { get; set; }

		/// <summary>
		/// (Read-Write). The interval, in seconds, that each thread should wait before polling the location for messages.
		/// </summary>
		public int Frequency { get; set; }

		/// <summary>
		/// (Read-Write). The interval or "schedule", in seconds, that the location will be polled looking for new messages.
		/// </summary>
		public int Interval { get; set; }

		/// <summary>
		/// (Read-Only). Flag to indicate whether the component is started or not.
		/// </summary>
		public virtual bool IsRunning { get; private set; }

		public virtual bool IsPaused { get; private set; }

		public virtual void Start()
		{
			if (IsRunning)
				return;

			if (m_disposed)
				return;

			int threads = Concurrency > 0 ? Concurrency : 1;
			Frequency = Frequency >= 0 ? Frequency*1000 : 0;

			if (Interval > 0)
			{
				m_schedule = new Timer(Interval*1000);
				m_schedule.Elapsed += ScheduleElasped;
				m_schedule.Start();
			}
			else
			{
				m_pool = new WorkerThreadPool(threads, () => InvokeExecute());
				m_pool.StartService();
			}

			IsRunning = true;
			IsPaused = false;
			OnComponentStarted();
		}

		public virtual void Pause()
		{
			IsPaused = true;
		}

		public virtual void Resume()
		{
			IsPaused = false;
		}

		public virtual void Stop()
		{
			IsRunning = false;
			Dispose();
			OnComponentStopped();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

		public event EventHandler<ComponentErrorEventArgs> ComponentErrorEvent;

		public virtual void OnDisposing()
		{
		}

		private void Dispose(bool disposing)
		{
			if (!Disposed)
			{
				if (disposing)
				{
					// clean up internal resources:
					if (m_pool != null)
					{
						//m_pool.StopService();
						m_pool.Dispose();
					}
					m_pool = null;

					if (m_schedule != null)
					{
						m_schedule.Stop();
						m_schedule.Elapsed -= ScheduleElasped;
					}
					m_schedule = null;

					// let custom implementations clean up resources (if needed):
					OnDisposing();
				}

				m_disposed = true;
			}
		}

		/// <summary>
		/// This will be invoked in a periodic fashion for the 
		/// custom service code to perform some actions specific 
		/// to their design.
		/// </summary>
		public abstract void Execute();

		public bool OnServiceError(string message, Exception exception)
		{
			EventHandler<ComponentErrorEventArgs> evt = ComponentErrorEvent;
			bool isHandlerAttached = (evt != null);

			if (isHandlerAttached)
				evt(this, new ComponentErrorEventArgs(exception));

			return isHandlerAttached;
		}

		private void InvokeExecute()
		{
			try
			{
				if (Disposed) return;

				if (Frequency > 0)
				{
					Thread.Sleep(TimeSpan.FromMilliseconds(Frequency));
				}

				if (IsPaused)
				{
					Thread.Sleep(100);
					return;
				}

				if(this.Concurrency > 1)
				{
					lock(_executeLock)
					{
						Execute();
					}
				}
				else
				{
					Execute();
				}

			}
			catch (ThreadAbortException tex)
			{
				Debug.Assert(false, "Thread abort exception: " + tex);
			}
			catch (TransactionAbortedException tex)
			{
				if (!OnServiceError(tex.Message, tex))
					throw;
			}
			catch (Exception exception)
			{
				if (!OnServiceError(exception.Message, exception))
					throw;
			}
		}

		private void ScheduleElasped(object sender, ElapsedEventArgs e)
		{
			InvokeExecute();
		}

		private void OnComponentStarted()
		{
			EventHandler<ComponentStartedEventArgs> evt = ComponentStartedEvent;
			if (evt != null)
			{
				evt(this, new ComponentStartedEventArgs(Name));
			}
		}

		private void OnComponentStopped()
		{
			EventHandler<ComponentStoppedEventArgs> evt = ComponentStoppedEvent;
			if (evt != null)
			{
				evt(this, new ComponentStoppedEventArgs(Name));
			}
		}

		private bool OnComponentError(Exception exception)
		{
			EventHandler<ComponentErrorEventArgs> evt = ComponentErrorEvent;
			bool isHandlerAttached = (evt != null);

			if (isHandlerAttached)
			{
				evt(this, new ComponentErrorEventArgs(exception));
			}

			return isHandlerAttached;
		}
	}
}