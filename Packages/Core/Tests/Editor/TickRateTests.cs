// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using NUnit.Framework;
using Spellbound.Core.Simulation;

namespace Spellbound.Core.Tests {
    public class TickRateTests {
        private const double Tolerance = 1e-9;

        [Test]
        public void PerSecondRefusesZeroNegativeAndNonFiniteRates() {
            Assert.Throws<ArgumentOutOfRangeException>(() => TickRate.PerSecond(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => TickRate.PerSecond(-20));
            Assert.Throws<ArgumentOutOfRangeException>(() => TickRate.PerSecond(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => TickRate.PerSecond(double.PositiveInfinity));
        }

        [Test]
        public void StepSecondsIsTheInverseOfTheRate() {
            Assert.AreEqual(0.05, TickRate.PerSecond(20).StepSeconds, Tolerance);
        }

        [Test]
        public void OneSecondAtTwentyIsTwentySteps() {
            Assert.AreEqual(new TickSpan(20), TickRate.PerSecond(20).FromSeconds(1.0));
        }

        [Test]
        public void PositiveDurationNeverRoundsToZero() {
            Assert.AreEqual(new TickSpan(1), TickRate.PerSecond(20).FromSeconds(0.024));
        }

        [Test]
        public void FromSecondsRoundsToTheNearestTick() {
            Assert.AreEqual(new TickSpan(2), TickRate.PerSecond(20).FromSeconds(0.076));
        }

        [Test]
        public void ZeroSecondsIsZeroSteps() {
            Assert.AreEqual(TickSpan.Zero, TickRate.PerSecond(20).FromSeconds(0));
        }

        [Test]
        public void FromSecondsRefusesNegativeAndNaN() {
            var rate = TickRate.PerSecond(20);
            Assert.Throws<ArgumentOutOfRangeException>(() => rate.FromSeconds(-0.5));
            Assert.Throws<ArgumentOutOfRangeException>(() => rate.FromSeconds(double.NaN));
        }

        [Test]
        public void RoundTripIsWithinHalfAStep() {
            var rate = TickRate.PerSecond(20);
            var durations = new[] { 0.05, 0.3, 1.234, 7.77, 59.99, 600.0 };

            foreach (var seconds in durations) {
                var roundTrip = rate.ToSeconds(rate.FromSeconds(seconds));
                Assert.LessOrEqual(Math.Abs(roundTrip - seconds), rate.StepSeconds / 2 + Tolerance, "at " + seconds);
            }
        }

        [Test]
        public void SecondsRoundTripAcrossRatesIsWithinHalfAStepOfTheLoadingRate() {
            const double savedSeconds = 12.34;
            var savingRate = TickRate.PerSecond(30);
            var loadingRate = TickRate.PerSecond(50);

            var writtenSeconds = savingRate.ToSeconds(savingRate.FromSeconds(savedSeconds));
            var loaded = loadingRate.FromSeconds(writtenSeconds);

            var difference = Math.Abs(loadingRate.ToSeconds(loaded) - savedSeconds);
            Assert.LessOrEqual(difference, loadingRate.StepSeconds / 2 + Tolerance);
        }

        [Test]
        public void EqualRatesAreEqual() {
            Assert.IsTrue(TickRate.PerSecond(20) == TickRate.PerSecond(20));
            Assert.IsTrue(TickRate.PerSecond(20) != TickRate.PerSecond(30));
            Assert.AreEqual(TickRate.PerSecond(20).GetHashCode(), TickRate.PerSecond(20).GetHashCode());
        }

        [Test]
        public void RateWritesTicksPerSecond() {
            Assert.AreEqual("20 ticks/s", TickRate.PerSecond(20).ToString());
            Assert.AreEqual("12.5 ticks/s", TickRate.PerSecond(12.5).ToString());
        }
    }
}
