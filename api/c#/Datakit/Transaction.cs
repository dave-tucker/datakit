using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Datakit.Abstractions
{
    public interface ITransaction
    {
        string Name { get; set; }
        string Message { get; set; }
        List<string> Path { get; set; }
        Task Write(List<string> key, string value);
        Task Commit();
        Task Close();
    }

    public class Transaction : ITransaction
    {
        private const string Ctl = "ctl";
        private const string CtlCommit = "commit";
        private const string CtlClose = "close";
        private const string Transactions = "transactions";
        private const string Rw = "rw";
        private readonly IClient _client;

        public Transaction(IClient client, IBranch parent, string name)
        {
            _client = client;
            Name = name;
            var path = parent.Path.ToList();
            path.Add(Transactions);
            path.Add(name);
            Path = path;
        }

        public async Task Init()
        {
            await _client.Mkdir(Path);
        }

        public string Name { get; set; }
        public string Message { get; set; }
        public List<string> Path { get; set; }

        public async Task Write(List<string> key, string value)
        {
            var path = Path.ToList();
            path.Add(Rw);
            path.AddRange(key);
            var dirs = path.Take(path.Count - 1).ToList();
            await _client.Mkdir(dirs);
            await _client.Write(path, value, true);
        }

        public async Task Commit()
        {
            var path = Path.ToList();
            path.Add(Ctl);
            await _client.Write(path, CtlCommit, false);
        }

        public async Task Close()
        {
            var path = Path.ToList();
            path.Add(Ctl);
            await _client.Write(path, CtlClose, false);
        }
    }
}