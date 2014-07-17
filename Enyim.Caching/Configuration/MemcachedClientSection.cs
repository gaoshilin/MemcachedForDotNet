using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Configuration;
using System.ComponentModel;
using System.Net;
using Enyim.Caching.Memcached.Transcoders;

namespace Enyim.Caching.Configuration
{
    /// <summary>
    /// Configures the <see cref="T:MemcachedClient"/>. This class cannot be inherited.
    /// </summary>
    public sealed class MemcachedClientSection : ConfigurationSection, IMemcachedClientConfiguration
    {
        /// <summary>
        /// Returns a collection of Memcached servers which can be used by the client.
        /// </summary>
        [ConfigurationProperty("servers", IsRequired = true)]
        public EndPointElementCollection Servers
        {
            get { return (EndPointElementCollection)base["servers"]; }
        }

        /// <summary>
        /// Gets or sets the configuration of the socket pool.
        /// </summary>
        [ConfigurationProperty("socketPool", IsRequired = false)]
        public SocketPoolElement SocketPool
        {
            get { return (SocketPoolElement)base["socketPool"]; }
            set { base["socketPool"] = value; }
        }

        /// <summary>
        /// Gets or sets the type of the <see cref="T:Enyim.Caching.Memcached.IMemcachedKeyTransformer"/> which will be used to convert item keys for Memcached.
        /// </summary>
        [ConfigurationProperty("keyTransformer", IsRequired = false), TypeConverter(typeof(TypeNameConverter)), InterfaceValidator(typeof(Memcached.IMemcachedKeyTransformer))]
        public Type KeyTransformer
        {
            get { return (Type)base["keyTransformer"]; }
            set { base["keyTransformer"] = value; }
        }

        /// <summary>
        /// Gets or sets the type of the <see cref="T:Enyim.Caching.Memcached.IMemcachedNodeLocator"/> which will be used to assign items to Memcached nodes.
        /// </summary>
        [ConfigurationProperty("nodeLocator", IsRequired = false), TypeConverter(typeof(TypeNameConverter)), InterfaceValidator(typeof(Memcached.IMemcachedNodeLocator))]
        public Type NodeLocator
        {
            get { return (Type)base["nodeLocator"]; }
            set { base["nodeLocator"] = value; }
        }

        /// <summary>
        /// Gets or sets the type of the <see cref="T:Enyim.Caching.Memcached.ITranscoder"/> which will be used serialzie or deserialize items.
        /// </summary>
        [ConfigurationProperty("transcoder", IsRequired = false), TypeConverter(typeof(TypeNameConverter)), InterfaceValidator(typeof(ITranscoder))]
        public Type Transcoder
        {
            get { return (Type)base["transcoder"]; }
            set { base["transcoder"] = value; }
        }

        /// <summary>
        /// Called after deserialization.
        /// </summary>
        protected override void PostDeserialize()
        {
            WebContext hostingContext = EvaluationContext.HostingContext as WebContext;

            if (hostingContext != null && hostingContext.ApplicationLevel == WebApplicationLevel.BelowApplication)
            {
                throw new InvalidOperationException("The " + SectionInformation.SectionName + " section cannot be defined below the application level.");
            }
        }

        #region [ IMemcachedClientConfiguration]
        IList<IPEndPoint> IMemcachedClientConfiguration.Servers
        {
            get { return Servers.ToIPEndPointCollection(); }
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
    }
}