// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using NUnit.Framework;
using Spellbound.Core.Packing;

namespace Spellbound.Core.Tests {
    public class QuantizationTests {
        [Test]
        public void TheEndsOfTheRangeMapToTheFirstAndLastCode() {
            var quantization = Quantization.Linear(-1f, 1f, 8);

            Assert.AreEqual(0u, quantization.Quantize(-1f));
            Assert.AreEqual(255u, quantization.Quantize(1f));
            Assert.AreEqual(255u, quantization.LargestCode);
            Assert.AreEqual(-1f, quantization.Dequantize(0));
            Assert.AreEqual(1f, quantization.Dequantize(255));
            Assert.AreEqual(2f / 255f, quantization.Step, 1e-7f);
        }

        [Test]
        public void ValuesOutsideTheRangeClampAndNaNIsTheFirstCode() {
            var quantization = Quantization.Linear(0f, 10f, 4);

            Assert.AreEqual(0u, quantization.Quantize(-5f));
            Assert.AreEqual(15u, quantization.Quantize(50f));
            Assert.AreEqual(0u, quantization.Quantize(float.NaN));
            Assert.AreEqual(10f, quantization.Dequantize(99));
        }

        [Test]
        public void ASweepRoundTripsWithinHalfAStep() {
            foreach (var bitCount in new[] { 1, 4, 8, 12, 16, 24, 32 }) {
                var quantization = Quantization.Linear(-50f, 70f, bitCount);
                var halfStep = quantization.Step / 2f + 1e-5f;

                for (var sample = 0; sample <= 1000; sample++) {
                    var value = -50f + 120f * sample / 1000f;
                    var restored = quantization.Dequantize(quantization.Quantize(value));
                    Assert.AreEqual(value, restored, halfStep, "bits " + bitCount + " value " + value);
                }
            }
        }

        [Test]
        public void ThirtyTwoBitsUseTheWholeCodeSpace() {
            var quantization = Quantization.Linear(0f, 1f, 32);

            Assert.AreEqual(uint.MaxValue, quantization.LargestCode);
            Assert.AreEqual(uint.MaxValue, quantization.Quantize(1f));
            Assert.AreEqual(0.5f, quantization.Dequantize(quantization.Quantize(0.5f)), 1e-6f);
        }

        [Test]
        public void AnEmptyOrReversedRangeAndABadBitCountAreRefused() {
            Assert.Throws<ArgumentOutOfRangeException>(() => Quantization.Linear(1f, 1f, 8));
            Assert.Throws<ArgumentOutOfRangeException>(() => Quantization.Linear(2f, 1f, 8));
            Assert.Throws<ArgumentOutOfRangeException>(() => Quantization.Linear(0f, 1f, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Quantization.Linear(0f, 1f, 33));
            Assert.Throws<ArgumentOutOfRangeException>(() => Quantization.Linear(float.NegativeInfinity, 1f, 8));
        }
    }
}
