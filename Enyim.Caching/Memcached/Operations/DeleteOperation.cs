using System;

namespace Enyim.Caching.Memcached.Operations
{
	internal sealed class DeleteOperation : ItemOperation
	{
		internal DeleteOperation(ServerPool pool, string key)
			: base(pool, key)
		{
		}

		protected override bool ExecuteAction()
		{
			PooledSocket socket = Socket;
			if (socket == null)
				return false;
			socket.SendCommand(string.Format("delete {0}", HashedKey));
			return String.Compare(socket.ReadResponse(), "DELETED", StringComparison.Ordinal) == 0;
		}
	}
}