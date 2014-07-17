using System.Collections.Generic;

namespace Enyim.Caching.Memcached
{
	/// <summary>
	/// Defines a locator class whihc maps item keys to memcached servers.
	/// </summary>
	public interface IMemcachedNodeLocator
	{
		/// <summary>
		/// Initializes the locator.
		/// </summary>
		/// <param name="nodes">The memcached nodes defined in the configuration.</param>
		void Initialize(IList<MemcachedNode> nodes);
		/// <summary>
		/// Returns the memcached node the specified key belongs to.
		/// </summary>
		/// <param name="key">The key of the item to be located.</param>
		/// <returns>The <see cref="T:MemcachedNode"/> the specifed item belongs to</returns>
		MemcachedNode Locate(string key);
	}
} 