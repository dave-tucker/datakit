using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Datakit.Abstractions
{
    public interface IRecord
    {
        List<string> Path { get; set; }
        List<Field> Fields { get; set; }
        IBranch Parent { get; set; }
        int Version { get; set; }
        Task Sync();
        Task WaitForUpdates();
        Task Upgrade(int schemaVersion);
        Task Start();
        void Stop();
    }

    public class Record : IRecord, IDisposable
    {
        private const string SchemaVersionKey = "schema-version";
        private const int DefaultSchemaVersion = 1;
        private const string Defaults = "defaults";
        private const string SchemaUpgrade = "schema_upgrade";
        private const string DefaultCommitMessage = "Set default values";
        private const int DefaultVersion = 1;
        private readonly IClient _client;
        private IWatch _watch;
        public Field<int> SchemaVersion;
        private readonly CancellationTokenSource _tokenSource;
        private Task _updateTask;

        public Record(IClient client, IBranch parent, List<string> path)
        {
            _client = client;
            Parent = parent;
            Path = path;
            SchemaVersion = new Field<int>(this, SchemaVersionKey, DefaultSchemaVersion);
            Fields = new List<Field> {SchemaVersion};
            Version = DefaultVersion;
            _tokenSource = new CancellationTokenSource();
        }

        public List<string> Path { get; set; }
        public List<Field> Fields { get; set; }
        public IBranch Parent { get; set; }
        public int Version { get; set; }

        public async Task Sync()
        {
            var transaction = await Parent.NewTransaction(Defaults);
            var operations = 0;
            var noCommits = false;
            Snapshot snapshot = null;
            try
            {
                snapshot = new CommmitSnapshot(_client, await Parent.Head());
            } catch (Exception)
            {
                noCommits = true;
            }
            if (!noCommits)
            {
                foreach (var field in Fields)
                {
                    var path = Path.ToList();
                    path.AddRange(field.Path);
                    try
                    {
                        await snapshot.Read(path);
                    }
                    catch (Exception)
                    {
                        await transaction.Write(path, field.DefaultValue);
                        operations++;
                    }
                }
            }
            else
            {
                foreach (var field in Fields)
                {
                    var path = Path.ToList();
                    path.AddRange(field.Path);

                    await transaction.Write(path, field.DefaultValue);
                    operations++;
                }
            }

            if (operations > 0)
            {
                transaction.Message = DefaultCommitMessage;
                await transaction.Commit();
            }
            else
            {
                await transaction.Close();
            }
        }

        public async Task WaitForUpdates()
        {
            var snapshot = await _watch.Next();
            Version++;
            foreach (var field in Fields)
            {
                await field.OnUpdate(Version, snapshot, Path);
            }
        }

        public async Task Start()
        {
            _watch = await Watch.CreateWatch(_client, Parent.Path, Path);
            _updateTask = Task.Factory.StartNew(
                UpdateLoop,
                _tokenSource.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        public void Stop()
        {
            _tokenSource.Cancel();
            Task.WaitAll(_updateTask);
        }

        private async Task UpdateLoop()
        {
            while (!_tokenSource.IsCancellationRequested)
            {
                try
                {
                    await WaitForUpdates();
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        public async Task Upgrade(int schemaVersion)
        {
            if (schemaVersion <= SchemaVersion.RawValue)
                return;
            SchemaVersion.RawDefaultValue = schemaVersion;
            var transaction = await Parent.NewTransaction(SchemaUpgrade);
            foreach (var field in Fields)
            {
                var path = Path.ToList();
                path.AddRange(field.Path);
                await transaction.Write(path, field.DefaultValue);
            }
            await transaction.Commit();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}