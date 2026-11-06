// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using NUnit.Framework;
using Spellbound.Core.Simulation;

namespace Spellbound.Core.Tests {
    public class TickTests {
        [Test]
        public void ZeroIsTheDefaultTick() {
            Assert.AreEqual(Tick.Zero, default(Tick));
            Assert.AreEqual(0UL, Tick.Zero.Value);
        }

        [Test]
        public void TicksCompareByValue() {
            var earlier = new Tick(3);
            var later = new Tick(7);
            Assert.IsTrue(earlier < later);
            Assert.IsTrue(later > earlier);
            Assert.IsTrue(earlier <= later);
            Assert.IsTrue(later >= earlier);
            Assert.IsTrue(earlier <= new Tick(3));
            Assert.IsTrue(earlier >= new Tick(3));
            Assert.Less(earlier.CompareTo(later), 0);
            Assert.Greater(later.CompareTo(earlier), 0);
            Assert.AreEqual(0, earlier.CompareTo(new Tick(3)));
        }

        [Test]
        public void EqualTicksAreEqualAndHashEqual() {
            var first = new Tick(42);
            var second = new Tick(42);
            Assert.IsTrue(first == second);
            Assert.IsFalse(first != second);
            Assert.IsTrue(first.Equals(second));
            Assert.IsTrue(first.Equals((object)second));
            Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
            Assert.IsTrue(new Tick(1) != new Tick(2));
        }

        [Test]
        public void PlusMovesTheTickForwardBySpan() {
            Assert.AreEqual(new Tick(15), new Tick(10).Plus(new TickSpan(5)));
            Assert.AreEqual(new Tick(10), new Tick(10).Plus(TickSpan.Zero));
        }

        [Test]
        public void UntilMeasuresTheSpanToALaterTick() {
            Assert.AreEqual(new TickSpan(5), new Tick(10).Until(new Tick(15)));
            Assert.AreEqual(TickSpan.Zero, new Tick(10).Until(new Tick(10)));
        }

        [Test]
        public void UntilAnEarlierTickThrows() {
            Assert.Throws<ArgumentException>(() => new Tick(10).Until(new Tick(9)));
        }

        [Test]
        public void TickWritesItsValue() {
            Assert.AreEqual("12000", new Tick(12000).ToString());
            Assert.AreEqual("0", Tick.Zero.ToString());
        }
    }
}
