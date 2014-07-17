using System;
using System.Collections.Generic;

namespace Enyim.Caching.Memcached
{
    /// <summary>
    /// This is a simple node locator with no computation overhead, always returns the first server from the list. Use only in single server deployments.
    /// </summary>
    public sealed class SingleNodeLocator : IMemcachedNodeLocator
    {
        private MemcachedNode node;
        private bool isInitialized;

        void IMemcachedNodeLocator.Initialize(IList<MemcachedNode> nodes)
        {
            if (isInitialized)
                throw new InvalidOperationException("Instance is already initialized.");

            // locking on this is rude but easy
            lock (this)
            {
                if (isInitialized)
                    throw new InvalidOperationException("Instance is already initialized.");

                if (nodes.Count > 0)
                    node = nodes[0];

                isInitialized = true;
            }
        }

        MemcachedNode IMemcachedNodeLocator.Locate(string key)
        {
            if (!isInitialized)
                throw new InvalidOperationException("You must call Initialize first");

            return node;
        }
    }
}
