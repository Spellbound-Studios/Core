// Copyright 2026 Spellbound Studio Inc.

using Unity.Entities;
using Unity.Transforms;

namespace Spellbound.Core.ECS {
    public struct EntitySpawnRequestElement : IBufferElementData {
        public Entity Prefab;
        public LocalTransform Transform;
        public int InstanceIndex;
    }
}