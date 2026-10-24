// Copyright 2026 Spellbound Studio Inc.

using System.Collections.Generic;

namespace Spellbound.Core.ObjectData {
    public class InstanceEntry {
        public readonly uint PresetHash;
        public readonly Dictionary<InstanceDataKey, byte[]> DataSlots;
        public TransformData? Transform;

        public InstanceEntry(uint presetHash) {
            PresetHash = presetHash;
            DataSlots = new Dictionary<InstanceDataKey, byte[]>();
            Transform = null;
        }
    }
}