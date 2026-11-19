// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Simulation {
    public readonly struct LocalIdentityAllocatorState : IEquatable<LocalIdentityAllocatorState> {
        public readonly int Lease;
        public readonly uint NextCounter;
        public readonly bool CounterExhausted;

        public LocalIdentityAllocatorState(int lease, uint nextCounter, bool counterExhausted) {
            if (lease < 1) {
                throw new ArgumentOutOfRangeException(nameof(lease), lease, "A lease is at least 1.");
            }

            Lease = lease;
            NextCounter = nextCounter;
            CounterExhausted = counterExhausted;
        }

        public bool HoldsALease => Lease >= 1;

        public bool Equals(LocalIdentityAllocatorState other) =>
            Lease == other.Lease && NextCounter == other.NextCounter && CounterExhausted == other.CounterExhausted;

        public override bool Equals(object obj) => obj is LocalIdentityAllocatorState other && Equals(other);

        public override int GetHashCode() {
            unchecked {
                return (Lease * 397 ^ (int)NextCounter) * 397 ^ (CounterExhausted ? 1 : 0);
            }
        }

        public static bool operator ==(LocalIdentityAllocatorState left, LocalIdentityAllocatorState right) =>
            left.Equals(right);

        public static bool operator !=(LocalIdentityAllocatorState left, LocalIdentityAllocatorState right) =>
            !left.Equals(right);
    }
}
