// Copyright 2026 Spellbound Studio Inc.

using Spellbound.Core.ObjectHandling;

namespace Spellbound.Core.ModuleContracts {
    public interface IDeleteHandler {
        /// <summary>
        /// For ObjectPreset PresetModules
        /// Interface Contract for a Module to respond to deletes with cosmetic/aesthetic/audio
        /// </summary>
        void HandleDelete(ObjectParent parent, int instanceIndex, TransformData transformData);
    }
}