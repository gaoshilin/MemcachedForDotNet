using System;

namespace Enyim.Caching.Memcached.Operations
{
	internal abstract class Operation : IDisposable
	{
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(Operation));

		private bool isDisposed;
		private bool success;
		private readonly ServerPool serverPool;

		protected Operation(ServerPool serverPool)
		{
			this.serverPool = serverPool;
		}

		public void Execute()
		{
			success = false;

			try
			{
				if (CheckDisposed(false))
					return;

				success = ExecuteAction();
			}
			catch (NotSupportedException)
			{
				throw;
			}
			catch (Exception e)
			{
				// TODO generic catch-all does not seem to be a good idea now. Some errors (like command not supported by server) should be exposed while retaining the fire-and-forget behavior
				Log.Error(e);
			}
		}

		protected ServerPool ServerPool
		{
			get { return serverPool; }
		}

		protected abstract bool ExecuteAction();

		protected bool CheckDisposed(bool throwOnError)
		{
			if (throwOnError && isDisposed)
				throw new ObjectDisposedException("Operation");

			return isDisposed;
		}

		public bool Success
		{
			get { return success; }
		}

		#region [ IDisposable                  ]
		public virtual void Dispose()
		{
			isDisposed = true;
		}

		void IDisposable.Dispose()
		{
			Dispose();
		}
		#endregion
	}
} 