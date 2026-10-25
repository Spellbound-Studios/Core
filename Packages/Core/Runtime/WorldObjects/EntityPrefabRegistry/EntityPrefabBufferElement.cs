// Copyright 2026 Spellbound Studio Inc.

using Unity.Entities;

namespace Spellbound.Core.EntityPrefabs {
    /// <summary>
    /// Buffer entry pairing a baked entity prefab with its preset's stable hash.
    /// </summary>
    public struct EntityPrefabBufferElement : IBufferElementData {
        public Entity Prefab;
        public uint Hash;
    }
}