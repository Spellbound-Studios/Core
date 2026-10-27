// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using NUnit.Framework;
using Spellbound.Core.Simulation;
using UnityEngine;

namespace Spellbound.Core.Tests {
    public class SeededStreamTests {
        [Test]
        public void RawStateMatchesTheReferenceVectors() {
            var stream = SeededStream.FromState(1, 2, 3, 4);
            Assert.AreEqual(11520UL, stream.NextUnsigned());
            Assert.AreEqual(0UL, stream.NextUnsigned());
            Assert.AreEqual(1509978240UL, stream.NextUnsigned());
            Assert.AreEqual(1215971899390074240UL, stream.NextUnsigned());
        }

        [Test]
        public void AllZeroRawStateThrows() {
            Assert.Throws<ArgumentException>(() => SeededStream.FromState(0, 0, 0, 0));
        }

        [Test]
        public void SeedZeroMatchesItsGoldenVector() {
            var stream = new SeededStream(0);
            Assert.AreEqual(11091344671253066420UL, stream.NextUnsigned());
            Assert.AreEqual(13793997310169335082UL, stream.NextUnsigned());
            Assert.AreEqual(1900383378846508768UL, stream.NextUnsigned());
            Assert.AreEqual(7684712102626143532UL, stream.NextUnsigned());
            Assert.AreEqual(13521403990117723737UL, stream.NextUnsigned());
        }

        [Test]
        public void SeedFortyTwoMatchesItsGoldenVector() {
            var stream = new SeededStream(42);
            Assert.AreEqual(1546998764402558742UL, stream.NextUnsigned());
            Assert.AreEqual(6990951692964543102UL, stream.NextUnsigned());
            Assert.AreEqual(12544586762248559009UL, stream.NextUnsigned());
            Assert.AreEqual(17057574109182124193UL, stream.NextUnsigned());
            Assert.AreEqual(18295552978065317476UL, stream.NextUnsigned());
        }

        [Test]
        public void SeedFortyTwoMatchesItsGoldenDoubles() {
            var stream = new SeededStream(42);
            Assert.AreEqual(0.08386297105988216, stream.NextDouble());
            Assert.AreEqual(0.3789802506626686, stream.NextDouble());
            Assert.AreEqual(0.6800434110281394, stream.NextDouble());
        }

        [Test]
        public void NextDoubleStaysInTheUnitInterval() {
            var stream = new SeededStream(7);
            for (var draw = 0; draw < 10000; draw++) {
                var value = stream.NextDouble();
                Assert.IsTrue(value >= 0.0 && value < 1.0, value.ToString("R"));
            }
        }

        [Test]
        public void NextFloatStaysInTheUnitInterval() {
            var stream = new SeededStream(7);
            for (var draw = 0; draw < 10000; draw++) {
                var value = stream.NextFloat();
                Assert.IsTrue(value >= 0f && value < 1f, value.ToString("R"));
            }
        }

        [Test]
        public void NextIntStaysInRangeAndHitsEveryValue() {
            var stream = new SeededStream(7);
            var hits = new int[4];
            for (var draw = 0; draw < 10000; draw++) {
                var value = stream.NextInt(3, 7);
                Assert.IsTrue(value >= 3 && value < 7, value.ToString());
                hits[value - 3]++;
            }

            for (var index = 0; index < hits.Length; index++) {
                Assert.Greater(hits[index], 0, "value " + (index + 3));
            }
        }

        [Test]
        public void NextIntCoversTheWholeIntegerRange() {
            var stream = new SeededStream(7);
            for (var draw = 0; draw < 1000; draw++) {
                stream.NextInt(int.MinValue, int.MaxValue);
            }
        }

        [Test]
        public void NextIntWithMaximumNotAboveMinimumThrows() {
            var stream = new SeededStream(7);
            Assert.Throws<ArgumentException>(() => stream.NextInt(5, 5));
            Assert.Throws<ArgumentException>(() => stream.NextInt(5, 4));
        }

        [Test]
        public void OneSeedDrawsOneSequence() {
            var first = new SeededStream(99);
            var second = new SeededStream(99);
            for (var draw = 0; draw < 1000; draw++) {
                Assert.AreEqual(first.NextUnsigned(), second.NextUnsigned());
            }
        }

        [Test]
        public void ForkDoesNotDependOnWhatTheParentDrew() {
            var fresh = new SeededStream(9);
            var drawn = new SeededStream(9);
            for (var draw = 0; draw < 100; draw++) {
                drawn.NextUnsigned();
            }

            var freshFork = fresh.Fork(7);
            var drawnFork = drawn.Fork(7);
            Assert.AreEqual(freshFork.NextUnsigned(), drawnFork.NextUnsigned());
        }

        [Test]
        public void ForksWithDifferentKeysDiffer() {
            var parent = new SeededStream(9);
            var first = parent.Fork(1);
            var second = parent.Fork(2);
            Assert.AreNotEqual(first.NextUnsigned(), second.NextUnsigned());
        }

        [Test]
        public void AlgorithmIsOne() {
            Assert.AreEqual(1, SeededStream.Algorithm);
        }

        [Test]
        public void WritesItsSeed() {
            Assert.AreEqual("42", new SeededStream(42).ToString());
        }

        [Test]
        public void SanityBatteryPasses() {
            const int bitDraws = 100000;
            var bitStream = new SeededStream(12345);
            var setCounts = new int[64];
            for (var draw = 0; draw < bitDraws; draw++) {
                var value = bitStream.NextUnsigned();
                for (var bit = 0; bit < 64; bit++) {
                    if (((value >> bit) & 1UL) != 0) {
                        setCounts[bit]++;
                    }
                }
            }

            var lowestFraction = 1.0;
            var highestFraction = 0.0;
            for (var bit = 0; bit < 64; bit++) {
                var fraction = setCounts[bit] / (double)bitDraws;
                lowestFraction = Math.Min(lowestFraction, fraction);
                highestFraction = Math.Max(highestFraction, fraction);
                Assert.IsTrue(fraction > 0.49 && fraction < 0.51, "bit " + bit + " fraction " + fraction);
            }

            var runStream = new SeededStream(12345);
            var longestRun = 0;
            var currentRun = 0;
            var previous = false;
            for (var draw = 0; draw < 100000; draw++) {
                var value = runStream.NextBool();
                currentRun = draw > 0 && value == previous ? currentRun + 1 : 1;
                previous = value;
                longestRun = Math.Max(longestRun, currentRun);
            }

            Assert.Less(longestRun, 40);

            const int byteDraws = 256000;
            var byteStream = new SeededStream(12345);
            var byteCounts = new int[256];
            for (var draw = 0; draw < byteDraws; draw++) {
                byteCounts[(int)(byteStream.NextUnsigned() & 0xFFUL)]++;
            }

            var expected = byteDraws / 256.0;
            var chiSquare = 0.0;
            for (var index = 0; index < byteCounts.Length; index++) {
                var difference = byteCounts[index] - expected;
                chiSquare += difference * difference / expected;
            }

            Assert.Less(chiSquare, 310.457);
            Debug.Log("SeededStreamTests: monobit fraction " + lowestFraction.ToString("F5") + " to "
                      + highestFraction.ToString("F5") + ", longest run " + longestRun + ", chi-square "
                      + chiSquare.ToString("F3") + " over 255 degrees of freedom");
        }
    }
}
