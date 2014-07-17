using System;
using System.Security.Cryptography;
using System.Text;

namespace Enyim.Caching.Memcached.KeyTransformers
{
	/// <summary>
	/// A key transformer which converts the item keys into their SHA1 hash.
	/// </summary>
	public sealed class SHA1KeyTransformer : IMemcachedKeyTransformer
	{
		string IMemcachedKeyTransformer.Transform(string key)
		{
			SHA1Managed sh = new SHA1Managed();
			byte[] data = sh.ComputeHash(Encoding.Unicode.GetBytes(key));

			return Convert.ToBase64String(data, Base64FormattingOptions.None);
		}
	}
} 