using System;
using System.Collections.Generic;
using System.Configuration;
using Enyim.Caching.Configuration;
using System.Threading;
using System.Collections.ObjectModel;
using System.Net;
using Enyim.Caching.Memcached.KeyTransformers;
using Enyim.Caching.Memcached.Transcoders;

namespace Enyim.Caching.Memcached
{
    internal class ServerPool : IDisposable
    {
        private static readonly MemcachedClientSection DefaultSettings = ConfigurationManager.GetSection("enyim.com/memcached") as MemcachedClientSection;

        // holds all dead servers which will be periodically rechecked and put back into the working servers if found alive
        readonly List<MemcachedNode> deadServers = new List<MemcachedNode>();
        // holds all of the currently working servers
        readonly List<MemcachedNode> workingServers = new List<MemcachedNode>();

        private ReadOnlyCollection<MemcachedNode> publicWorkingServers;

        // used to synchronize read/write accesses on the server lists
        private ReaderWriterLock serverAccessLock = new ReaderWriterLock();

        private Timer isAliveTimer;
        private readonly IMemcachedClientConfiguration configuration;
        private readonly IMemcachedKeyTransformer keyTransformer;
        private IMemcachedNodeLocator nodeLocator;
        private readonly ITranscoder transcoder;

        public ServerPool() : this(DefaultSettings) { }

        public ServerPool(IMemcachedClientConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException("configuration", "Invalid or missing pool configuration. Check if the enyim.com/memcached section or your custom section presents in the app/web.config.");

            this.configuration = configuration;
            isAliveTimer = new Timer(callback_isAliveTimer, null, (int)this.configuration.SocketPool.DeadTimeout.TotalMilliseconds, (int)this.configuration.SocketPool.DeadTimeout.TotalMilliseconds);

            // create the key transformer instance
            Type t = this.configuration.KeyTransformer;
            keyTransformer = (t == null) ? new DefaultKeyTransformer() : (IMemcachedKeyTransformer)Activator.CreateInstance(t);

            // create the item transcoder instance
            t = this.configuration.Transcoder;
            transcoder = (t == null) ? new DefaultTranscoder() : (ITranscoder)Activator.CreateInstance(t);


            // initialize the server list
            ISocketPoolConfiguration ispc = configuration.SocketPool;

            foreach (IPEndPoint ip in configuration.Servers)
            {
                workingServers.Add(new MemcachedNode(ip, ispc));
            }

            // (re)creates the locator
            RebuildIndexes();
        }

        private void RebuildIndexes()
        {
            serverAccessLock.UpgradeToWriterLock(Timeout.Infinite);

            try
            {
                Type ltype = configuration.NodeLocator;

                IMemcachedNodeLocator l = ltype == null ? new DefaultNodeLocator() : (IMemcachedNodeLocator)Reflection.FastActivator.CreateInstance(ltype);
                l.Initialize(workingServers);

                nodeLocator = l;

                publicWorkingServers = null;
            }
            finally
            {
                serverAccessLock.ReleaseLock();
            }
        }

        /// <summary>
        /// Checks if a dead node is working again.
        /// </summary>
        /// <param name="state"></param>
        private void callback_isAliveTimer(object state)
        {
            serverAccessLock.AcquireReaderLock(Timeout.Infinite);

            try
            {
                if (deadServers.Count == 0)
                    return;

                List<MemcachedNode> resurrectList = deadServers.FindAll(node => !(node == null || !node.Ping()));

                if (resurrectList.Count > 0)
                {
                    serverAccessLock.UpgradeToWriterLock(Timeout.Infinite);

                    resurrectList.ForEach(delegate(MemcachedNode node)
                    {
                        // maybe it got removed while we were waiting for the writer lock upgrade?
                        if (deadServers.Remove(node))
                            workingServers.Add(node);
                    });

                    RebuildIndexes();
                }
            }
            finally
            {
                serverAccessLock.ReleaseLock();
            }
        }

