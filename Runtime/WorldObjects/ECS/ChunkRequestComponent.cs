// Copyright 2026 Spellbound Studio Inc.

using Unity.Entities;
using Unity.Mathematics;

namespace Spellbound.Core.ECS {
    public struct ChunkRequestComponent : IComponentData {
        public int3 ChunkCoord;
    }
}