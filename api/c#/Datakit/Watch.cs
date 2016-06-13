using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Datakit.Abstractions
{
    public interface IWatch
    {
        Task<Snapshot> Next();
    }

    public class Watch : IWatch
    {
        private const string WatchKey = "watch";
        private const string TreeLive = "tree.live";
        private const string NodeSuffix = ".node";
        private readonly IClient _client;
        private readonly uint _fid;

        public Watch(IClient client, uint fid)
        {
            _client = client;
            _fid = fid;
        }

        public static async Task<IWatch> CreateWatch(IClient client, IEnumerable<string> parentPath, IEnumerable<string> path)
        {
            var watchPath = parentPath.ToList();
            watchPath.Add(WatchKey);
            var nodes = path.Select(node => $"{node}{NodeSuffix}").ToList();
            nodes.Add(TreeLive);
            watchPath.AddRange(nodes);
            var fid = await client.Open(watchPath, Sharp9P.Constants.Oread);
            // read initial state
            var intialState = await client.Read(fid, 0);
            return new Watch(client, fid);
        }

        public async Task<Snapshot> Next()
        {
            string data;
            while (true)
            {
                data = await _client.Read(_fid, 43);
                if (data == null)
                {
                    continue;
                }
                if (data.Equals("\n"))
                {
                    // Path doesn't exist yet
                    continue;
                }
                break;
            }
            var results = data.Split(new[] {"\n"}, StringSplitOptions.None);
            var sha = Array.FindLast(results, item => item != "");
            if (sha == null)
            {
                throw new Exception($"sha was null. result was: {data}");
            }
            return new ObjectSnapsot(_client, sha);
        }

        ~Watch()
        {
            _client.Close(_fid);
        }
    }
}