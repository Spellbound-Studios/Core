// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Globalization;

namespace Spellbound.Core.Viewers {
    public readonly struct ViewerId : IEquatable<ViewerId> {
        public readonly uint Value;

        public ViewerId(uint value) {
            Value = value;
        }

        public static ViewerId None => default;

        public bool IsNone => Value == 0;

        public bool Equals(ViewerId other) => Value == other.Value;

        public override bool Equals(object obj) => obj is ViewerId other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        public static bool operator ==(ViewerId left, ViewerId right) => left.Equals(right);

        public static bool operator !=(ViewerId left, ViewerId right) => !left.Equals(right);

        public override string ToString() => IsNone ? "none" : Value.ToString(CultureInfo.InvariantCulture);
    }
}
