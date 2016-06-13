﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Datakit.Abstractions
{
    public interface IUpdateable
    {
        Task OnUpdate(int version, Snapshot snapshot, List<string> path);
    }

    public class Field<T> : Field
    {
        private const int DefaultVersion = 1;

        public Field(IRecord parent, string key, T defaultValue)
        {
            _parent = parent;
            Version = DefaultVersion;
            Key = key;
            Path = new List<string>(key.Split(new[] {"/"}, StringSplitOptions.None));
            RawValue = defaultValue;
            RawDefaultValue = defaultValue;
        }

        private readonly IRecord _parent;

        public T RawValue { get; protected set;}
        public T RawDefaultValue { get; set; }
        public override string Value => RawValue.ToString().ToLower();
        public override string DefaultValue => RawDefaultValue.ToString().ToLower();
        private readonly object _lock = new object();

        public override async Task OnUpdate(int version, Snapshot snapshot, List<string> path)
        {
            // Path should be relative to the watch
            var newString = await snapshot.Read(Path);
            var newValue = (T) Convert.ChangeType(newString, typeof(T));
            if (RawValue.Equals(newValue)) return;
            RawValue = newValue;
            Version = version;     
        }

        public override async void SetValue(string description, string value)
        {
            var transactionName = $"update-{Path.Last()}-{Version}";
            var t = await _parent.Parent.NewTransaction(transactionName);
            t.Message = description;
            var path = _parent.Path.ToList();
            path.AddRange(Path);
            await t.Write(path, value);
            await t.Commit();
            // RawValue and Version are updated through OnUpdate
        }
    }

    public abstract class Field : IUpdateable
    {
        public List<string> Path;
        public string Key { get; protected set; }
        public int Version { get; protected set; }
        public abstract string Value { get; }
        public abstract string DefaultValue { get; }
        public bool RequiresReboot { get; set; }

        public abstract Task OnUpdate(int version, Snapshot snapshot, List<string> path);

        public abstract void SetValue(string description, string value);

        public bool HasChanged(int version)
        {
            return version < Version;
        }
    }
}