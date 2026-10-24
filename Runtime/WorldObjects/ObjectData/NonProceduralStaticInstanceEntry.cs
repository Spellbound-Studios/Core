// Copyright 2026 Spellbound Studio Inc.

using System.Collections.Generic;

namespace Spellbound.Core.ObjectData {
    public class NonProceduralStaticInstanceEntry {
        public readonly uint PresetHash;
        public readonly Dictionary<InstanceDataKey, byte[]> DataSlots = new();
        public TransformData Transform;

        public NonProceduralStaticInstanceEntry(uint presetHash, TransformData transform) {
            PresetHash = presetHash;
            Transform = transform;
        }
    }
}