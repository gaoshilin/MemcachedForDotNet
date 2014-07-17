using System;
using System.Configuration;
using System.ComponentModel;

namespace Enyim.Caching.Configuration
{
	/// <summary>
	/// Configures the socket pool settings for Memcached servers.
	/// </summary>
	public sealed class SocketPoolElement : ConfigurationElement, ISocketPoolConfiguration
	{
		/// <summary>
		/// Gets or sets a value indicating the minimum amount of sockets per server in the socket pool.
		/// </summary>
		/// <returns>The minimum amount of sockets per server in the socket pool.</returns>
		[ConfigurationProperty("minPoolSize", IsRequired = false, DefaultValue = 10), IntegerValidator(MinValue = 0, MaxValue = 1000)]
		public int MinPoolSize
		{
			get { return (int)base["minPoolSize"]; }
			set { base["minPoolSize"] = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating the maximum amount of sockets per server in the socket pool.
		/// </summary>
		/// <returns>The maximum amount of sockets per server in the socket pool.</returns>
		[ConfigurationProperty("maxPoolSize", IsRequired = false, DefaultValue = 200), IntegerValidator(MinValue = 0, MaxValue = 1000)]
		public int MaxPoolSize
		{
			get { return (int)base["maxPoolSize"]; }
			set { base["maxPoolSize"] = value; }
		}

		/// <summary>
		/// Gets or sets a value that specifies the amount of time after which the connection attempt will fail.
		/// </summary>
		/// <returns>The value of the connection timeout. The default is 10 seconds.</returns>
		[ConfigurationProperty("connectionTimeout", IsRequired = false, DefaultValue = "00:00:10"), PositiveTimeSpanValidator, TypeConverter(typeof(InfiniteTimeSpanConverter))]
		public TimeSpan ConnectionTimeout
		{
			get { return (TimeSpan)base["connectionTimeout"]; }
			set { base["connectionTimeout"] = value; }
		}

		/// <summary>
		/// Gets or sets a value that specifies the amount of time after which receiving data from the socket fails.
		/// </summary>
		/// <returns>The value of the receive timeout. The default is 10 seconds.</returns>
		[ConfigurationProperty("receiveTimeout", IsRequired = false, DefaultValue = "00:00:10"), PositiveTimeSpanValidator, TypeConverter(typeof(InfiniteTimeSpanConverter))]
		public TimeSpan ReceiveTimeout
		{
			get { return (TimeSpan)base["receiveTimeout"]; }
			set { base["receiveTimeout"] = value; }
		}

		/// <summary>
		/// Gets or sets a value that specifies the amount of time after which an unresponsive (dead) server will be checked if it is working.
		/// </summary>
		/// <returns>The value of the dead timeout. The default is 2 minutes.</returns>
		[ConfigurationProperty("deadTimeout", IsRequired = false, DefaultValue = "00:02:00"), PositiveTimeSpanValidator, TypeConverter(typeof(InfiniteTimeSpanConverter))]
		public TimeSpan DeadTimeout
		{
			get { return (TimeSpan)base["deadTimeout"]; }
			set { base["deadTimeout"] = value; }
		}

		/// <summary>
		/// Called after deserialization.
		/// </summary>
		protected override void PostDeserialize()
		{
			base.PostDeserialize();

			if(MinPoolSize > MaxPoolSize)
				throw new ConfigurationErrorsException("maxPoolSize must be larger than minPoolSize.");
		}

		#region [ ISocketPoolConfiguration     ]

		int ISocketPoolConfiguration.MinPoolSize
		{
			get { return MinPoolSize; }
			set { MinPoolSize = value; }
		}

		int ISocketPoolConfiguration.MaxPoolSize
		{
			get { return MaxPoolSize; }
			set { MaxPoolSize = value; }
		}

		TimeSpan ISocketPoolConfiguration.ConnectionTimeout
		{
			get { return ConnectionTimeout; }
			set { ConnectionTimeout = value; }
		}

		TimeSpan ISocketPoolConfiguration.DeadTimeout
		{
			get { return DeadTimeout; }
			set { DeadTimeout = value; }
		}

		#endregion
	}
}
