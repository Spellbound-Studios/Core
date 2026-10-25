// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Globalization;

namespace Spellbound.Core.ReferenceFrames {
    public readonly struct ReferenceFrameId : IEquatable<ReferenceFrameId> {
        public readonly ulong Value;

        public ReferenceFrameId(ulong value) {
            Value = value;
        }

        public static ReferenceFrameId World => default;

        public bool IsWorld => Value == 0;

        public bool Equals(ReferenceFrameId other) => Value == other.Value;

        public override bool Equals(object obj) => obj is ReferenceFrameId other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        public static bool operator ==(ReferenceFrameId left, ReferenceFrameId right) => left.Equals(right);

        public static bool operator !=(ReferenceFrameId left, ReferenceFrameId right) => !left.Equals(right);

        public override string ToString() => IsWorld ? "world" : Value.ToString(CultureInfo.InvariantCulture);
    }
}
