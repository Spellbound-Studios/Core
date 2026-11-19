// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Spellbound.Core.Intents;
using Spellbound.Core.Observations;
using Spellbound.Core.Packing;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Spellbound.Core.Tests {
    public class IntentKeyTests {
        private static readonly byte[] GoldenBytes = {
            0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01, 0x0D, 0x0C, 0x0B, 0x0A
        };

        private static readonly IntentKey GoldenKey = new IntentKey(new IntentActor(0x0102030405060708UL), 0x0A0B0C0Du);

        [Test]
        public void ActorNoneIsZero() {
            Assert.AreEqual(0UL, IntentActor.None.Value);
            Assert.AreEqual(IntentActor.None, default(IntentActor));
            Assert.AreEqual("none", IntentActor.None.ToString());
            Assert.AreEqual("42", new IntentActor(42).ToString());
        }

        [Test]
        public void ActorsCompareByValue() {
            Assert.AreEqual(new IntentActor(7), new IntentActor(7));
            Assert.IsTrue(new IntentActor(7) == new IntentActor(7));
            Assert.IsTrue(new IntentActor(7) != new IntentActor(8));
            Assert.AreEqual(new IntentActor(7).GetHashCode(), new IntentActor(7).GetHashCode());
        }

        [Test]
        public void KeyNoneIsTheDefault() {
            Assert.IsTrue(IntentKey.None.IsNone);
            Assert.AreEqual(IntentKey.None, default(IntentKey));
            Assert.IsFalse(new IntentKey(new IntentActor(1), 1).IsNone);
            Assert.IsFalse(new IntentKey(IntentActor.None, 1).IsNone);
        }

        [Test]
        public void KeysCompareByActorAndSequence() {
            var key = new IntentKey(new IntentActor(3), 9);
            Assert.AreEqual(key, new IntentKey(new IntentActor(3), 9));
            Assert.IsTrue(key == new IntentKey(new IntentActor(3), 9));
            Assert.IsTrue(key != new IntentKey(new IntentActor(3), 10));
            Assert.IsTrue(key != new IntentKey(new IntentActor(4), 9));
            Assert.AreEqual(key.GetHashCode(), new IntentKey(new IntentActor(3), 9).GetHashCode());
        }

        [Test]
        public void ReceiptNoneIsTheDefault() {
            Assert.AreEqual(IntentReceipt.None, default(IntentReceipt));
            Assert.IsTrue(IntentReceipt.None.Reason.IsNone);
            Assert.AreEqual(0u, IntentReceipt.None.Count);
            var granted = CoreKit.Observations.Reason("intentkeytests.granted");
            Assert.AreEqual(new IntentReceipt(granted, 2), new IntentReceipt(granted, 2));
            Assert.AreNotEqual(new IntentReceipt(granted, 2), new IntentReceipt(granted, 3));
            Assert.AreNotEqual(new IntentReceipt(granted, 2), new IntentReceipt(ObservationReason.None, 2));
        }

        [Test]
        public void KeyPacksToTheGoldenBytes() {
            var buffer = new byte[32];
            Assert.IsTrue(CoreKit.Packing.TryPack(GoldenKey, buffer, out var written));
            Assert.AreEqual(12, written);
            CollectionAssert.AreEqual(GoldenBytes, new ArraySegment<byte>(buffer, 0, written));
        }

        [Test]
        public void GoldenBytesUnpackToTheKey() {
            Assert.IsTrue(CoreKit.Packing.TryUnpack<IntentKey>(GoldenBytes, out var unpacked));
            Assert.AreEqual(GoldenKey, unpacked);

            ReadOnlySpan<byte> span = GoldenBytes;
            Assert.IsTrue(IntentKey.TryUnpack(ref span, out var read));
            Assert.AreEqual(GoldenKey, read);
            Assert.AreEqual(0, span.Length);
        }

        [Test]
        public void TryUnpackOnAShortSpanAnswersFalseAndLeavesTheSpan() {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(GoldenBytes, 0, 11);
            Assert.IsFalse(IntentKey.TryUnpack(ref span, out var key));
            Assert.AreEqual(IntentKey.None, key);
            Assert.AreEqual(11, span.Length);

            ReadOnlySpan<byte> empty = ReadOnlySpan<byte>.Empty;
            Assert.IsFalse(IntentKey.TryUnpack(ref empty, out _));
        }

        [Test]
        public void BitWriterWritesTheGoldenBytes() {
            var buffer = new byte[16];
            var writer = new BitWriter(buffer);
            GoldenKey.Write(ref writer);
            Assert.AreEqual(96, writer.BitsWritten);
            CollectionAssert.AreEqual(GoldenBytes, writer.Written.ToArray());

            var reader = new BitReader(GoldenBytes);
            Assert.IsTrue(IntentKey.TryRead(ref reader, out var read));
            Assert.AreEqual(GoldenKey, read);
            Assert.AreEqual(96, reader.BitsRead);
        }

        [Test]
        public void KeyFollowsOtherBitsInACommand() {
            var buffer = new byte[16];
            var writer = new BitWriter(buffer);
            writer.WriteBits(5u, 3);
            GoldenKey.Write(ref writer);
            writer.WriteBool(true);

            var reader = new BitReader(writer.Written);
            Assert.IsTrue(reader.TryReadBits(3, out var prefix));
            Assert.AreEqual(5u, prefix);
            Assert.IsTrue(IntentKey.TryRead(ref reader, out var read));
            Assert.AreEqual(GoldenKey, read);
            Assert.IsTrue(reader.TryReadBool(out var flag));
            Assert.IsTrue(flag);
        }

        [Test]
        public void TryReadOnAShortBitSpanAnswersFalseAndReadsNothing() {
            var reader = new BitReader(new ReadOnlySpan<byte>(GoldenBytes, 0, 11));
            Assert.IsFalse(IntentKey.TryRead(ref reader, out var key));
            Assert.AreEqual(IntentKey.None, key);
            Assert.AreEqual(0, reader.BitsRead);
        }

        [Test]
        public void WriteWithoutRoomThrows() {
            var buffer = new byte[11];
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                var writer = new BitWriter(buffer);
                GoldenKey.Write(ref writer);
            });
        }

        [Test]
        public void Pack_NoAllocation() {
            var buffer = new byte[32];
            var bits = new byte[16];
            TestDelegate action = () => {
                CoreKit.Packing.TryPack(GoldenKey, buffer, out var written);
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(buffer, 0, written);
                IntentKey.TryUnpack(ref span, out _);
                CoreKit.Packing.TryUnpack<IntentKey>(new ReadOnlySpan<byte>(buffer, 0, written), out _);
                var writer = new BitWriter(bits);
                GoldenKey.Write(ref writer);
                var reader = new BitReader(writer.Written);
                IntentKey.TryRead(ref reader, out _);
            };

            action();
            var constraint = ((IResolveConstraint)Is.Not.AllocatingGCMemory()).Resolve();
            constraint.ApplyTo(action);
            Assert.That(action, constraint);
        }
    }
}
