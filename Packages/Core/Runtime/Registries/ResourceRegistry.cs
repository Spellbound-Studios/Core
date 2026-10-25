// Copyright 2026 Spellbound Studio Inc.

using System;
using System.Collections.Generic;
using Spellbound.Core.Logging;
using UnityEngine;

namespace Spellbound.Core.Registries {
    public sealed class ResourceRegistry<TEntry> : IRegistry<TEntry>
            where TEntry : ScriptableObject, IRegistryEntry {
        private readonly string _resourceFolder;
        private readonly Func<TEntry, string> _nameSelector;
        private readonly Action<TEntry> _validateEntry;
        private readonly Func<string, TEntry[]> _loader;

        private readonly HashRegistry<TEntry> _entries = new();
        private readonly Dictionary<string, TEntry> _byName = new();

        private bool _isLoaded;

        public ResourceRegistry(
            string resourceFolder,
            Func<TEntry, string> nameSelector = null,
            Action<TEntry> validateEntry = null,
            Func<string, TEntry[]> loader = null) {
            _resourceFolder = resourceFolder ?? string.Empty;
            _nameSelector = nameSelector;
            _validateEntry = validateEntry;
            _loader = loader ?? Resources.LoadAll<TEntry>;
        }

        public int Count {
            get {
                EnsureLoaded();

                return _entries.Count;
            }
        }

        public IReadOnlyList<TEntry> All {
            get {
                EnsureLoaded();

                return _entries.All;
            }
        }

        public bool TryGet(uint hash, out TEntry entry) {
            EnsureLoaded();

            return _entries.TryGet(hash, out entry);
        }

        public TEntry Get(uint hash) {
            EnsureLoaded();

            return _entries.TryGet(hash, out var entry) ? entry : null;
        }

        public bool Contains(uint hash) {
            EnsureLoaded();

            return _entries.Contains(hash);
        }

        public TEntry Get(string entryName) {
            RequireNameIndex();
            EnsureLoaded();

            return entryName != null && _byName.TryGetValue(entryName, out var entry) ? entry : null;
        }

        public uint GetHash(string entryName) {
            var entry = Get(entryName);

            if (entry == null) {
                throw new KeyNotFoundException(
                    $"{typeof(TEntry).Name} '{entryName}' is not registered. Author one under {DescribeFolder()}.");
            }

            return entry.Hash;
        }

        public bool TryGetHash(string entryName, out uint hash) {
            var entry = Get(entryName);
            hash = entry == null ? 0u : entry.Hash;

            return entry != null;
        }

        public bool IsRegistered(string entryName) => Get(entryName) != null;

        public string GetName(uint hash) {
            RequireNameIndex();
            EnsureLoaded();

            return _entries.TryGet(hash, out var entry) ? _nameSelector(entry) : null;
        }

        public bool TryGetName(uint hash, out string entryName) {
            entryName = GetName(hash);

            return entryName != null;
        }

        public void EnsureLoaded() {
            if (_isLoaded)
                return;

            Load();
        }

        public void Reload() {
            Clear();
            Load();
        }

        public void Reset() => Clear();

        private void Load() {
            try {
                var loaded = _loader(_resourceFolder);

                if (loaded == null)
                    return;

                foreach (var entry in loaded) {
                    if (entry == null)
                        continue;

                    if (entry.Hash == 0u) {
                        Log.Error(
                            $"{typeof(TEntry).Name} '{entry.name}' has no stamped identity; select the asset once in " +
                            "the editor so it restamps, then save the project. Entry skipped.");

                        continue;
                    }

                    RejectHashCollision(entry);
                    IndexByName(entry);

                    _validateEntry?.Invoke(entry);

                    _entries.Add(entry);
                }
            }
            catch {
                Clear();

                throw;
            }

            _isLoaded = true;
        }

        private void RejectHashCollision(TEntry entry) {
            if (!_entries.TryGet(entry.Hash, out var existing))
                return;

            throw new InvalidOperationException(
                $"{typeof(TEntry).Name} hash collision: asset '{entry.name}' collides with '{existing.name}' at hash " +
                $"{entry.Hash}. Regenerate one asset's GUID to resolve.");
        }

        private void IndexByName(TEntry entry) {
            if (_nameSelector == null)
                return;

            var entryName = _nameSelector(entry);

            if (string.IsNullOrEmpty(entryName)) {
                throw new InvalidOperationException(
                    $"{typeof(TEntry).Name} asset '{entry.name}' has an empty name. This registry indexes by name, so " +
                    "every entry needs one.");
            }

            if (_byName.TryAdd(entryName, entry))
                return;

            throw new InvalidOperationException(
                $"Duplicate {typeof(TEntry).Name} name: '{entryName}' (asset '{entry.name}') is already registered by " +
                $"asset '{_byName[entryName].name}'. Names must be unique, rename one.");
        }

        private void RequireNameIndex() {
            if (_nameSelector != null)
                return;

            throw new InvalidOperationException(
                $"ResourceRegistry<{typeof(TEntry).Name}> was built without a name selector, so it cannot resolve by " +
                "name. Pass one to the constructor.");
        }

        private void Clear() {
            _entries.Clear();
            _byName.Clear();
            _isLoaded = false;
        }

        private string DescribeFolder() =>
                string.IsNullOrEmpty(_resourceFolder) ? "a Resources folder" : $"Resources/{_resourceFolder}";
    }
}
