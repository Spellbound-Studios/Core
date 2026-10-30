// Copyright 2026 Spellbound Studio Inc.

using Unity.Entities;
using UnityEngine;

namespace Spellbound.Core.ObjectHandling {
    /// <summary>
    /// Contract for registering and unregistering network governed objects.
    /// </summary>
    public interface IObjectParent {
        public ObjectParent ObjectParent { get; }
        public Vector3Int ChunkCoord { get; }
        void InitializeObjectParentChunk(Vector3Int id, Entity ecsChunk);
    }
}