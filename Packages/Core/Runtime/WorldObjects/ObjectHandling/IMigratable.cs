// Copyright 2026 Spellbound Studio Inc.

using Spellbound.Core.Surfaces;
using UnityEngine;

namespace Spellbound.Core.ObjectHandling {
    public interface IMigratable : IEventSurface {
        int InstanceIndex { get; }
        Vector3Int CurrentChunk { get; }
        Vector3Int DesiredChunk { get; }
        bool IsSeedDerived { get; }
        void HandleLimbo();
    }
}