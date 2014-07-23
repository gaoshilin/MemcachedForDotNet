using System;
using Enyim.Caching;
using Enyim.Caching.Memcached;

namespace DemoApp
{
    class Program
    {
        private static readonly MemcachedClient MemClient = new MemcachedClient();
        static void Main()
        {
            //����Mencached�ͻ���ʵ�� create a MemcachedClient
            // in your application you can cache the client in a static variable or just recreate it every time

            //���Դ�config�ļ��д��������Ĳ�ͬ�Ľڵ� you can create another client using a different section from your app/web.config
            //��Щ�ͻ��˿��԰�����ͬ��pool settings, key transformer, ���趨 this client instance can have different pool settings, key transformer, etc.
            // MemcachedClient mc2 = new MemcachedClient("memcached");�����ָ����ĳ���ڵ���Ϊ��ʼ�����ò���

            // or just initialize the client from code��������Ҳ������ͨ������������config�ļ��е��������£�
            //
            // MemcachedClientConfiguration config = new MemcachedClientConfiguration();
            // config.Servers.Add(new IPEndPoint(IPAddress.Loopback, 20002));
            //
            // MemcachedClient mc = new MemcachedClient(config);


            // simple multiget; please note that only 1.2.4 supports it (windows version is at 1.2.1)
            //List<string> keys = new List<string>();

            //for (int i = 1; i < 100; i++)
            //{
            //    string k = "aaaa" + i + "--" + (i * 2);
            //    keys.Add(k);

            //    mc.Store(StoreMode.Set, k, i);
            //}

            //IDictionary<string, ulong> cas;
            //IDictionary<string, object> retvals = mc.Get(keys, out cas);

            //List<string> keys2 = new List<string>(keys);
            //keys2.RemoveRange(0, 50);

            //IDictionary<string, object> retvals2 = mc.Get(keys2, out cas);
            //retvals2 = mc.Get(keys2, out cas);

            //ServerStats ms = mc.Stats();

            // store a string in the cache
            MemClient.Store(StoreMode.Set, "MyKey", "Hello World");

            // retrieve the item from the cache
            Console.WriteLine(MemClient.Get("MyKey"));

            // store some other items
            MemClient.Store(StoreMode.Set, "D1", 1234L);
            MemClient.Store(StoreMode.Set, "D2", DateTime.Now);
            MemClient.Store(StoreMode.Set, "D3", true);
            MemClient.Store(StoreMode.Set, "D4", new Product());

            MemClient.Store(StoreMode.Set, "D5", new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });


            //mc2.Store(StoreMode.Set, "D1", 1234L);
            //mc2.Store(StoreMode.Set, "D2", DateTime.Now);
            //mc2.Store(StoreMode.Set, "D3", true);
            //mc2.Store(StoreMode.Set, "D4", new Product());

            Console.WriteLine("D1: {0}", MemClient.Get("D1"));
            Console.WriteLine("D2: {0}", MemClient.Get("D2"));
            Console.WriteLine("D3: {0}", MemClient.Get("D3"));
            Console.WriteLine("D4: {0}", MemClient.Get("D4"));

            MemClient.Get<byte[]>("D5");

            // delete them from the cache
            MemClient.Remove("D1");
            MemClient.Remove("D2");
            MemClient.Remove("D3");
            MemClient.Remove("D4");

            // add an item which is valid for 10 mins
            MemClient.Store(StoreMode.Set, "D4", new Product(), new TimeSpan(0, 10, 0));

            Console.ReadLine();
        }

        // objects must be serializable to be able to store them in the cache
        [Serializable]
        class Product
        {
            private const double Price = 1.24;
            private const string Name = "Mineral Water";

            public override string ToString()
            {
                return String.Format("Product {{{0}: {1}}}", Name, Price);
            }
        }
    }
}
