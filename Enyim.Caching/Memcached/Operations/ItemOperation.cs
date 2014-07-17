using System;

namespace Enyim.Caching.Memcached.Operations
{
	internal abstract class ItemOperation : Operation
	{
		private readonly string key;
		private string hashedKey;

		private PooledSocket socket;

		protected ItemOperation(ServerPool pool, string key)
			: base(pool)
		{
			this.key = key;
		}

		protected string Key
		{
			get { return key; }
		}

		/// <summary>
		/// Gets the hashed bersion of the key which should be used as key in communication with memcached
		/// </summary>
		protected string HashedKey
		{
			get { return hashedKey ?? (hashedKey = ServerPool.KeyTransformer.Transform(key)); }
		}

		protected PooledSocket Socket
		{
			get
			{
				if (socket == null)
				{
					// get a connection to the server which belongs to "key"
					PooledSocket ps = ServerPool.Acquire(key);

					// null was returned, so our server is dead and no one could replace it
					// (probably all of our servers are down)
					if (ps == null)
					{
						return null;
					}

					socket = ps;
				}

				return socket;
			}
		}

		public override void Dispose()
		{
			GC.SuppressFinalize(this);

			if (socket != null)
			{
				((IDisposable)socket).Dispose();
				socket = null;
			}

			base.Dispose();
		}
	}
}
 