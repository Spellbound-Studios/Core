// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using NUnit.Framework;
using Spellbound.Core.Intents;

namespace Spellbound.Core.Tests {
    public class IntentSequenceTests {
        private static readonly IntentActor Actor = new IntentActor(11);

        [Test]
        public void TheFirstKeyIsSequenceOne() {
            var sequence = CoreKit.Intents.Sequence(Actor);
            Assert.AreEqual(Actor, sequence.Actor);
            Assert.AreEqual(1u, sequence.NextSequence);
            Assert.AreEqual(new IntentKey(Actor, 1), sequence.Next());
            Assert.AreEqual(2u, sequence.NextSequence);
        }

        [Test]
        public void KeysAreIssuedInOrder() {
            var sequence = new IntentSequence(Actor);

            for (uint expected = 1; expected <= 100; expected++) {
                var key = sequence.Next();
                Assert.AreEqual(Actor, key.Actor);
                Assert.AreEqual(expected, key.Sequence);
            }
        }

        [Test]
        public void ASequenceStartsWhereItIsTold() {
            var sequence = new IntentSequence(Actor, 500);
            Assert.AreEqual(500u, sequence.NextSequence);
            Assert.AreEqual(500u, sequence.Next().Sequence);
            Assert.AreEqual(501u, sequence.Next().Sequence);
        }

        [Test]
        public void ASequenceNeedsAnActor() {
            Assert.Throws<ArgumentException>(() => new IntentSequence(IntentActor.None));
            Assert.Throws<ArgumentException>(() => new IntentSequence(IntentActor.None, 5));
        }

        [Test]
        public void SequenceZeroIsNeverIssued() {
            Assert.Throws<ArgumentOutOfRangeException>(() => new IntentSequence(Actor, 0));
        }

        [Test]
        public void ASequenceAtTheLastValueThrowsRatherThanWrapping() {
            var sequence = new IntentSequence(Actor, uint.MaxValue - 1);
            Assert.AreEqual(uint.MaxValue - 1, sequence.Next().Sequence);
            Assert.Throws<InvalidOperationException>(() => sequence.Next());
            Assert.AreEqual(uint.MaxValue, sequence.NextSequence);
            Assert.Throws<InvalidOperationException>(() => sequence.Next());
        }
    }
}
