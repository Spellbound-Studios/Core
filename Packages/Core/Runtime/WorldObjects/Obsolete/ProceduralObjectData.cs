// Copyright 2026 Spellbound Studio Inc.

using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Spellbound.Core {
    /// <summary>
    /// TODO: Obsolete - needs to conform to new transform data/ecs system.
    /// This struct represents a procedurally generated GoData.
    /// Its custom data must be inserted separately outside of jobs
    /// </summary>
    [Obsolete, Serializable]
    public struct ProceduralObjectData {
        public float3 position;
        public float3 rotation;
        public float3 scale;
        public Entity entityPrefab;

        public ProceduralObjectData(
            float3 pos,
            float3 rot,
            float3 sc,
            Entity prefab
        ) {
            position = pos;
            rotation = rot;
            scale = sc;
            entityPrefab = prefab;
        }

        public override string ToString() => $"GoData at: {position}, {rotation}, {scale}, {entityPrefab}";
    }
}