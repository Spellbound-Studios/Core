// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Simulation {
    public sealed class LocalIdentityAllocator : IIdentityAllocator {
        private readonly ILeaseIssuer _issuer;
        private bool _counterExhausted;

        public int CurrentLease { get; private set; }

        public uint NextCounter { get; private set; }

        public LocalIdentityAllocator(ILeaseIssuer issuer) {
            _issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
        }

        public LocalIdentityAllocator(ILeaseIssuer issuer, int lease, uint nextCounter) {
            _issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));

            if (lease < 1) {
                throw new ArgumentOutOfRangeException(nameof(lease), lease, "A lease is at least 1.");
            }

            CurrentLease = lease;
            NextCounter = nextCounter;
        }

        public InstanceId Allocate() {
            if (CurrentLease == 0 || _counterExhausted) {
                CurrentLease = _issuer.Issue();
                NextCounter = 0;
                _counterExhausted = false;
            }

            var counter = NextCounter;
            var id = InstanceId.Compose(CurrentLease, counter);

            if (counter == uint.MaxValue) {
                _counterExhausted = true;
            }
            else {
                NextCounter = counter + 1;
            }

            return id;
        }
    }
}
