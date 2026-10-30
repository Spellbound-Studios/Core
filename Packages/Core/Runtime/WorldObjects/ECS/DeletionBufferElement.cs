// Copyright 2026 Spellbound Studio Inc.

using Unity.Entities;

namespace Spellbound.Core.ECS {
    /// <summary>
    /// DeletionBufferElement is an essential script for the ECS portion of Core. It exists as a means for registering
    /// an entity for deletion. ECS utilizes buffers so that inside of something like an ISystem, it can be handled
    /// and managed at a frame by frame level.
    /// Add entities to this buffer in order to schedule them for proper deletion.
    /// </summary>
    public struct DeletionBufferElement : IBufferElementData {
        public int Value;
    }
}