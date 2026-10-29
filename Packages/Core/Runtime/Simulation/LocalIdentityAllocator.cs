// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Simulation {
    public sealed class LocalIdentityAllocator : IIdentityAllocator {
        private readonly ILeaseIssuer _issuer;

        public int CurrentLease { get; private set; }

        public uint NextCounter { get; private set; }

        public bool CounterExhausted { get; private set; }

        public LocalIdentityAllocator(ILeaseIssuer issuer) {
            _issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
        }

        public LocalIdentityAllocator(ILeaseIssuer issuer, in LocalIdentityAllocatorState state) {
            _issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));

            if (!state.HoldsALease) {
                return;
            }

            CurrentLease = state.Lease;
            NextCounter = state.NextCounter;
            CounterExhausted = state.CounterExhausted;
        }

        public LocalIdentityAllocatorState State =>
            CurrentLease < 1
                ? default
                : new LocalIdentityAllocatorState(CurrentLease, NextCounter, CounterExhausted);

        public InstanceId Allocate() {
            if (CurrentLease == 0 || CounterExhausted) {
                CurrentLease = _issuer.Issue();
                NextCounter = 0;
                CounterExhausted = false;
            }

            var counter = NextCounter;
            var id = InstanceId.Compose(CurrentLease, counter);

            if (counter == uint.MaxValue) {
                CounterExhausted = true;
            }
            else {
                NextCounter = counter + 1;
            }

            return id;
        }
    }
}
