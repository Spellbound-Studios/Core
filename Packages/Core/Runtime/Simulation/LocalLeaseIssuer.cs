// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Simulation {
    public sealed class LocalLeaseIssuer : ILeaseIssuer {
        public int NextLease { get; private set; }

        public LocalLeaseIssuer(int nextLease = 1) {
            if (nextLease < 1) {
                throw new ArgumentOutOfRangeException(nameof(nextLease), nextLease, "The first lease is at least 1.");
            }

            NextLease = nextLease;
        }

        public int Issue() {
            if (NextLease == int.MaxValue) {
                throw new InvalidOperationException("Every lease up to " + int.MaxValue + " has been issued.");
            }

            var lease = NextLease;
            NextLease = lease + 1;
            return lease;
        }
    }
}
