// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Globalization;

namespace Spellbound.Core.Simulation {
    public readonly struct InstanceId : IEquatable<InstanceId> {
        private const ulong PlacementBit = 1UL << 63;
        private const ulong LeaseMask = 0x7FFFFFFFUL;

        public readonly ulong Value;

        public InstanceId(ulong value) {
            Value = value;
        }

        public static InstanceId None => default;

        public bool IsNone => Value == 0;

        public bool IsPlacement => (Value & PlacementBit) != 0;

        public int LeaseOf() {
            return (int)((Value >> 32) & LeaseMask);
        }

        public uint CounterOf() {
            return (uint)Value;
        }

        public InstanceId AsPlacement() {
            return new InstanceId(Value | PlacementBit);
        }

        public static InstanceId Compose(int lease, uint counter) {
            if (lease < 1) {
                throw new ArgumentOutOfRangeException(nameof(lease), lease,
                    "A lease is between 1 and " + int.MaxValue + ".");
            }

            return new InstanceId(((ulong)lease << 32) | counter);
        }

        public bool Equals(InstanceId other) => Value == other.Value;

        public override bool Equals(object obj) => obj is InstanceId other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        public static bool operator ==(InstanceId left, InstanceId right) => left.Equals(right);

        public static bool operator !=(InstanceId left, InstanceId right) => !left.Equals(right);

        public override string ToString() {
            if (IsNone) {
                return "none";
            }

            var value = Value.ToString(CultureInfo.InvariantCulture);
            return IsPlacement ? "placement " + value : value;
        }
    }
}
