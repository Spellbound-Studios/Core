// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Spellbound.Core.Simulation;

namespace Spellbound.Core.Tests {
    public class LocalIdentityAllocatorTests {
        private sealed class SteppingLeaseIssuer : ILeaseIssuer {
            private int _next = 5;

            public int Issue() {
                var lease = _next;
                _next += 4;
                return lease;
            }
        }

        private sealed class SequenceIdentityAllocator : IIdentityAllocator {
            private ulong _next = 100;

            public InstanceId Allocate() {
                return new InstanceId(_next++);
            }
        }

        [Test]
        public void FirstIdIsLeaseOneCounterZero() {
            var allocator = new LocalIdentityAllocator(new LocalLeaseIssuer());
            Assert.AreEqual(0, allocator.CurrentLease);
            Assert.AreEqual(default(LocalIdentityAllocatorState), allocator.State);
            var id = allocator.Allocate();
            Assert.IsFalse(id.IsNone);
            Assert.AreEqual(1, id.LeaseOf());
            Assert.AreEqual(0U, id.CounterOf());
            Assert.AreEqual(1, allocator.CurrentLease);
            Assert.AreEqual(1U, allocator.NextCounter);
            Assert.AreEqual(new LocalIdentityAllocatorState(1, 1, false), allocator.State);
        }

        [Test]
        public void AllocationsAreDistinctAndNeverNone() {
            AssertTheAllocationContract(new LocalIdentityAllocator(new LocalLeaseIssuer()));
        }

        [Test]
        public void ILeaseIssuer_CustomImplementation() {
            var issuer = new SteppingLeaseIssuer();
            var first = new LocalIdentityAllocator(issuer).Allocate();
            var second = new LocalIdentityAllocator(issuer).Allocate();
            var third = new LocalIdentityAllocator(issuer).Allocate();
            Assert.AreEqual(5, first.LeaseOf());
            Assert.AreEqual(9, second.LeaseOf());
            Assert.AreEqual(13, third.LeaseOf());
        }

        [Test]
        public void FullCounterTakesTheNextLease() {
            var allocator = new LocalIdentityAllocator(new LocalLeaseIssuer(2),
                new LocalIdentityAllocatorState(1, uint.MaxValue - 1, false));
            var first = allocator.Allocate();
            var second = allocator.Allocate();
            Assert.IsTrue(allocator.CounterExhausted);
            Assert.AreEqual(new LocalIdentityAllocatorState(1, uint.MaxValue, true), allocator.State);
            var third = allocator.Allocate();
            Assert.AreEqual(1, first.LeaseOf());
            Assert.AreEqual(uint.MaxValue - 1, first.CounterOf());
            Assert.AreEqual(1, second.LeaseOf());
            Assert.AreEqual(uint.MaxValue, second.CounterOf());
            Assert.AreEqual(2, third.LeaseOf());
            Assert.AreEqual(0U, third.CounterOf());
            Assert.AreEqual(2, allocator.CurrentLease);
            Assert.IsFalse(allocator.CounterExhausted);
        }

        [Test]
        public void AResumedAllocatorNeverRepeatsAnId() {
            var issued = new HashSet<InstanceId>();
            var allocator = new LocalIdentityAllocator(new LocalLeaseIssuer(2),
                new LocalIdentityAllocatorState(1, uint.MaxValue - 2, false));

            for (var allocation = 0; allocation < 3; allocation++) {
                Assert.IsTrue(issued.Add(allocator.Allocate()));
            }

            var saved = allocator.State;
            Assert.IsTrue(saved.CounterExhausted);
            Assert.AreEqual(uint.MaxValue, saved.NextCounter);
            var resumed = new LocalIdentityAllocator(new LocalLeaseIssuer(2), saved);
            Assert.AreEqual(saved, resumed.State);

            for (var allocation = 0; allocation < 3; allocation++) {
                var id = resumed.Allocate();
                Assert.IsTrue(issued.Add(id), id.ToString());
                Assert.AreEqual(2, id.LeaseOf());
            }
        }

        [Test]
        public void AResumedAllocatorContinuesItsCounter() {
            var allocator = new LocalIdentityAllocator(new LocalLeaseIssuer());
            var first = allocator.Allocate();
            var resumed = new LocalIdentityAllocator(new LocalLeaseIssuer(), allocator.State);
            var second = resumed.Allocate();
            Assert.AreEqual(first.LeaseOf(), second.LeaseOf());
            Assert.AreEqual(first.CounterOf() + 1, second.CounterOf());
        }

        [Test]
        public void ADefaultStateResumesAsAFreshAllocator() {
            var allocator = new LocalIdentityAllocator(new LocalLeaseIssuer(3), default);
            Assert.AreEqual(0, allocator.CurrentLease);
            Assert.AreEqual(3, allocator.Allocate().LeaseOf());
        }

        [Test]
        public void AStateBelowLeaseOneThrows() {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LocalIdentityAllocatorState(0, 0, false));
        }

        [Test]
        public void MissingIssuerThrows() {
            Assert.Throws<ArgumentNullException>(() => new LocalIdentityAllocator(null));
            Assert.Throws<ArgumentNullException>(() => new LocalIdentityAllocator(null, default));
        }

        [Test]
        public void LeaseIssuerStartsAtOneAndMoves() {
            var issuer = new LocalLeaseIssuer();
            Assert.AreEqual(1, issuer.NextLease);
            Assert.AreEqual(1, issuer.Issue());
            Assert.AreEqual(2, issuer.NextLease);
            Assert.AreEqual(2, issuer.Issue());
            Assert.AreEqual(3, issuer.NextLease);
        }

        [Test]
        public void LeaseIssuerResumesAtItsSavedLease() {
            var issuer = new LocalLeaseIssuer(40);
            Assert.AreEqual(40, issuer.Issue());
            Assert.AreEqual(41, issuer.NextLease);
        }

        [Test]
        public void LeaseIssuerBelowOneThrows() {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LocalLeaseIssuer(0));
        }

        [Test]
        public void LeaseIssuerIssuesTheLargestLeaseThenRefuses() {
            var issuer = new LocalLeaseIssuer(int.MaxValue - 1);
            Assert.AreEqual(int.MaxValue - 1, issuer.Issue());
            Assert.IsFalse(issuer.Exhausted);
            Assert.AreEqual(int.MaxValue, issuer.Issue());
            Assert.IsTrue(issuer.Exhausted);
            Assert.Throws<InvalidOperationException>(() => issuer.Issue());
            Assert.Throws<InvalidOperationException>(() => new LocalLeaseIssuer(int.MaxValue, true).Issue());
        }

        [Test]
        public void IIdentityAllocator_CustomImplementation() {
            IIdentityAllocator custom = new SequenceIdentityAllocator();
            Assert.AreEqual(new InstanceId(100), custom.Allocate());
            AssertTheAllocationContract(custom);
            AssertTheAllocationContract(new LocalIdentityAllocator(new SteppingLeaseIssuer()));
        }

        private static void AssertTheAllocationContract(IIdentityAllocator allocator) {
            var seen = new HashSet<InstanceId>();

            for (var allocation = 0; allocation < 1000; allocation++) {
                var id = allocator.Allocate();
                Assert.IsFalse(id.IsNone);
                Assert.IsFalse(id.IsPlacement);
                Assert.IsTrue(seen.Add(id), id.ToString());
            }
        }
    }
}
