// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Globalization;

namespace Spellbound.Core.Simulation {
    public readonly struct Tick : IEquatable<Tick>, IComparable<Tick> {
        public readonly ulong Value;

        public Tick(ulong value) {
            Value = value;
        }

        public static Tick Zero => default;

        public Tick Plus(TickSpan span) => new Tick(Value + span.Steps);

        public TickSpan Until(Tick later) {
            if (later.Value < Value) {
                throw new ArgumentException(
                    "Tick " + later + " is earlier than tick " + this + ".", nameof(later));
            }

            return new TickSpan(later.Value - Value);
        }

        public bool Equals(Tick other) => Value == other.Value;

        public override bool Equals(object obj) => obj is Tick other && Equals(other);

        public int CompareTo(Tick other) => Value.CompareTo(other.Value);

        public override int GetHashCode() => Value.GetHashCode();

        public static bool operator ==(Tick left, Tick right) => left.Value == right.Value;

        public static bool operator !=(Tick left, Tick right) => left.Value != right.Value;

        public static bool operator <(Tick left, Tick right) => left.Value < right.Value;

        public static bool operator >(Tick left, Tick right) => left.Value > right.Value;

        public static bool operator <=(Tick left, Tick right) => left.Value <= right.Value;

        public static bool operator >=(Tick left, Tick right) => left.Value >= right.Value;

        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
    }
}
