// Copyright 2026 Spellbound Studio Inc.

using Unity.Entities;
using Unity.Mathematics;

namespace Spellbound.Core.ECS {
    /// <summary>
    /// Component to label what geoChunk an entity belongs.
    /// So tagging them for deletion can be quick via SharedComponent Query
    /// </summary>
    public struct ChunkParentComponent : ISharedComponentData {
        public int3 ChunkCoord;
    }
}