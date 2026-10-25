// Copyright 2026 Spellbound Studio Inc.

using Spellbound.Core.ObjectData;
using Spellbound.Core.ObjectHandling;

namespace Spellbound.Core.ModuleContracts {
    /// <summary>
    /// For ObjectPreset PresetModules
    /// Interface Contract for a Module to respond to changes with cosmetic/aesthetic/audio
    /// Typically context will play a large role in the response, via switch statement or ifs/elses.
    /// </summary>
    public interface IChangeHandler<T>  where T : IPackerObjectData{
        void HandleChange(T data, byte context, ObjectParent parent, int instanceIndex, TransformData transformData);
    }
}