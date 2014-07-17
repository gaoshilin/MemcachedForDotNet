using System;
using System.Collections.Generic;

namespace Enyim.Caching.Memcached.Operations
{
    internal class MultiGetOperation : Operation
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(MultiGetOperation));

        private readonly IEnumerable<string> keys;
        private Dictionary<string, object> result;
        private Dictionary<string, ulong> casValues;

        public MultiGetOperation(ServerPool pool, IEnumerable<string> keys)
            : base(pool)
        {
            this.keys = keys;
        }

        protected override bool ExecuteAction()
        {
            // {hashed key -> normal key}: will be used when mapping the returned items back to the original keys
            Dictionary<string, string> hashedToReal = new Dictionary<string, string>(StringComparer.Ordinal);

            // {normal key -> hashed key}: we have to hash all keys anyway, so we better cache them to improve performance instead of doing the hashing later again
            Dictionary<string, string> realToHashed = new Dictionary<string, string>(StringComparer.Ordinal);

            IMemcachedKeyTransformer transformer = ServerPool.KeyTransformer;

            // and store them with the originals so we can map the returned items 
            // to the original keys
            foreach (string s in keys)
            {
                string hashed = transformer.Transform(s);

                hashedToReal[hashed] = s;
                realToHashed[s] = hashed;
            }

            // map each key to the appropriate server in the pool
            IDictionary<MemcachedNode, IList<string>> splitKeys = ServerPool.SplitKeys(keys);

            // we'll open 1 socket for each server
            List<PooledSocket> sockets = new List<PooledSocket>();

            try
            {
                // send a 'gets' to each server
                foreach (KeyValuePair<MemcachedNode, IList<string>> kp in splitKeys)
                {
                    // gets <keys>
                    //
                    // keys: key key key key
                    string[] command = new string[kp.Value.Count + 1];
                    command[0] = "gets";
                    kp.Value.CopyTo(command, 1);

                    for (int i = 1; i < command.Length; i++)
                        command[i] = realToHashed[command[i]];

                    PooledSocket socket = kp.Key.Acquire();
                    if (socket == null)
                        continue;

                    sockets.Add(socket);
                    socket.SendCommand(String.Join(" ", command));
                }

                Dictionary<string, object> retval = new Dictionary<string, object>(StringComparer.Ordinal);
                Dictionary<string, ulong> cas = new Dictionary<string, ulong>(StringComparer.Ordinal);

                // process each response and build a dictionary from the results
                foreach (PooledSocket socket in sockets)
                {
                    try
                    {
                        GetResponse r;

                        while ((r = GetHelper.ReadItem(socket)) != null)
                        {
                            string originalKey = hashedToReal[r.Key];

                            retval[originalKey] = ServerPool.Transcoder.Deserialize(r.Item);
                            cas[originalKey] = r.CasValue;
                        }
                    }
                    catch (NotSupportedException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        log.Error(e);
                    }
                }

                result = retval;
                casValues = cas;
            }
            finally
            {
                foreach (PooledSocket socket in sockets)
                    ((IDisposable)socket).Dispose();
            }

            return true;
        }

        public IDictionary<string, object> Result
        {
            get { return result; }
        }

        public IDictionary<string, ulong> CasValues
        {
            get { return casValues; }
        }
    }
}
