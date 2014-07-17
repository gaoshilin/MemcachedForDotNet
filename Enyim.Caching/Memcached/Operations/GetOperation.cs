namespace Enyim.Caching.Memcached.Operations
{
	internal class GetOperation : ItemOperation
	{
		private object result;

		internal GetOperation(ServerPool pool, string key)
			: base(pool, key)
		{
		}

		public object Result
		{
			get { return result; }
		}

		protected override bool ExecuteAction()
		{
			PooledSocket socket = Socket;

			if (socket == null)
				return false;

			socket.SendCommand("get " + HashedKey);

			GetResponse r = GetHelper.ReadItem(Socket);

			if (r != null)
			{
				result = ServerPool.Transcoder.Deserialize(r.Item);
				GetHelper.FinishCurrent(Socket);
			}

			return true;
		}
	}
}
 