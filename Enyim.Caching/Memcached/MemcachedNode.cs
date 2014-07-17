using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Enyim.Caching.Configuration;
using System.Diagnostics;
using Enyim.Collections;

namespace Enyim.Caching.Memcached
{
    /// <summary>
    /// Represents a Memcached node in the pool.
    /// </summary>
    [DebuggerDisplay("{{MemcachedNode [ Address: {EndPoint}, IsAlive = {IsAlive}  ]}}")]
    public sealed class MemcachedNode : IDisposable
    {
        private bool isDisposed;
        private readonly int deadTimeout = 2 * 60;

        private readonly IPEndPoint endPoint;
        private readonly ISocketPoolConfiguration config;
        private InternalPoolImpl internalPoolImpl;

        internal MemcachedNode(IPEndPoint endpoint, ISocketPoolConfiguration config)
        {
            endPoint = endpoint;
            this.config = config;
            internalPoolImpl = new InternalPoolImpl(endpoint, config);

            deadTimeout = (int)config.DeadTimeout.TotalSeconds;
            if (deadTimeout < 0)
                throw new InvalidOperationException("deadTimeout must be >= TimeSpan.Zero");
        }

        /// <summary>
        /// Gets the <see cref="T:IPEndPoint"/> of this instance
        /// </summary>
        public IPEndPoint EndPoint
        {
            get { return endPoint; }
        }

        /// <summary>
        /// <para>Gets a value indicating whether the server is working or not. Returns a <b>cached</b> state.</para>
        /// <para>To get real-time information and update the cached state, use the <see cref="M:Ping"/> method.</para>
        /// </summary>
        /// <remarks>Used by the <see cref="T:ServerPool"/> to quickly check if the server's state is valid.</remarks>
        internal bool IsAlive
        {
            get { return internalPoolImpl.IsAlive; }
        }

        /// <summary>
        /// Gets a value indicating whether the server is working or not.
        /// 
        /// If the server is not working, and the "being dead" timeout has been expired it will reinitialize itself.
        /// </summary>
        /// <remarks>It's possible that the server is still not up &amp; running so the next call to <see cref="M:Acquire"/> could mark the instance as dead again.</remarks>
        /// <returns></returns>
        internal bool Ping()
        {
            // is the server working?
            if (internalPoolImpl.IsAlive)
                return true;

            // deadTimeout was set to 0 which means forever
            if (deadTimeout == 0)
                return false;

            TimeSpan diff = DateTime.UtcNow - internalPoolImpl.MarkedAsDeadUtc;

            // only do the real check if the configured time interval has passed
            if (diff.TotalSeconds < deadTimeout)
                return false;

            // it's (relatively) safe to lock on 'this' since 
            // this codepath is (should be) called very rarely
            // if you get here hundreds of times then you have bigger issues
            // and try to make the memcached instaces more stable and/or increase the deadTimeout
            lock (this)
            {
                if (internalPoolImpl.IsAlive)
                    return true;

                // it's easier to create a new pool than reinitializing a dead one
                internalPoolImpl.Dispose();

                if (endPoint != null) internalPoolImpl = new InternalPoolImpl(endPoint, config);
            }

            return true;
        }

        /// <summary>
        /// Acquires a new item from the pool
        /// </summary>
        /// <returns>An <see cref="T:PooledSocket"/> instance which is connected to the memcached server, or <value>null</value> if the pool is dead.</returns>
        internal PooledSocket Acquire()
        {
            return internalPoolImpl.Acquire();
        }

        /// <summary>
        /// Releases all resources allocated by this instance
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            // this is not a graceful shutdown
            // if someone uses a pooled item then 99% that an exception will be thrown
            // somewhere. But since the dispose is mostly used when everyone else is finished
            // this should not kill any kittens
            lock (this)
            {
                if (isDisposed)
                    return;

                isDisposed = true;

                internalPoolImpl.Dispose();
                internalPoolImpl = null;
            }
        }

        void IDisposable.Dispose()
        {
            Dispose();
        }

        #region [ InternalPoolImpl             ]
        private class InternalPoolImpl : IDisposable
        {
            private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(InternalPoolImpl));

            /// <summary>
            /// A list of already connected but free to use sockets
            /// </summary>
            private InterlockedQueue<PooledSocket> freeItems;

            private bool isDisposed;
            private bool isAlive;
            private DateTime markedAsDeadUtc;

            private readonly int minItems;
            private readonly int maxItems;
            private int workingCount;

            private AutoResetEvent itemReleasedEvent;
            private readonly IPEndPoint endPoint;
            private readonly ISocketPoolConfiguration config;

            internal InternalPoolImpl(IPEndPoint endpoint, ISocketPoolConfiguration config)
            {
                isAlive = true;
                endPoint = endpoint;
                this.config = config;

                minItems = config.MinPoolSize;
                maxItems = config.MaxPoolSize;

                if (minItems < 0)
                    throw new InvalidOperationException("minItems must be larger than 0", null);
                if (maxItems < minItems)
                    throw new InvalidOperationException("maxItems must be larger than minItems", null);
                if (this.config.ConnectionTimeout < TimeSpan.Zero)
                    throw new InvalidOperationException("connectionTimeout must be >= TimeSpan.Zero", null);

                freeItems = new InterlockedQueue<PooledSocket>();

                itemReleasedEvent = new AutoResetEvent(false);
                InitPool();
            }

            private void InitPool()
            {
                try
                {
                    if (minItems <= 0) return;
                    for (int i = 0; i < minItems; i++)
                    {
                        freeItems.Enqueue(CreateSocket());

                        // cannot connect to the server
                        if (!IsAlive)
                            break;
                    }
                }
                catch (Exception e)
                {
                    Log.Error("Could not init pool.", e);
                    MarkAsDead();
                }
            }

