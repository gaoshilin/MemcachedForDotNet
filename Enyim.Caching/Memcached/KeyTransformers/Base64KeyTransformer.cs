using System;
using System.Text;

namespace Enyim.Caching.Memcached.KeyTransformers
{
	/// <summary>
	/// A key transformer which converts the item keys into Base64.
	/// </summary>
	public sealed class Base64KeyTransformer : IMemcachedKeyTransformer
	{
		string IMemcachedKeyTransformer.Transform(string key)
		{
			return Convert.ToBase64String(Encoding.UTF8.GetBytes(key), Base64FormattingOptions.None);
		}
	}
}
 