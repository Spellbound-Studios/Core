// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using NUnit.Framework;
using Spellbound.Core.Testing;
using Unity.Collections;

namespace Spellbound.Core.Tests {
    public class GoldenHashTests {
        [Test]
        public void EmptyHashIsTheOffsetBasis() {
            Assert.AreEqual(14695981039346656037UL, GoldenHash.Start().Value);
        }

        [Test]
        public void SingleLetterMatchesTheReferenceValue() {
            Assert.AreEqual(0xaf63dc4c8601ec8cUL, GoldenHash.Start().Append("a").Value);
            Assert.AreEqual(0xaf63dc4c8601ec8cUL, GoldenHash.Start().Append((byte)0x61).Value);
        }

        [Test]
        public void TwoAppendsEqualOneAppendOfTheJoinedBytes() {
            var joined = GoldenHash.Start().Append(new ReadOnlySpan<byte>(new byte[] { 1, 2, 3, 4, 5 }));
            var split = GoldenHash.Start().Append(new ReadOnlySpan<byte>(new byte[] { 1, 2 }))
                .Append(new ReadOnlySpan<byte>(new byte[] { 3, 4, 5 }));
            Assert.AreEqual(joined.Value, split.Value);
        }

        [Test]
        public void OrderMatters() {
            var forward = GoldenHash.Start().Append(1).Append(2);
            var backward = GoldenHash.Start().Append(2).Append(1);
            Assert.AreNotEqual(forward.Value, backward.Value);
        }

        [Test]
        public void IntegersHashTheirLittleEndianBytes() {
            var bytes = GoldenHash.Start().Append(new ReadOnlySpan<byte>(new byte[] { 0x78, 0x56, 0x34, 0x12 }));
            Assert.AreEqual(bytes.Value, GoldenHash.Start().Append(0x12345678).Value);
            Assert.AreEqual(bytes.Value, GoldenHash.Start().Append(0x12345678U).Value);
        }

        [Test]
        public void FloatHashesItsBits() {
            var bits = BitConverter.SingleToInt32Bits(1.5f);
            Assert.AreEqual(GoldenHash.Start().Append(bits).Value, GoldenHash.Start().Append(1.5f).Value);
        }

        [Test]
        public void NullStringHashesAsEmpty() {
            Assert.AreEqual(GoldenHash.Start().Value, GoldenHash.Start().Append((string)null).Value);
            Assert.AreEqual(GoldenHash.Start().Value, GoldenHash.Start().Append(string.Empty).Value);
        }

        [Test]
        public void NativeArrayHashesItsBytes() {
            var values = new NativeArray<int>(2, Allocator.Temp);
            try {
                values[0] = 7;
                values[1] = -3;
                var expected = GoldenHash.Start().Append(7).Append(-3);
                Assert.AreEqual(expected.Value, GoldenHash.Start().Append(values).Value);
            } finally {
                values.Dispose();
            }
        }

        [Test]
        public void HexIsSixteenLowerCaseDigits() {
            Assert.AreEqual("cbf29ce484222325", GoldenHash.Start().Hex);
            Assert.AreEqual("af63dc4c8601ec8c", GoldenHash.Start().Append("a").Hex);
        }

        [Test]
        public void AssertionPassesOnEqualHashes() {
            var hash = GoldenHash.Start().Append("a");
            DeterminismAssert.GoldenHash(0xaf63dc4c8601ec8cUL, hash, "letters");
            DeterminismAssert.GoldenHash("af63dc4c8601ec8c", hash, "letters");
            DeterminismAssert.GoldenHash("0xAF63DC4C8601EC8C", hash, "letters");
        }

        [Test]
        public void AssertionThrowsWithTheRegionOnUnequalHashes() {
            var hash = GoldenHash.Start().Append("a");
            var exception = Assert.Throws<AssertionException>(
                () => DeterminismAssert.GoldenHash(1UL, hash, "harbour"));
            Assert.AreEqual("region 'harbour': golden 0x0000000000000001, actual 0xaf63dc4c8601ec8c",
                exception.Message);
        }

        [Test]
        public void HexAssertionThrowsOnUnequalHashes() {
            var hash = GoldenHash.Start();
            var exception = Assert.Throws<AssertionException>(
                () => DeterminismAssert.GoldenHash("af63dc4c8601ec8c", hash, "harbour"));
            StringAssert.Contains("harbour", exception.Message);
        }
    }
}
