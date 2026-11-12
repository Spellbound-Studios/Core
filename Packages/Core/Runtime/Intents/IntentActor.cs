// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Globalization;

namespace Spellbound.Core.Intents {
    public readonly struct IntentActor : IEquatable<IntentActor> {
        public readonly ulong Value;

        public IntentActor(ulong value) {
            Value = value;
        }

        public static IntentActor None => default;

        public bool Equals(IntentActor other) => Value == other.Value;

        public override bool Equals(object obj) => obj is IntentActor other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        public static bool operator ==(IntentActor left, IntentActor right) => left.Value == right.Value;

        public static bool operator !=(IntentActor left, IntentActor right) => left.Value != right.Value;

        public override string ToString() => Value == 0 ? "none" : Value.ToString(CultureInfo.InvariantCulture);
    }
}
