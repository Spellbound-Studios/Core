// Copyright 2026 Spellbound Studio Inc.

using System.Collections.Generic;
using Spellbound.Core.Logging;

namespace Spellbound.Core.Registries {
    /// <summary>
    /// Default in-memory <see cref="IRegistry{TEntry}"/> backend: a uint-hash to entry dictionary.
    /// </summary>
    public class HashRegistry<TEntry> : IRegistry<TEntry> where TEntry : class, IRegistryEntry {
        private readonly Dictionary<uint, TEntry> _byHash = new();
        private readonly List<TEntry> _all = new();

        public int Count => _byHash.Count;
        public IReadOnlyList<TEntry> All => _all;

        public void Clear() {
            _byHash.Clear();
            _all.Clear();
        }

        /// <summary>
        /// Registers one entry.
        /// </summary>
        public void Add(TEntry entry) {
            if (entry == null)
                return;

            var hash = entry.Hash;

            if (hash == 0u) {
                Log.Error($"HashRegistry<{typeof(TEntry).Name}>: '{entry}' has hash 0 (reserved null). Skipped.");

                return;
            }

            if (!_byHash.TryAdd(hash, entry)) {
                Log.Error(
                    $"HashRegistry<{typeof(TEntry).Name}>: hash collision {hash} " +
                    $"('{_byHash[hash]}' vs '{entry}'). Kept first.");

                return;
            }

            _all.Add(entry);
        }

        public void AddRange(IReadOnlyList<TEntry> entries) {
            if (entries == null)
                return;

            foreach (var t in entries)
                Add(t);
        }

        public bool TryGet(uint hash, out TEntry entry) {
            if (hash != 0u) 
                return _byHash.TryGetValue(hash, out entry);

            entry = null;

            return false;

        }

        public TEntry Get(uint hash) {
            if (hash != 0u && _byHash.TryGetValue(hash, out var entry))
                return entry;

            Log.Error($"HashRegistry<{typeof(TEntry).Name}>: no entry for hash {hash}.");

            return null;
        }

        public bool Contains(uint hash) => hash != 0u && _byHash.ContainsKey(hash);
    }
}
