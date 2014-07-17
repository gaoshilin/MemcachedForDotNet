using System;
using System.Collections.Generic;
using System.Net;

namespace Enyim.Caching.Configuration
{
	/// <summary>
	/// Defines an interface for configuring the <see cref="T:MemcachedClient"/>.
	/// </summary>
	public interface IMemcachedClientConfiguration
	{
		/// <summary>
		/// Gets a list of <see cref="T:IPEndPoint"/> each representing a Memcached server in the pool.
		/// </summary>
		IList<IPEndPoint> Servers
		{
			get;
		}

		/// <summary>
		/// Gets the configuration of the socket pool.
		/// </summary>
		ISocketPoolConfiguration SocketPool
		{
			get;
		}

		/// <summary>
		/// Gets or sets the type of the <see cref="T:Enyim.Caching.Memcached.IMemcachedKeyTransformer"/> which will be used to convert item keys for Memcached.
		/// </summary>
		Type KeyTransformer
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the type of the <see cref="T:Enyim.Caching.Memcached.IMemcachedNodeLocator"/> which will be used to assign items to Memcached nodes.
		/// </summary>
		Type NodeLocator
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the type of the <see cref="T:Enyim.Caching.Memcached.ITranscoder"/> which will be used serialzie or deserialize items.
		/// </summary>
		Type Transcoder
		{
			get;
			set;
		}
	}
}
