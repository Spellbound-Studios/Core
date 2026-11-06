// Copyright (c) Spellbound Studios. All rights reserved.

using NUnit.Framework;
using Spellbound.Core.ReferenceFrames;

namespace Spellbound.Core.Tests {
    public class ReferenceFrameIdTests {
        [Test]
        public void DefaultIdIsTheWorld() {
            Assert.IsTrue(default(ReferenceFrameId).IsWorld);
            Assert.AreEqual(ReferenceFrameId.World, default(ReferenceFrameId));
        }

        [Test]
        public void EqualValuesAreEqualAndHashEqual() {
            var first = new ReferenceFrameId(42);
            var second = new ReferenceFrameId(42);
            Assert.IsTrue(first == second);
            Assert.IsTrue(first.Equals(second));
            Assert.IsTrue(first.Equals((object)second));
            Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
        }

        [Test]
        public void DifferentValuesAreNotEqual() {
            Assert.IsTrue(new ReferenceFrameId(1) != new ReferenceFrameId(2));
        }

        [Test]
        public void WorldWritesAsWorld() {
            Assert.AreEqual("world", ReferenceFrameId.World.ToString());
        }

        [Test]
        public void NonWorldWritesItsValue() {
            Assert.AreEqual("42", new ReferenceFrameId(42).ToString());
        }
    }
}
