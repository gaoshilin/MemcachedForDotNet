using System;

namespace Enyim.Caching.Memcached.Transcoders
{
	/// <summary>
	/// Provides an interface for serializing items for Memcached.
	/// </summary>
	public interface ITranscoder
	{
		/// <summary>
		/// Serializes an object for storing in the cache.
		/// </summary>
		/// <param name="o">The object to serialize</param>
		/// <returns>The serialized object</returns>
		CacheItem Serialize(Object o);

		/// <summary>
		/// Deserializes the <see cref="T:CacheItem"/> into an object.
		/// </summary>
		/// <param name="item">The stream that contains the data to deserialize.</param>
		/// <returns>The deserialized object</returns>
		object Deserialize(CacheItem item);
	}
}
 