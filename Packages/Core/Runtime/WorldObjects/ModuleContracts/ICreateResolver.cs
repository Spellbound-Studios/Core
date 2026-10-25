// Copyright 2026 Spellbound Studio Inc.

using Spellbound.Core.ObjectHandling;

namespace Spellbound.Core.ModuleContracts {
    public interface ICreateResolver {
        /// <summary>
        /// For ObjectPreset PresetModules
        /// Interface Contract for a Module to respond to creations with major consequences
        /// </summary>
        void ResolveCreate(ObjectParent parent, int instanceIndex, TransformData transformData);
    }
}