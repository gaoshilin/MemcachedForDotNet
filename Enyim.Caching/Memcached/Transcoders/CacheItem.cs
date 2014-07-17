using System;

namespace Enyim.Caching.Memcached.Transcoders
{
    /// <summary>
    /// Represents an object either being retrieved from the cache
    /// or being sent to the cache.
    /// </summary>
    public struct CacheItem
    {
        private ArraySegment<byte> data;
        private ushort flags;

        /// <summary>
        /// Initializes a new instance of <see cref="T:CacheItem"/>.
        /// </summary>
        /// <param name="flags">Custom item data.</param>
        /// <param name="data">The serialized item.</param>
        public CacheItem(ushort flags, ArraySegment<byte> data)
        {
            this.data = data;
            this.flags = flags;
        }

        /// <summary>
        /// The data representing the item being stored/retireved.
        /// </summary>
        public ArraySegment<byte> Data
        {
            get { return data; }
            set { data = value; }
        }

        /// <summary>
        /// Flags set for this instance.
        /// </summary>
        public ushort Flag
        {
            get { return flags; }
            set { flags = value; }
        }
    }
}