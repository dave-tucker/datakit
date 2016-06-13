using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Datakit.Abstractions
{
    public interface IBranch
    {
        List<string> Path { get; set; }
        string Name { get; set; }
        Task<string> Head();
        void FastForward(string sha);
        Task<ITransaction> NewTransaction(string name);
        IRecord NewRecord(List<string> path);
        Task<string> Read(List<string> key);
    }

    public class Branch : IBranch
    {
        private const string BranchKey = "branch";
        private const string HeadKey = "head";
        private const string Ro = "ro";
        private readonly IClient _client;

        public Branch(IClient client, string name)
        {
            _client = client;
            Name = name;
            Path = new List<string> {BranchKey, name};
        }

        public List<string> Path { get; set; }
        public string Name { get; set; }

        public async Task<string> Head()
        {
            var path = Path.ToList();
            path.Add(HeadKey);
            var result = await _client.ReadAll(path);
            if (result == null || result == "\n" || result.Length < 2)
            {
                throw new NoHeadException("Cannot get HEAD of branch");
            }
            return result;
        }
        
        public void FastForward(string sha)
        {
            throw new NotImplementedException();
        }

        public async Task<ITransaction> NewTransaction(string name)
        {
            return await DatakitFactory.NewTransaction(this, name);
        }

        public IRecord NewRecord(List<string> path)
        {
            return DatakitFactory.NewRecord(this, path);
        }

        public async Task<string> Read(List<string> key)
        {
            var path = Path.ToList();
            path.Add(Ro);
            path.AddRange(key);
            return await _client.ReadAll(path);
        }
    }
}