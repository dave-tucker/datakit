using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Datakit.Abstractions
{
    public interface IClient
    {
        Task Mkdir(List<string> path);
        Task<uint> Open(List<string> path, byte mode);
        Task Close(uint fid);
        Task Write(List<string> path, string data, bool create);
        Task<string> ReadAll(List<string> path);
        Task<string> Read(uint fid, ulong offset);
        void Start();
        void Stop();
    }

    public class Client : IClient
    {
        private const string PipeLocation = ".";
        private const string PipeName = "dockerdb";
        private const int ConnectTimeout = 1000;
        private const int MaxRetries = 100;
        private const string Anyone = "anyone";
        private const string DatabaseRoot = "/";
        private static Client _instance;
        private readonly Sharp9P.Client _client;

        private Client()
        {
            var stream = new NamedPipeClientStream(PipeLocation, PipeName, PipeDirection.InOut, PipeOptions.None,
                TokenImpersonationLevel.None);

            var retries = 0;
            while (true)
            {
                try
                {
                    stream.Connect(ConnectTimeout);
                    break;
                }
                catch (TimeoutException)
                {
                    retries++;
                    if (retries > MaxRetries)
                    {
                        throw new Exception("Unable to connect to database");
                    }
                }
            }
            _client = Sharp9P.Client.FromStream(stream);
          }

        public void Start()
        {
            _client.Start();
            _client.Version(Sharp9P.Constants.DefaultMsize, Sharp9P.Constants.DefaultVersion).Wait();
            _client.Attach(Sharp9P.Constants.RootFid, Sharp9P.Constants.NoFid, Anyone, DatabaseRoot).Wait();
        }

        public void Stop()
        {
            _client.Stop();
        }

        public static Client Instance => _instance ?? (_instance = new Client());

        public async Task Mkdir(List<string> path)
        {
            var fid = _client.AllocateFid(Sharp9P.Constants.RootFid);
            foreach (var dir in path)
            {
                var dirFid = _client.AllocateFid(fid.Result);
                try
                {
                    await _client.Create(dirFid.Result, dir, Constants.DirPerm, Sharp9P.Constants.Oread);
                }
                catch
                {
                    // Ignore File Exists error
                }
                await _client.FreeFid(dirFid.Result);
                await _client.Walk(fid.Result, fid.Result, new[] {dir});
            }
        }

        public async Task<uint> Open(List<string> path, byte mode)
        {
            var fid = _client.AllocateFid(Sharp9P.Constants.RootFid);
            await _client.Walk(fid.Result, fid.Result, path.ToArray());
            await _client.Open(fid.Result, mode);
            return fid.Result;
        }

        public async Task Close(uint fid)
        {
            await _client.FreeFid(fid);
        }

        public async Task Write(List<string> path, string data, bool create)
        {
            var fid = await _client.AllocateFid(Sharp9P.Constants.RootFid);
            var dirs = path.Take(path.Count - 1).ToArray();
            var utf8 = new UTF8Encoding();
            await _client.Walk(fid, fid, dirs);
            if (create)
            {
                try
                {
                    await _client.Create(fid, path.Last(), Constants.FilePerm, Sharp9P.Constants.Ordwr);
                }
                catch
                {
                    // ignore File Exists error
                }
            }
            else
            {
                await _client.Walk(fid, fid, new[] {path.Last()});
                await _client.Open(fid, Sharp9P.Constants.Ordwr);
            }
            await _client.Write(fid, 0, (uint) utf8.GetByteCount(data), utf8.GetBytes(data));
            await _client.FreeFid(fid);
        }

        public async Task<string> ReadAll(List<string> path)
        {
            var fid = await _client.AllocateFid(Sharp9P.Constants.RootFid);
            var utf8 = new UTF8Encoding();
            await _client.Walk(fid, fid, path.ToArray());
            await _client.Open(fid, Sharp9P.Constants.Oread);
            var read = await _client.Read(fid, 0, 16360);
            await _client.FreeFid(fid);
            return read.Item1 > 0 ? utf8.GetString(read.Item2, 0, (int) read.Item1) : null;
        }

        public async Task<string> Read(uint fid, ulong offset)
        {
            var utf8 = new UTF8Encoding();
            var read = await _client.Read(fid, offset, 16360);
            return read.Item1 > 0 ? utf8.GetString(read.Item2, 0, (int)read.Item1) : null;
        }
    }
}