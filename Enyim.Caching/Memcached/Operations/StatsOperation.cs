using System;
using System.Collections.Generic;
using System.Net;

namespace Enyim.Caching.Memcached.Operations
{
	internal sealed class StatsOperation : Operation
	{
		private readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(StatsOperation));

		private ServerStats results;

		public StatsOperation(ServerPool pool) : base(pool) { }

		public ServerStats Results
		{
			get { return results; }
		}

		protected override bool ExecuteAction()
		{
			Dictionary<IPEndPoint, Dictionary<string, string>> retval = new Dictionary<IPEndPoint, Dictionary<string, string>>();

			foreach (MemcachedNode server in ServerPool.WorkingServers)
			{
				using (PooledSocket ps = server.Acquire())
				{
					if (ps == null)
						continue;

					ps.SendCommand("stats");

					Dictionary<string, string> serverData = new Dictionary<string, string>(StringComparer.Ordinal);

					while (true)
					{
						string line = ps.ReadResponse();

						// stat values are terminated by END
						if (String.Compare(line, "END", StringComparison.Ordinal) == 0)
							break;

						// expected response is STAT item_name item_value
						if (line.Length < 6 || String.Compare(line, 0, "STAT ", 0, 5, StringComparison.Ordinal) != 0)
						{
							if (log.IsWarnEnabled)
								log.Warn("Unknow response: " + line);

							continue;
						}

						// get the key&value
						string[] parts = line.Remove(0, 5).Split(' ');
						if (parts.Length != 2)
						{
							if (log.IsWarnEnabled)
								log.Warn("Unknow response: " + line);

							continue;
						}

						// store the stat item
						serverData[parts[0]] = parts[1];
					}

					retval[server.EndPoint] = serverData;
				}
			}

			results = new ServerStats(retval);

			return true;
		}
	}
} 