using System;
using System.Globalization;

namespace Enyim.Caching.Memcached.Operations
{
	internal sealed class IncrementOperation : ItemOperation
	{
		private readonly uint amount;
		private uint result;

		internal IncrementOperation(ServerPool pool, string key, uint amount)
			: base(pool, key)
		{
			this.amount = amount;
		}

		protected override bool ExecuteAction()
		{
			PooledSocket socket = Socket;
			if (socket == null)
				return false;

			socket.SendCommand(String.Concat("incr ", HashedKey, " ", amount.ToString(CultureInfo.InvariantCulture)));

			string response = socket.ReadResponse();

			//maybe we should throw an exception when the item is not found?
			if (String.Compare(response, "NOT_FOUND", StringComparison.Ordinal) == 0)
				return false;

			return UInt32.TryParse(response, out result);
		}

		public uint Result
		{
			get { return result; }
		}
	}
}
 