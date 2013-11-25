using System;
using System.Threading;
using log4net;
using Monaco.Bus.Internals.Disposable;

namespace Monaco.Bus.Internals.Threading
{
	/// <summary>
	/// Represents a worker thread that will repeatedly execute a callback.
	/// </summary>
	public class WorkerThread : BaseDisposable
	{
		private static readonly object m_stop_requested_lock = new object();
		private readonly Thread m_thread;
		private bool m_isStopRequested;
		private Action m_methodToRunInLoop;

		/// <summary>
		/// Initializes a new WorkerThread for the specified method to run.
		/// </summary>
		/// <param name="methodToRunInLoop">The delegate method to execute in a loop.</param>
		public WorkerThread(Action methodToRunInLoop)
		{
			m_methodToRunInLoop = methodToRunInLoop;
			m_thread = new Thread(Loop);
			m_thread.SetApartmentState(ApartmentState.MTA);
			m_thread.Name = string.Format("Monaco.Worker.Thread.{0}", m_thread.ManagedThreadId);

			Name = m_thread.Name;

			m_thread.IsBackground = true;
		}

		public string Name { get; private set; }

		public bool StopRequested
		{
			get
			{
				return m_isStopRequested;
				//bool result;
				//lock (m_stop_requested_lock)
				//{
				//    result = m_isStopRequested;
				//}
				//return result;
			}
		}

		public event Action<WorkerThread> Stopped;

		public void Start()
		{
			if (Disposed) return;

			if (!m_thread.IsAlive)
				m_thread.Start();
		}

		public void Stop()
		{
			Dispose();
		}

		protected void Loop()
		{
			while (!StopRequested & m_methodToRunInLoop != null)
			{
				try
				{
					m_methodToRunInLoop();
				}
				catch
				{
				}
			}
		}

		public override void ReleaseManagedResources()
		{
			lock (m_stop_requested_lock)
			{
				m_isStopRequested = true;
				m_methodToRunInLoop = null;
			}

			WaitForThreadToDie(m_thread);
			OnStopRequested();
		}

		private static void WaitForThreadToDie(Thread thread)
		{
			thread.Join();
		}

		private void OnStopRequested()
		{
			Action<WorkerThread> evt = Stopped;

			if (evt != null)
				evt(this);
		}
	}

	public class WorkerThreadPool : BaseDisposable
	{
		private static readonly object m_lock = new object();
		private readonly ILog logger = LogManager.GetLogger(typeof (WorkerThreadPool));
		private readonly int m_thread_count;
		private Action m_method_to_run_in_loop;
		private bool m_running;
		private WorkerThread[] m_workerThreads = {};

		public WorkerThreadPool(int threadCount, Action methodToRunInLoop)
		{
			m_thread_count = threadCount;
			m_method_to_run_in_loop = methodToRunInLoop;
			m_workerThreads = new WorkerThread[threadCount];
		}

		public bool IsRunning
		{
			get
			{
				bool result;
				lock (m_lock)
				{
					result = m_running;
				}
				return m_running;
			}
		}

		public override void ReleaseManagedResources()
		{
			StopService();
			m_workerThreads = null;
			m_method_to_run_in_loop = null;
		}

		public void StartService()
		{
			if (m_running) return;

			for (int index = 0; index < m_thread_count; index++)
			{
				var t = new WorkerThread(m_method_to_run_in_loop);
				t.Stopped += ThreadStopped;
				t.Start();

				logger.Debug(string.Format("Thread '{0}' started.", t.Name));

				m_workerThreads[index] = t;
			}

			m_running = true;
		}

		public void StopService()
		{
			foreach (WorkerThread thread in m_workerThreads)
			{
				thread.Dispose();
				thread.Stopped -= ThreadStopped;
			}
			m_running = false;
		}

		private void ThreadStopped(WorkerThread workerThread)
		{
			logger.Debug(string.Format("Thread '{0}' stopped.", workerThread.Name));
		}
	}
}