using System.Collections.Generic;
using System.Threading.Tasks;

namespace Datakit.Abstractions
{
    public static class DatakitFactory
    {
        public static IRecord NewRecord(IBranch parent, List<string> path)
        {
            var client = Client.Instance;
            var r = new Record(client, parent, path);
            return r;
        }

        public static async Task<IBranch> NewBranch(string name)
        {
            var client = Client.Instance;
            var r = new Branch(client, name);
            await client.Mkdir(r.Path);
            return r;
        }

        public static async Task<ITransaction> NewTransaction(IBranch parent, string name)
        {
            var client = Client.Instance;
            var r = new Transaction(client, parent, name);
            await client.Mkdir(r.Path);
            return r;
        }
    }
}
