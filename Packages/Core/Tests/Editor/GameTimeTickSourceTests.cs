// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using NUnit.Framework;
using Spellbound.Core.Simulation;

namespace Spellbound.Core.Tests {
    public class GameTimeTickSourceTests {
        [Test]
        public void ADefaultRateIsRefused() {
            Assert.Throws<ArgumentException>(() => new GameTimeTickSource(default));
        }

        [Test]
        public void FiftyStepsOfTwentyMillisecondsAtTwentyGiveTwentyTicks() {
            Assert.AreEqual(new Tick(20), TicksAfter(20, 0.02, 50));
        }

        [Test]
        public void SixHundredSecondsGiveTwelveThousandTicks() {
            Assert.AreEqual(new Tick(12000), TicksAfter(20, 0.02, 30000));
        }

        [Test]
        public void TenMillisecondStepsGiveTheSameTicks() {
            Assert.AreEqual(new Tick(20), TicksAfter(20, 0.01, 100));
        }

        [Test]
        public void TwentyFiveMillisecondStepsGiveTheSameTicks() {
            Assert.AreEqual(new Tick(20), TicksAfter(20, 0.025, 40));
        }

        [Test]
        public void FloatStepStillGivesTwentyTicks() {
            Assert.AreEqual(new Tick(20), TicksAfter(20, (double)0.02f, 50));
        }

        [Test]
        public void ChangingTheRateOrTheStepChangesNoCode() {
            var rows = new[] {
                (rate: 20.0, step: 0.02, steps: 50, expected: 20UL),
                (rate: 30.0, step: 0.02, steps: 50, expected: 30UL),
                (rate: 20.0, step: 0.01, steps: 100, expected: 20UL),
                (rate: 12.5, step: 0.02, steps: 400, expected: 100UL)
            };

            foreach (var row in rows) {
                Assert.AreEqual(
                    new Tick(row.expected),
                    TicksAfter(row.rate, row.step, row.steps),
                    "at " + row.rate + " a second with a step of " + row.step);
            }
        }

        [Test]
        public void ElapsedSecondsAfterFiftyStepsIsOneSecond() {
            var source = new GameTimeTickSource(TickRate.PerSecond(20));

            for (var step = 0; step < 50; step++) {
                source.FixedStep(0.02);
            }

            Assert.AreEqual(1_000_000L, source.ElapsedMicroseconds);
            Assert.AreEqual(1.0, source.ElapsedSeconds);
        }

        [Test]
        public void NegativeStepThrows() {
            var source = new GameTimeTickSource(TickRate.PerSecond(20));
            Assert.Throws<ArgumentOutOfRangeException>(() => source.FixedStep(-0.02));
        }

        private static Tick TicksAfter(double ticksPerSecond, double stepSeconds, int steps) {
            var source = new GameTimeTickSource(TickRate.PerSecond(ticksPerSecond));

            for (var step = 0; step < steps; step++) {
                source.FixedStep(stepSeconds);
            }

            return source.Current();
        }
    }
}