        /// <summary>
        /// Marks a node as dead (unusable)
        ///  - moves hte node to the  "dead list"
        ///  - recreates the locator based on the new list of still functioning servers
        /// </summary>
        /// <param name="node"></param>
        private void MarkAsDead(MemcachedNode node)
        {
            serverAccessLock.UpgradeToWriterLock(Timeout.Infinite);

            try
            {
                // server gained AoeREZ while AFK?
                if (!node.IsAlive)
                {
                    workingServers.Remove(node);
                    deadServers.Add(node);

                    RebuildIndexes();
                }
            }
            finally
            {
                serverAccessLock.ReleaseLock();
            }
        }

        /// <summary>
        /// Returns the <see cref="t:IKeyTransformer"/> instance used by the pool
        /// </summary>
        public IMemcachedKeyTransformer KeyTransformer
        {
            get { return keyTransformer; }
        }

        public IMemcachedNodeLocator NodeLocator
        {
            get { return nodeLocator; }
        }

        public ITranscoder Transcoder
        {
            get { return transcoder; }
        }

        /// <summary>
        /// Finds the <see cref="T:MemcachedNode"/> which is responsible for the specified item
        /// </summary>
        /// <param name="itemKey"></param>
        /// <returns></returns>
        private MemcachedNode LocateNode(string itemKey)
        {
            serverAccessLock.AcquireReaderLock(Timeout.Infinite);

            try
            {
                MemcachedNode node = NodeLocator.Locate(itemKey);
                if (node == null)
                    return null;

                if (node.IsAlive)
                    return node;

                MarkAsDead(node);

                return LocateNode(itemKey);
            }
            finally
            {
                serverAccessLock.ReleaseLock();
            }
        }

        public PooledSocket Acquire(string itemKey)
        {
            if (serverAccessLock == null)
                throw new ObjectDisposedException("ServerPool");

            MemcachedNode server = LocateNode(itemKey);

            if (server == null)
                return null;

            return server.Acquire();
        }

        public ReadOnlyCollection<MemcachedNode> WorkingServers
        {
            get
            {
                if (publicWorkingServers == null)
                {
                    serverAccessLock.AcquireReaderLock(Timeout.Infinite);

                    try
                    {
                        if (publicWorkingServers == null)
                        {
                            return publicWorkingServers = new ReadOnlyCollection<MemcachedNode>(workingServers.FindAll(node => true));
                        }
                    }
                    finally
                    {
                        serverAccessLock.ReleaseLock();
                    }
                }

                return publicWorkingServers;
            }
        }

        public IDictionary<MemcachedNode, IList<string>> SplitKeys(IEnumerable<string> keys)
        {
            Dictionary<MemcachedNode, IList<string>> keysByNode = new Dictionary<MemcachedNode, IList<string>>(MemcachedNode.Comparer.Instance);

            foreach (string key in keys)
            {
                MemcachedNode node = LocateNode(key);

                IList<string> nodeKeys;
                if (!keysByNode.TryGetValue(node, out nodeKeys))
                {
                    nodeKeys = new List<string>();
                    keysByNode.Add(node, nodeKeys);
                }

                nodeKeys.Add(key);
            }

            return keysByNode;
        }

        #region [ IDisposable                  ]
        void IDisposable.Dispose()
        {
            ReaderWriterLock rwl = serverAccessLock;

            if (rwl == null)
                return;

            GC.SuppressFinalize(this);

            serverAccessLock = null;

            try
            {
                rwl.UpgradeToWriterLock(Timeout.Infinite);

                deadServers.ForEach(node => node.Dispose());
                workingServers.ForEach(node => node.Dispose());

                deadServers.Clear();
                workingServers.Clear();
                nodeLocator = null;

                isAliveTimer.Dispose();
                isAliveTimer = null;
            }
            finally
            {
                rwl.ReleaseLock();
            }
        }
        #endregion
    }
}

#region [ License information          ]
/* ************************************************************
 *
 * Copyright (c) Attila Kiskó, enyim.com, 2007
 *
 * This source code is subject to terms and conditions of 
 * Microsoft Permissive License (Ms-PL).
 * 
 * A copy of the license can be found in the License.html
 * file at the root of this distribution. If you can not 
 * locate the License, please send an email to a@enyim.com
 * 
 * By using this source code in any fashion, you are 
 * agreeing to be bound by the terms of the Microsoft 
 * Permissive License.
 *
 * You must not remove this notice, or any other, from this
 * software.
 *
 * ************************************************************/
#endregion