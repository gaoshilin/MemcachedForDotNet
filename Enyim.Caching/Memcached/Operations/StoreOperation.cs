using System;
using System.Globalization;
using System.Text;
using Enyim.Caching.Memcached.Transcoders;

namespace Enyim.Caching.Memcached.Operations
{
    internal class StoreOperation : ItemOperation
    {
        private const int MaxSeconds = 60 * 60 * 24 * 30;

        private static readonly ArraySegment<byte> DataTerminator = new ArraySegment<byte>(new[] { (byte)'\r', (byte)'\n' });
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1);

        private readonly StoreCommand mode;
        private readonly object value;

        private readonly long expires;
        private readonly ulong casValue;

        internal StoreOperation(ServerPool pool, StoreCommand mode, string key, object value, ulong casValue, TimeSpan validFor, DateTime expiresAt)
            : base(pool, key)
        {
            this.mode = mode;
            this.value = value;
            this.casValue = casValue;

            expires = GetExpiration(validFor, expiresAt);
        }

        private static long GetExpiration(TimeSpan validFor, DateTime expiresAt)
        {
            if (validFor >= TimeSpan.Zero && expiresAt > DateTime.MinValue)
                throw new ArgumentException("You cannot specify both validFor and expiresAt.");

            if (expiresAt > DateTime.MinValue)
            {
                if (expiresAt < UnixEpoch)
                    throw new ArgumentOutOfRangeException("expiresAt", "expiresAt must be >= 1970/1/1");

                return (long)(expiresAt.ToUniversalTime() - UnixEpoch).TotalSeconds;
            }

            if (validFor.TotalSeconds >= MaxSeconds || validFor < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException("validFor", "validFor must be < 30 days && >= 0");

            return (long)validFor.TotalSeconds;
        }

        protected override bool ExecuteAction()
        {
            if (Socket == null)
                return false;

            CacheItem item = ServerPool.Transcoder.Serialize(value);

            return Store(item.Flag, item.Data);
        }

        private bool Store(ushort flag, ArraySegment<byte> data)
        {
            StringBuilder sb = new StringBuilder(100);

            switch (mode)
            {
                case StoreCommand.Add:
                    sb.Append("add ");
                    break;
                case StoreCommand.Replace:
                    sb.Append("replace ");
                    break;
                case StoreCommand.Set:
                    sb.Append("set ");
                    break;

                case StoreCommand.Append:
                    sb.Append("append ");
                    break;

                case StoreCommand.Prepend:
                    sb.Append("prepend ");
                    break;

                case StoreCommand.CheckAndSet:
                    sb.Append("cas ");
                    break;

                default:
                    throw new MemcachedClientException(mode + " is not supported.");
            }

            sb.Append(HashedKey);
            sb.Append(" ");
            sb.Append(flag.ToString(CultureInfo.InvariantCulture));
            sb.Append(" ");
            sb.Append(expires.ToString(CultureInfo.InvariantCulture));
            sb.Append(" ");
            sb.Append(Convert.ToString(data.Count - data.Offset, CultureInfo.InvariantCulture));

            if (mode == StoreCommand.CheckAndSet)
            {
                sb.Append(" ");
                sb.Append(Convert.ToString(casValue, CultureInfo.InvariantCulture));
            }

            ArraySegment<byte> commandBuffer = PooledSocket.GetCommandBuffer(sb.ToString());

            Socket.Write(new[] { commandBuffer, data, DataTerminator });

            return String.Compare(Socket.ReadResponse(), "STORED", StringComparison.Ordinal) == 0;
        }
    }
}
