// Copyright 2026 Spellbound Studio Inc.

using System.Collections.Generic;

namespace Spellbound.Core.Registries {
    /// <summary>
    /// A lookup of <typeparamref name="TEntry"/> by stable uint hash.
    /// </summary>
    public interface IRegistry<TEntry> where TEntry : class {
        bool TryGet(uint hash, out TEntry entry);
        TEntry Get(uint hash);
        bool Contains(uint hash);
        int Count { get; }
        IReadOnlyList<TEntry> All { get; }
    }
}
