// Copyright 2026 Spellbound Studio Inc.

using System.Collections.Generic;

namespace Spellbound.Core.ObjectData {
    public class ProceduralStaticInstanceEntry {
        public readonly Dictionary<InstanceDataKey, byte[]> DataSlots = new();
    }
}