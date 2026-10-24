// Copyright 2026 Spellbound Studio Inc.

using Spellbound.Core.ObjectHandling;

namespace Spellbound.Core.ModuleContracts {
    public interface ICreateHandler {
        /// <summary>
        /// For ObjectPreset PresetModules
        /// Interface Contract for a Module to respond to creations with cosmetic/aesthetic/audio
        /// </summary>
        void HandleCreate(ObjectParent parent, int instanceIndex, TransformData transformData);
    }
}