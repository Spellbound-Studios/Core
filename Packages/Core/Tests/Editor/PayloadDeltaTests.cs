// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Spellbound.Core.Logging;
using Spellbound.Core.Packing;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Spellbound.Core.Tests {
    public class PayloadDeltaTests {
        [Test]
        public void IdenticalPayloadsGiveTheLengthOneSkipAndNoLiteral() {
            var payload = Fill(300, 7);
            var delta = new byte[PayloadDelta.LargestDeltaBytes(payload.Length)];

            Assert.IsTrue(PayloadDelta.Pack(payload, payload, delta, out var written));
            Assert.AreEqual("ac 02 ac 02 00", Hex(delta, written));
            AssertRebuilds(payload, payload, delta, written);
        }

        [Test]
        public void OneChangedByteGivesSkipThenOneLiteralThenSkip() {
            var baseline = Fill(32, 1);
            var current = (byte[])baseline.Clone();
            current[10] = 0xEE;
            var delta = new byte[PayloadDelta.LargestDeltaBytes(current.Length)];

            Assert.IsTrue(PayloadDelta.Pack(baseline, current, delta, out var written));
            Assert.AreEqual("20 0a 01 ee 15 00", Hex(delta, written));
            AssertRebuilds(baseline, current, delta, written);
            Assert.IsTrue(PayloadDelta.TryReadUnpackedLength(delta.AsSpan(0, written), out var length));
            Assert.AreEqual(32, length);
        }

        [Test]
        public void ChangedRunsAtTheStartMiddleAndEndRoundTrip() {
            var baseline = Fill(64, 3);
            var start = (byte[])baseline.Clone();
            var middle = (byte[])baseline.Clone();
            var end = (byte[])baseline.Clone();

            for (var index = 0; index < 5; index++) {
                start[index] = 0xA0;
                middle[30 + index] = 0xB0;
                end[59 + index] = 0xC0;
            }

            AssertRoundTrips(baseline, start, "40 00 05 a0 a0 a0 a0 a0 3b 00");
            AssertRoundTrips(baseline, middle, "40 1e 05 b0 b0 b0 b0 b0 1d 00");
            AssertRoundTrips(baseline, end, "40 3b 05 c0 c0 c0 c0 c0");
        }

        [Test]
        public void ShortUnchangedRunsFoldIntoTheLiteral() {
            var baseline = Fill(16, 0);
            var current = (byte[])baseline.Clone();
            current[2] = 1;
            current[4] = 1;
            current[6] = 1;

            AssertRoundTrips(baseline, current, "10 02 05 01 00 01 00 01 09 00");
        }

        [Test]
        public void ALongerCurrentCarriesItsTailAsLiteral() {
            var baseline = Fill(8, 9);
            var current = new byte[12];
            baseline.CopyTo(current, 0);
            current[8] = 1;
            current[9] = 2;
            current[10] = 3;
            current[11] = 4;

            AssertRoundTrips(baseline, current, "0c 08 04 01 02 03 04");
        }

        [Test]
        public void AShorterCurrentEndsEarly() {
            AssertRoundTrips(Fill(20, 5), Fill(12, 5), "0c 0c 00");
        }

        [Test]
        public void EmptyPayloadsGiveTheLengthAlone() {
            var delta = new byte[8];

            Assert.IsTrue(PayloadDelta.Pack(Array.Empty<byte>(), Array.Empty<byte>(), delta, out var written));
            Assert.AreEqual("00", Hex(delta, written));
            Assert.IsTrue(PayloadDelta.TryUnpack(Array.Empty<byte>(), delta.AsSpan(0, written), delta, out var rebuilt));
            Assert.AreEqual(0, rebuilt);
            Assert.IsTrue(PayloadDelta.Pack(Fill(5, 1), Array.Empty<byte>(), delta, out written));
            Assert.AreEqual("00", Hex(delta, written));
            Assert.IsFalse(PayloadDelta.TryUnpack(Fill(5, 1), ReadOnlySpan<byte>.Empty, delta, out _));
        }

        [Test]
        public void AThousandSeededPairsRoundTripWithinTheBound() {
            var random = new Random(20);

            for (var pair = 0; pair < 1000; pair++) {
                var baseline = new byte[random.Next(0, 300)];
                random.NextBytes(baseline);
                var current = new byte[Math.Max(0, baseline.Length + random.Next(-20, 21))];
                var shared = Math.Min(baseline.Length, current.Length);
                Array.Copy(baseline, current, shared);

                for (var index = shared; index < current.Length; index++) {
                    current[index] = (byte)random.Next(256);
                }

                var changes = random.Next(0, 12);

                for (var change = 0; change < changes && current.Length > 0; change++) {
                    current[random.Next(current.Length)] ^= (byte)random.Next(1, 256);
                }

                var delta = new byte[PayloadDelta.LargestDeltaBytes(current.Length)];
                Assert.IsTrue(PayloadDelta.Pack(baseline, current, delta, out var written), "pair " + pair);
                Assert.LessOrEqual(written, delta.Length);
                AssertRebuilds(baseline, current, delta, written);
            }
        }

        [Test]
        public void AMalformedDeltaIsRefused() {
            var baseline = Fill(8, 1);
            var destination = new byte[16];

            Assert.IsFalse(PayloadDelta.TryUnpack(baseline, new byte[] { 0x08, 0x09, 0x00 }, destination, out _));
            Assert.IsFalse(PayloadDelta.TryUnpack(baseline, new byte[] { 0x08, 0x00, 0x05, 1, 2 }, destination, out _));
            Assert.IsFalse(PayloadDelta.TryUnpack(baseline, new byte[] { 0x80 }, destination, out _));
            Assert.IsFalse(PayloadDelta.TryUnpack(baseline, new byte[] { 0x08, 0x04 }, destination, out _));
            Assert.IsFalse(PayloadDelta.TryUnpack(baseline, new byte[] { 0x08, 0x08, 0x10, 1 }, destination, out _));
            Assert.IsFalse(PayloadDelta.TryUnpack(baseline, new byte[] { 0x10, 0x08, 0x00 }, destination, out _));
            Assert.IsFalse(PayloadDelta.TryUnpack(baseline, new byte[] { 0x20, 0x0a, 0x01, 0xee }, destination, out _));
            Assert.IsFalse(PayloadDelta.TryUnpack(baseline, new byte[] { 0x11, 0x08, 0x00 }, destination, out _));
        }

        [Test]
        public void ALiteralPastTheBaselineThenASkipIsSoundOrRefusedWithoutThrowing() {
            var baseline = new byte[] { 1, 2 };
            var destination = new byte[16];
            var split = new byte[] { 0x05, 0x00, 0x03, 7, 8, 9, 0x00, 0x02, 10, 11 };

            Assert.IsTrue(PayloadDelta.TryUnpack(baseline, split, destination, out var written));
            Assert.AreEqual(5, written);
            Assert.AreEqual(new byte[] { 7, 8, 9, 10, 11 }, destination.AsSpan(0, 5).ToArray());

            var skipPastBaseline = new byte[] { 0x0c, 0x00, 0x0a, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 0x02, 0x00 };
            Assert.IsFalse(PayloadDelta.TryUnpack(baseline, skipPastBaseline, destination, out _));
        }

        [Test]
        public void RandomDeltaBytesNeverThrow() {
            var random = new Random(7);
            var destination = new byte[64];

            for (var attempt = 0; attempt < 200000; attempt++) {
                var baseline = new byte[random.Next(0, 9)];
                random.NextBytes(baseline);
                var delta = new byte[random.Next(0, 16)];
                random.NextBytes(delta);
                PayloadDelta.TryUnpack(baseline, delta, destination, out _);
            }
        }

        [Test]
        public void ADestinationTooSmallIsRefused() {
            var baseline = Fill(40, 2);
            var current = (byte[])baseline.Clone();
            current[0] = 9;

            Assert.IsFalse(PayloadDelta.Pack(baseline, current, new byte[2], out var written));
            Assert.AreEqual(0, written);

            var delta = new byte[16];
            Assert.IsTrue(PayloadDelta.Pack(baseline, current, delta, out written));
            Assert.IsFalse(PayloadDelta.TryUnpack(baseline, delta.AsSpan(0, written), new byte[39], out var rebuilt));
            Assert.AreEqual(0, rebuilt);
        }

        [Test]
        public void AKilobyteWithEightChangedBytesIsReported() {
            var baseline = Fill(1024, 4);
            var current = (byte[])baseline.Clone();

            foreach (var index in new[] { 3, 100, 101, 400, 640, 641, 642, 1000 }) {
                current[index] = 0xFF;
            }

            var delta = new byte[PayloadDelta.LargestDeltaBytes(current.Length)];
            Assert.IsTrue(PayloadDelta.Pack(baseline, current, delta, out var written));
            AssertRebuilds(baseline, current, delta, written);
            Log.Emit(LogLevel.Info, "PayloadDelta: 1024-byte payload with 8 changed bytes packs to " + written
                                    + " bytes; the bound is " + delta.Length);
            Assert.Less(written, 40);
        }

        [Test]
        public void DeltaPack_NoAllocation() {
            var baseline = Fill(256, 6);
            var current = (byte[])baseline.Clone();
            current[50] = 1;
            current[200] = 2;
            var delta = new byte[PayloadDelta.LargestDeltaBytes(current.Length)];
            var rebuilt = new byte[256];
            TestDelegate action = () => {
                PayloadDelta.Pack(baseline, current, delta, out var written);
                PayloadDelta.TryUnpack(baseline, delta.AsSpan(0, written), rebuilt, out _);
            };

            for (var warm = 0; warm < 3; warm++) {
                action();
            }

            var constraint = ((IResolveConstraint)Is.Not.AllocatingGCMemory()).Resolve();
            constraint.ApplyTo(action);
            Assert.That(action, constraint);
        }

        private static void AssertRoundTrips(byte[] baseline, byte[] current, string expectedHex) {
            var delta = new byte[PayloadDelta.LargestDeltaBytes(current.Length)];

            Assert.IsTrue(PayloadDelta.Pack(baseline, current, delta, out var written));
            Assert.AreEqual(expectedHex, Hex(delta, written));
            AssertRebuilds(baseline, current, delta, written);
        }

        private static void AssertRebuilds(byte[] baseline, byte[] current, byte[] delta, int written) {
            var rebuilt = new byte[current.Length];

            Assert.IsTrue(PayloadDelta.TryReadUnpackedLength(delta.AsSpan(0, written), out var expected));
            Assert.AreEqual(current.Length, expected);
            Assert.IsTrue(PayloadDelta.TryUnpack(baseline, delta.AsSpan(0, written), rebuilt, out var length));
            Assert.AreEqual(current.Length, length);
            Assert.AreEqual(current, rebuilt);
        }

        private static byte[] Fill(int length, byte value) {
            var bytes = new byte[length];
            Array.Fill(bytes, value);
            return bytes;
        }

        private static string Hex(byte[] bytes, int count) {
            var parts = new string[count];

            for (var index = 0; index < count; index++) {
                parts[index] = bytes[index].ToString("x2");
            }

            return string.Join(" ", parts);
        }
    }
}
