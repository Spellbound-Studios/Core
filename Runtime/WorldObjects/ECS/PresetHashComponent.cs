// Copyright 2026 Spellbound Studio Inc.

using System;
using Unity.Entities;

namespace Spellbound.Core.ECS {
    /// <summary>
    /// The preset identity carried by every baked entity, as the preset's stable hash.
    /// </summary>
    public struct PresetHashComponent : ISharedComponentData, IEquatable<PresetHashComponent> {
        public uint Value;

        public bool Equals(PresetHashComponent other) => Value == other.Value;

        public override bool Equals(object obj) => obj is PresetHashComponent other && Equals(other);

        public override int GetHashCode() => (int)Value;
    }
}
