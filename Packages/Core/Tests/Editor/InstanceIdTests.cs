// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using NUnit.Framework;
using Spellbound.Core.Simulation;

namespace Spellbound.Core.Tests {
    public class InstanceIdTests {
        [Test]
        public void NoneIsZero() {
            Assert.AreEqual(0UL, InstanceId.None.Value);
            Assert.IsTrue(InstanceId.None.IsNone);
            Assert.AreEqual(InstanceId.None, default(InstanceId));
        }

        [Test]
        public void FirstComposedIdIsLeaseOneCounterZero() {
            var id = InstanceId.Compose(1, 0);
            Assert.IsFalse(id.IsNone);
            Assert.AreEqual(1, id.LeaseOf());
            Assert.AreEqual(0U, id.CounterOf());
            Assert.IsFalse(id.IsPlacement);
        }

        [Test]
        public void PlacementKeepsLeaseAndCounter() {
            var id = InstanceId.Compose(12, 345);
            var placement = id.AsPlacement();
            Assert.IsTrue(placement.IsPlacement);
            Assert.AreEqual(12, placement.LeaseOf());
            Assert.AreEqual(345U, placement.CounterOf());
            Assert.AreNotEqual(id, placement);
        }

        [Test]
        public void LeaseZeroThrows() {
            Assert.Throws<ArgumentOutOfRangeException>(() => InstanceId.Compose(0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => InstanceId.Compose(-1, 0));
        }

        [Test]
        public void LargestLeaseAndCounterAreKept() {
            var id = InstanceId.Compose(int.MaxValue, uint.MaxValue);
            Assert.AreEqual(int.MaxValue, id.LeaseOf());
            Assert.AreEqual(uint.MaxValue, id.CounterOf());
            Assert.IsFalse(id.IsPlacement);
        }

        [Test]
        public void EqualValuesAreEqualAndHashEqual() {
            var first = InstanceId.Compose(3, 4);
            var second = InstanceId.Compose(3, 4);
            Assert.IsTrue(first == second);
            Assert.IsTrue(first.Equals(second));
            Assert.IsTrue(first.Equals((object)second));
            Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
            Assert.IsTrue(first != InstanceId.Compose(3, 5));
        }

        [Test]
        public void NoneWritesAsNone() {
            Assert.AreEqual("none", InstanceId.None.ToString());
        }

        [Test]
        public void IdWritesItsValue() {
            Assert.AreEqual("4294967296", InstanceId.Compose(1, 0).ToString());
        }

        [Test]
        public void PlacementWritesItsKind() {
            Assert.AreEqual("placement 9223372041149743104", InstanceId.Compose(1, 0).AsPlacement().ToString());
        }
    }
}
