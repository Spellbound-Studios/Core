// Copyright 2026 Spellbound Studio Inc.

using Unity.Entities;
using Unity.Mathematics;

namespace Spellbound.Core.ECS {
    /// <summary>
    /// This is intended to be a unified component for proximity jobs.
    /// </summary>
    public struct ProximityThresholdComponent : IComponentData {
        public float2 Value;
    }
}