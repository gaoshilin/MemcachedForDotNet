using System;
using System.Collections.Generic;
using System.Net;
using Enyim.Caching.Memcached;
using Enyim.Caching.Memcached.Transcoders;

namespace Enyim.Caching.Configuration
{
    /// <summary>
    /// COnfiguration class
    /// </summary>
    public class MemcachedClientConfiguration : IMemcachedClientConfiguration
    {
        private readonly List<IPEndPoint> servers;
        private readonly ISocketPoolConfiguration socketPool;
        private Type keyTransformer;
        private Type nodeLocator;
        private Type transcoder;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:MemcachedClientConfiguration"/> class.
        /// </summary>
        public MemcachedClientConfiguration()
        {
            servers = new List<IPEndPoint>();
            socketPool = new _SocketPoolConfig();
        }

        /// <summary>
        /// Gets a list of <see cref="T:IPEndPoint"/> each representing a Memcached server in the pool.
        /// </summary>
        public IList<IPEndPoint> Servers
        {
            get { return servers; }
        }

        /// <summary>
        /// Gets the configuration of the socket pool.
        /// </summary>
        public ISocketPoolConfiguration SocketPool
        {
            get { return socketPool; }
        }

        /// <summary>
        /// Gets or sets the type of the <see cref="T:Enyim.Caching.Memcached.IMemcachedKeyTransformer"/> which will be used to convert item keys for Memcached.
        /// </summary>
        public Type KeyTransformer
        {
            get { return keyTransformer; }
            set
            {
                ConfigurationHelper.CheckForInterface(value, typeof(IMemcachedKeyTransformer));

                keyTransformer = value;
            }
        }

        /// <summary>
        /// Gets or sets the type of the <see cref="T:Enyim.Caching.Memcached.IMemcachedNodeLocator"/> which will be used to assign items to Memcached nodes.
        /// </summary>
        public Type NodeLocator
        {
            get { return nodeLocator; }
            set
            {
                ConfigurationHelper.CheckForInterface(value, typeof(IMemcachedNodeLocator));

                nodeLocator = value;
            }
        }

        /// <summary>
        /// Gets or sets the type of the <see cref="T:Enyim.Caching.Memcached.ITranscoder"/> which will be used serialzie or deserialize items.
        /// </summary>
        public Type Transcoder
        {
            get { return transcoder; }
            set
            {
                ConfigurationHelper.CheckForInterface(value, typeof(ITranscoder));

                transcoder = value;
            }
        }

        #region [ IMemcachedClientConfiguration]

        IList<IPEndPoint> IMemcachedClientConfiguration.Servers
        {
            get { return Servers; }
        }

        ISocketPoolConfiguration IMemcachedClientConfiguration.SocketPool
        {
            get { return SocketPool; }
        }

        Type IMemcachedClientConfiguration.KeyTransformer
        {
            get { return KeyTransformer; }
            set { KeyTransformer = value; }
        }

        Type IMemcachedClientConfiguration.NodeLocator
        {
            get { return NodeLocator; }
            set { NodeLocator = value; }
        }

        Type IMemcachedClientConfiguration.Transcoder
        {
            get { return Transcoder; }
            set { Transcoder = value; }
        }
        #endregion
        #region [ T:SocketPoolConfig           ]
        private class _SocketPoolConfig : ISocketPoolConfiguration
        {
            private int minPoolSize = 10;
            private int maxPoolSize = 200;
            private TimeSpan connectionTimeout = new TimeSpan(0, 0, 10);
            private TimeSpan receiveTimeout = new TimeSpan(0, 0, 10);
            private TimeSpan deadTimeout = new TimeSpan(0, 2, 0);

            int ISocketPoolConfiguration.MinPoolSize
            {
                get { return minPoolSize; }
                set
                {
                    if (value > 1000 || value > maxPoolSize)
                        throw new ArgumentOutOfRangeException("value", "MinPoolSize must be <= MaxPoolSize and must be <= 1000");

                    minPoolSize = value;
                }
            }

            int ISocketPoolConfiguration.MaxPoolSize
            {
                get { return maxPoolSize; }
                set
                {
                    if (value > 1000 || value < minPoolSize)
                        throw new ArgumentOutOfRangeException("value", "MaxPoolSize must be >= MinPoolSize and must be <= 1000");

                    maxPoolSize = value;
                }
            }

            TimeSpan ISocketPoolConfiguration.ConnectionTimeout
            {
                get { return connectionTimeout; }
                set
                {
                    if (value < TimeSpan.Zero)
                        throw new ArgumentOutOfRangeException("value", "value must be positive");

                    connectionTimeout = value;
                }
            }

            TimeSpan ISocketPoolConfiguration.ReceiveTimeout
            {
                get { return receiveTimeout; }
                set
                {
                    if (value < TimeSpan.Zero)
                        throw new ArgumentOutOfRangeException("value", "value must be positive");

                    receiveTimeout = value;
                }
            }

            TimeSpan ISocketPoolConfiguration.DeadTimeout
            {
                get { return deadTimeout; }
                set
                {
                    if (value < TimeSpan.Zero)
                        throw new ArgumentOutOfRangeException("value", "value must be positive");

                    deadTimeout = value;
                }
            }
        }
        #endregion
    }
}