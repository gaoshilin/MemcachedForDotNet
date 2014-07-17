namespace Enyim.Caching.Memcached.Operations
{
	internal sealed class FlushOperation : Operation
	{
		public FlushOperation(ServerPool pool) : base(pool) { }

		protected override bool ExecuteAction()
		{
			foreach (MemcachedNode server in ServerPool.WorkingServers)
			{
				using (PooledSocket ps = server.Acquire())
				{
					if (ps != null)
						ps.SendCommand("flush_all");
				}
			}

			return true;
		}
	}
}
 