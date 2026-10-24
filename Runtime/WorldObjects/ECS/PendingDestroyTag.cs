// Copyright 2026 Spellbound Studio Inc.

using Unity.Entities;

namespace Spellbound.Core.ECS {
    /// <summary>
    /// Tag for Entities to be destroyed
    /// </summary>
    public struct PendingDestroyTag : IComponentData { }
}