            private PooledSocket CreateSocket()
            {
                PooledSocket retval = new PooledSocket(endPoint, config.ConnectionTimeout, config.ReceiveTimeout, ReleaseSocket);
                retval.Reset();

                return retval;
            }

            public bool IsAlive
            {
                get { return isAlive; }
            }

            public DateTime MarkedAsDeadUtc
            {
                get { return markedAsDeadUtc; }
            }

            /// <summary>
            /// Acquires a new item from the pool
            /// </summary>
            /// <returns>An <see cref="T:PooledSocket"/> instance which is connected to the memcached server, or <value>null</value> if the pool is dead.</returns>
            public PooledSocket Acquire()
            {
                if (Log.IsDebugEnabled)
                    Log.Debug("Acquiring stream from pool.");

                if (!IsAlive)
                {
                    if (Log.IsDebugEnabled)
                        Log.Debug("Pool is dead, returning null.");

                    return null;
                }

                // every release signals the event, so even if the pool becomes full in the meantime
                // the WaitOne will succeed, and more items will be in the pool than allowed,
                // so reset the event when an item is inserted
                itemReleasedEvent.Reset();

                PooledSocket retval;

                // do we have free items?
                if (freeItems.Dequeue(out retval))
                {
                    try
                    {
                        retval.Reset();

                        if (Log.IsDebugEnabled)
                            Log.Debug("Socket was reset. " + retval.InstanceId);

                        Interlocked.Increment(ref workingCount);

                        return retval;
                    }
                    catch (Exception e)
                    {
                        Log.Error("Failed to reset an acquired socket.", e);

                        MarkAsDead();

                        return null;
                    }
                }
                // free item pool is empty
                if (Log.IsDebugEnabled)
                    Log.Debug("Could not get a socket from the pool.");

                // we are not allowed to create more, so wait for an item to be released back into the pool
                if (workingCount >= maxItems)
                {
                    if (Log.IsDebugEnabled)
                        Log.Debug("Pool is full, wait for a release.");

                    // wait on the event
                    if (!itemReleasedEvent.WaitOne(config.ConnectionTimeout, false))
                    {
                        if (Log.IsDebugEnabled)
                            Log.Debug("Pool is still full, timeouting.");

                        // everyone is working
                        throw new TimeoutException();
                    }
                }

                if (Log.IsDebugEnabled)
                    Log.Debug("Creating a new item.");

                try
                {
                    // okay, create the new item
                    retval = CreateSocket();

                    Interlocked.Increment(ref workingCount);
                }
                catch (Exception e)
                {
                    Log.Error("Failed to create socket.", e);
                    MarkAsDead();

                    return null;
                }

                if (Log.IsDebugEnabled)
                    Log.Debug("Done.");

                return retval;
            }

            private void MarkAsDead()
            {
                if (Log.IsWarnEnabled)
                    Log.WarnFormat("Marking pool {0} as dead", endPoint);

                isAlive = false;
                markedAsDeadUtc = DateTime.UtcNow;
            }

            /// <summary>
            /// Releases an item back into the pool
            /// </summary>
            /// <param name="socket"></param>
            private void ReleaseSocket(PooledSocket socket)
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Debug("Releasing socket " + socket.InstanceId);
                    Log.Debug("Are we alive? " + isAlive);
                }

                if (isAlive)
                {
                    // is it still working (i.e. the server is still connected)
                    if (socket.IsAlive)
                    {
                        // mark the item as free
                        freeItems.Enqueue(socket);

                        // there can be a race condition (see the count check in acquire)
                        // not sure what to do about it
                        Interlocked.Decrement(ref workingCount);

                        // signal the event so if someone is waiting for it can reuse this item
                        itemReleasedEvent.Set();
                    }
                    else
                    {
                        // kill this item
                        socket.Destroy();

                        // mark ourselves as not working for a while
                        MarkAsDead();
                    }
                }
                else
                {
                    // one of our previous sockets has died, so probably all of them are dead
                    // kill the socket thus clearing the pool, and after we become alive
                    // we'll fill the pool with working sockets
                    socket.Destroy();
                }
            }

            /// <summary>
            /// Releases all resources allocated by this instance
            /// </summary>
            public void Dispose()
            {
                // this is not a graceful shutdown
                // if someone uses a pooled item then 99% that an exception will be thrown
                // somewhere. But since the dispose is mostly used when everyone else is finished
                // this should not kill any kittens
                lock (this)
                {
                    CheckDisposed();

                    isDisposed = true;

                    using (itemReleasedEvent)
                        itemReleasedEvent.Reset();

                    itemReleasedEvent = null;

                    PooledSocket ps;

                    while (freeItems.Dequeue(out ps))
                    {
                        try
                        {
                            ps.Destroy();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                            throw;
                        }
                    }

                    freeItems = null;
                }
            }

            private void CheckDisposed()
            {
                if (isDisposed)
                    throw new ObjectDisposedException("pool");
            }

            void IDisposable.Dispose()
            {
                Dispose();
            }
        }
        #endregion
        #region [ Comparer                     ]
        internal sealed class Comparer : IEqualityComparer<MemcachedNode>
        {
            public static readonly Comparer Instance = new Comparer();

            bool IEqualityComparer<MemcachedNode>.Equals(MemcachedNode x, MemcachedNode y)
            {
                return x.EndPoint.Equals(y.EndPoint);
            }

            int IEqualityComparer<MemcachedNode>.GetHashCode(MemcachedNode obj)
            {
                return obj.EndPoint.GetHashCode();
            }
        }
        #endregion
    }
}