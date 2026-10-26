// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Globalization;

namespace Spellbound.Core.Simulation {
    public readonly struct TickSpan : IEquatable<TickSpan>, IComparable<TickSpan> {
        public readonly ulong Steps;

        public TickSpan(ulong steps) {
            Steps = steps;
        }

        public static TickSpan Zero => default;

        public TickSpan Plus(TickSpan other) => new TickSpan(Steps + other.Steps);

        public bool Equals(TickSpan other) => Steps == other.Steps;

        public override bool Equals(object obj) => obj is TickSpan other && Equals(other);

        public int CompareTo(TickSpan other) => Steps.CompareTo(other.Steps);

        public override int GetHashCode() => Steps.GetHashCode();

        public static bool operator ==(TickSpan left, TickSpan right) => left.Steps == right.Steps;

        public static bool operator !=(TickSpan left, TickSpan right) => left.Steps != right.Steps;

        public static bool operator <(TickSpan left, TickSpan right) => left.Steps < right.Steps;

        public static bool operator >(TickSpan left, TickSpan right) => left.Steps > right.Steps;

        public override string ToString() => Steps.ToString(CultureInfo.InvariantCulture);
    }
}
