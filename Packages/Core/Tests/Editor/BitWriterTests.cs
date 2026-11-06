// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Spellbound.Core.Packing;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Spellbound.Core.Tests {
    public class BitWriterTests {
        [Test]
        public void BitsGoLeastSignificantFirstIntoLittleEndianBytes() {
            var buffer = new byte[4];
            var writer = new BitWriter(buffer);

            writer.WriteBits(0b101u, 3);
            writer.WriteBits(0b11u, 2);
            writer.WriteBits(0x1FFu, 9);

            Assert.AreEqual(14, writer.BitsWritten);
            Assert.AreEqual(2, writer.BytesWritten);
            Assert.AreEqual(new byte[] { 0xFD, 0x3F, 0x00, 0x00 }, buffer);
            Assert.AreEqual(2, writer.Written.Length);
        }

        [Test]
        public void SixtyFourBitsSplitAcrossTwoWords() {
            var buffer = new byte[16];
            var writer = new BitWriter(buffer);

            writer.WriteBits(1u, 1);
            writer.WriteLongBits(0x0123456789ABCDEFUL, 64);

            Assert.AreEqual(65, writer.BitsWritten);

            var reader = new BitReader(buffer);

            Assert.IsTrue(reader.TryReadBits(1, out var one));
            Assert.AreEqual(1u, one);
            Assert.IsTrue(reader.TryReadLongBits(64, out var value));
            Assert.AreEqual(0x0123456789ABCDEFUL, value);
        }

        [Test]
        public void EveryBitCountRoundTripsThroughTheReader() {
            var buffer = new byte[128];

            for (var bitCount = 1; bitCount <= 32; bitCount++) {
                var writer = new BitWriter(buffer);
                var mask = bitCount == 32 ? uint.MaxValue : (1u << bitCount) - 1u;
                var value = 0xA5A5A5A5u & mask;
                writer.WriteBits(0b1u, 1);
                writer.WriteBits(value, bitCount);
                writer.WriteBits(mask, bitCount);

                var reader = new BitReader(writer.Written);

                Assert.IsTrue(reader.TryReadBits(1, out var lead));
                Assert.AreEqual(1u, lead);
                Assert.IsTrue(reader.TryReadBits(bitCount, out var first));
                Assert.AreEqual(value, first, "bit count " + bitCount);
                Assert.IsTrue(reader.TryReadBits(bitCount, out var second));
                Assert.AreEqual(mask, second, "bit count " + bitCount);
            }
        }

        [Test]
        public void WritingPastTheEndIsRefusedAndWritesNothing() {
            var buffer = new byte[1];
            var writer = new BitWriter(buffer);

            Assert.IsTrue(writer.TryWriteBits(0x7Fu, 7));
            Assert.IsFalse(writer.TryWriteBits(0x3u, 2));
            Assert.AreEqual(7, writer.BitsWritten);
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                var failing = new BitWriter(new byte[1]);
                failing.WriteBits(0u, 9);
            });
        }

        [Test]
        public void ReadingPastTheEndIsRefusedAndMovesNothing() {
            var reader = new BitReader(new byte[] { 0xFF });

            Assert.IsTrue(reader.TryReadBits(6, out _));
            Assert.IsFalse(reader.TryReadBits(3, out _));
            Assert.AreEqual(6, reader.BitsRead);
            Assert.IsFalse(reader.TryReadVarUInt(out _));
            Assert.AreEqual(6, reader.BitsRead);
        }

        [Test]
        public void VarIntsWriteSevenBitsPerByteWithTheHighBitAsContinuation() {
            AssertVarUInt(0u, "00");
            AssertVarUInt(1u, "01");
            AssertVarUInt(127u, "7f");
            AssertVarUInt(128u, "80 01");
            AssertVarUInt(300u, "ac 02");
            AssertVarUInt(16383u, "ff 7f");
            AssertVarUInt(16384u, "80 80 01");
            AssertVarUInt(uint.MaxValue, "ff ff ff ff 0f");
        }

        [Test]
        public void LongVarIntsReachTenBytes() {
            var buffer = new byte[16];
            var writer = new BitWriter(buffer);
            writer.WriteVarULong(ulong.MaxValue);
            Assert.AreEqual(10, writer.BytesWritten);
            Assert.AreEqual("ff ff ff ff ff ff ff ff ff 01", Hex(writer.Written));

            var reader = new BitReader(writer.Written);
            Assert.IsTrue(reader.TryReadVarULong(out var value));
            Assert.AreEqual(ulong.MaxValue, value);
        }

        [Test]
        public void ZigZagVarIntsRoundTripTheExtremes() {
            var buffer = new byte[64];
            var writer = new BitWriter(buffer);
            writer.WriteVarInt(0);
            writer.WriteVarInt(-1);
            writer.WriteVarInt(1);
            writer.WriteVarInt(int.MinValue);
            writer.WriteVarInt(int.MaxValue);
            writer.WriteVarLong(long.MinValue);
            writer.WriteVarLong(long.MaxValue);
            writer.WriteVarLong(-2);

            Assert.AreEqual("00 01 02 ff ff ff ff 0f fe ff ff ff 0f", Hex(writer.Written[..13]));

            var reader = new BitReader(writer.Written);
            Assert.IsTrue(reader.TryReadVarInt(out var zero));
            Assert.IsTrue(reader.TryReadVarInt(out var minusOne));
            Assert.IsTrue(reader.TryReadVarInt(out var one));
            Assert.IsTrue(reader.TryReadVarInt(out var minimum));
            Assert.IsTrue(reader.TryReadVarInt(out var maximum));
            Assert.IsTrue(reader.TryReadVarLong(out var longMinimum));
            Assert.IsTrue(reader.TryReadVarLong(out var longMaximum));
            Assert.IsTrue(reader.TryReadVarLong(out var minusTwo));
            Assert.AreEqual(0, zero);
            Assert.AreEqual(-1, minusOne);
            Assert.AreEqual(1, one);
            Assert.AreEqual(int.MinValue, minimum);
            Assert.AreEqual(int.MaxValue, maximum);
            Assert.AreEqual(long.MinValue, longMinimum);
            Assert.AreEqual(long.MaxValue, longMaximum);
            Assert.AreEqual(-2L, minusTwo);
        }

        [Test]
        public void AVarIntThatOverflowsItsTypeIsRefused() {
            var reader = new BitReader(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x1F });
            Assert.IsFalse(reader.TryReadVarUInt(out _));
            Assert.AreEqual(0, reader.BitsRead);

            var unterminated = new BitReader(new byte[] { 0x80, 0x80 });
            Assert.IsFalse(unterminated.TryReadVarUInt(out _));
        }

        [Test]
        public void QuantizedValuesRoundTripWithinHalfAStep() {
            var quantization = Quantization.Linear(-100f, 100f, 10);
            var buffer = new byte[64];
            var writer = new BitWriter(buffer);
            writer.WriteQuantized(-100f, quantization);
            writer.WriteQuantized(100f, quantization);
            writer.WriteQuantized(0f, quantization);
            writer.WriteQuantized(37.5f, quantization);
            writer.WriteQuantized(250f, quantization);

            var reader = new BitReader(writer.Written);
            Assert.IsTrue(reader.TryReadQuantized(quantization, out var minimum));
            Assert.IsTrue(reader.TryReadQuantized(quantization, out var maximum));
            Assert.IsTrue(reader.TryReadQuantized(quantization, out var zero));
            Assert.IsTrue(reader.TryReadQuantized(quantization, out var middle));
            Assert.IsTrue(reader.TryReadQuantized(quantization, out var clamped));
            Assert.AreEqual(-100f, minimum);
            Assert.AreEqual(100f, maximum);
            Assert.AreEqual(0f, zero, quantization.Step / 2);
            Assert.AreEqual(37.5f, middle, quantization.Step / 2);
            Assert.AreEqual(100f, clamped);
        }

        [Test]
        public void AlignMovesToTheNextByte() {
            var buffer = new byte[4];
            var writer = new BitWriter(buffer);
            writer.WriteBits(1u, 3);
            writer.Align();
            writer.WriteBits(0xFFu, 8);

            Assert.AreEqual(16, writer.BitsWritten);
            Assert.AreEqual(new byte[] { 0x01, 0xFF, 0, 0 }, buffer);

            var reader = new BitReader(buffer);
            Assert.IsTrue(reader.TryReadBits(3, out _));
            reader.Align();
            Assert.IsTrue(reader.TryReadBits(8, out var next));
            Assert.AreEqual(0xFFu, next);
        }

        [Test]
        public void BitWriter_NoAllocation() {
            var buffer = new byte[256];
            var quantization = Quantization.Linear(0f, 1f, 12);
            TestDelegate action = () => {
                var writer = new BitWriter(buffer);
                writer.WriteBits(5u, 3);
                writer.WriteVarUInt(300u);
                writer.WriteVarLong(-70000L);
                writer.WriteQuantized(0.25f, quantization);
                var reader = new BitReader(writer.Written);
                reader.TryReadBits(3, out _);
                reader.TryReadVarUInt(out _);
                reader.TryReadVarLong(out _);
                reader.TryReadQuantized(quantization, out _);
            };

            for (var warm = 0; warm < 3; warm++) {
                action();
            }

            var constraint = ((IResolveConstraint)Is.Not.AllocatingGCMemory()).Resolve();
            constraint.ApplyTo(action);
            Assert.That(action, constraint);
        }

        [Test]
        public void EveryLongBitCountRoundTripsAndFailsAtTheEnd() {
            var buffer = new byte[16];

            for (var bitCount = 33; bitCount <= 64; bitCount++) {
                var writer = new BitWriter(buffer);
                var mask = bitCount == 64 ? ulong.MaxValue : (1UL << bitCount) - 1UL;
                var value = 0x5A5A5A5A5A5A5A5AUL & mask;
                writer.WriteBits(1u, 3);
                writer.WriteLongBits(value, bitCount);

                var reader = new BitReader(writer.Written);
                Assert.IsTrue(reader.TryReadBits(3, out _));
                Assert.IsTrue(reader.TryReadLongBits(bitCount, out var read));
                Assert.AreEqual(value, read, "bit count " + bitCount);

                var short1 = new BitReader(writer.Written[..(writer.BytesWritten - 1)]);
                Assert.IsTrue(short1.TryReadBits(3, out _));
                Assert.IsFalse(short1.TryReadLongBits(bitCount, out _), "bit count " + bitCount);
                Assert.AreEqual(3, short1.BitsRead);

                var tight = new BitWriter(new byte[(bitCount + 7) / 8]);
                Assert.IsTrue(tight.TryWriteLongBits(value, bitCount));
                Assert.IsFalse(tight.TryWriteBits(1u, 8));
            }
        }

        [Test]
        public void LongVarIntsWriteTheSameBytesAtEveryBoundary() {
            AssertVarULong(127UL, "7f");
            AssertVarULong(128UL, "80 01");
            AssertVarULong(1UL << 32, "80 80 80 80 10");
            AssertVarULong((1UL << 35) - 1, "ff ff ff ff 7f");
            AssertVarULong(1UL << 35, "80 80 80 80 80 01");
            AssertVarULong((1UL << 63), "80 80 80 80 80 80 80 80 80 01");
            AssertVarULong(ulong.MaxValue, "ff ff ff ff ff ff ff ff ff 01");

            var overflow = new BitReader(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x02 });
            Assert.IsFalse(overflow.TryReadVarULong(out _));
            Assert.AreEqual(0, overflow.BitsRead);
        }

        [Test]
        public void ZigZagLongsWriteTheirGoldenBytes() {
            var buffer = new byte[32];
            var writer = new BitWriter(buffer);
            writer.WriteVarLong(long.MinValue);
            writer.WriteVarLong(long.MaxValue);
            writer.WriteVarLong(-2);
            writer.WriteVarLong(1);

            Assert.AreEqual("ff ff ff ff ff ff ff ff ff 01 fe ff ff ff ff ff ff ff ff 01 03 02", Hex(writer.Written));
        }

        private static void AssertVarULong(ulong value, string expected) {
            var buffer = new byte[16];
            var writer = new BitWriter(buffer);
            writer.WriteVarULong(value);
            Assert.AreEqual(expected, Hex(writer.Written), "value " + value);

            var reader = new BitReader(writer.Written);
            Assert.IsTrue(reader.TryReadVarULong(out var read));
            Assert.AreEqual(value, read);

            var aligned = new byte[16];
            var span = aligned.AsSpan();
            Packer.WriteVarULong(ref span, value);
            Assert.AreEqual(expected, Hex(aligned.AsSpan(0, 16 - span.Length)), "aligned value " + value);
        }

        private static void AssertVarUInt(uint value, string expected) {
            var buffer = new byte[8];
            var writer = new BitWriter(buffer);
            writer.WriteVarUInt(value);
            Assert.AreEqual(expected, Hex(writer.Written), "value " + value);

            var reader = new BitReader(writer.Written);
            Assert.IsTrue(reader.TryReadVarUInt(out var read));
            Assert.AreEqual(value, read);
        }

        private static string Hex(ReadOnlySpan<byte> bytes) {
            var parts = new string[bytes.Length];

            for (var index = 0; index < bytes.Length; index++) {
                parts[index] = bytes[index].ToString("x2");
            }

            return string.Join(" ", parts);
        }
    }
}
