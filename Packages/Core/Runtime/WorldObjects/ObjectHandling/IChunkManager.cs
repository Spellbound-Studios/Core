// Copyright 2026 Spellbound Studio Inc.

using UnityEngine;

namespace Spellbound.Core.ObjectHandling {
    /// <summary>
    /// IChunkManager is the responsible interface for providing a contractual binding back to a managers role to
    /// objects.
    /// </summary>
    public interface IChunkManager {
        public bool TryGetObjectParentChunk(Vector3 position, out IObjectParent chunk);
    }
}