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
            var id = allocator.Allocate();
            Assert.IsFalse(id.IsNone);
            Assert.AreEqual(1, id.LeaseOf());
            Assert.AreEqual(0U, id.CounterOf());
            Assert.AreEqual(1, allocator.CurrentLease);
            Assert.AreEqual(1U, allocator.NextCounter);
        }

        [Test]
        public void AllocationsAreDistinctAndNeverNone() {
            var allocator = new LocalIdentityAllocator(new LocalLeaseIssuer());
            var seen = new HashSet<InstanceId>();
            for (var allocation = 0; allocation < 1000; allocation++) {
                var id = allocator.Allocate();
                Assert.IsFalse(id.IsNone);
                Assert.IsTrue(seen.Add(id), id.ToString());
            }
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
            var allocator = new LocalIdentityAllocator(new LocalLeaseIssuer(2), 1, uint.MaxValue - 1);
            var first = allocator.Allocate();
            var second = allocator.Allocate();
            var third = allocator.Allocate();
            Assert.AreEqual(1, first.LeaseOf());
            Assert.AreEqual(uint.MaxValue - 1, first.CounterOf());
            Assert.AreEqual(1, second.LeaseOf());
            Assert.AreEqual(uint.MaxValue, second.CounterOf());
            Assert.AreEqual(2, third.LeaseOf());
            Assert.AreEqual(0U, third.CounterOf());
            Assert.AreEqual(2, allocator.CurrentLease);
        }

        [Test]
        public void ResumingBelowLeaseOneThrows() {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new LocalIdentityAllocator(new LocalLeaseIssuer(), 0, 0));
        }

        [Test]
        public void MissingIssuerThrows() {
            Assert.Throws<ArgumentNullException>(() => new LocalIdentityAllocator(null));
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
        public void LeaseIssuerRefusesToPassTheLargestLease() {
            var issuer = new LocalLeaseIssuer(int.MaxValue - 1);
            Assert.AreEqual(int.MaxValue - 1, issuer.Issue());
            Assert.Throws<InvalidOperationException>(() => issuer.Issue());
        }

        [Test]
        public void IIdentityAllocator_CustomImplementation() {
            IIdentityAllocator allocator = new SequenceIdentityAllocator();
            Assert.AreEqual(new InstanceId(100), allocator.Allocate());
            Assert.AreEqual(new InstanceId(101), allocator.Allocate());
        }
    }
}
