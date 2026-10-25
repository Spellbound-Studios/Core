// Copyright 2026 Spellbound Studio Inc.

using System;
using Spellbound.Core.Objects;

namespace Spellbound.Core.Modules {
    /// <summary>
    /// TODO: 
    /// </summary>
    [Serializable]
    public abstract class PresetModule {
        [NonSerialized] private ObjectPreset _preset;
        [NonSerialized] private byte _surfaceIndex;

        protected ObjectPreset Preset => _preset;
        protected byte SurfaceIndex => _surfaceIndex;

        /// <summary>
        /// Called when the owning ObjectPreset is loaded into memory.
        /// Caches the preset and surface index so modules don't need them passed per-call.
        /// Override to do additional setup — always call base first.
        /// </summary>
        public virtual void OnPresetLoaded(ObjectPreset preset, byte surfaceIndex) {
            _preset = preset;
            _surfaceIndex = surfaceIndex;
        }
    }
}