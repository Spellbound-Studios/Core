// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using NUnit.Framework;
using Spellbound.Core.Observations;
using Spellbound.Core.Simulation;
using UnityEngine.TestTools;

namespace Spellbound.Core.Tests {
    public class TickClockTests {
        [TearDown]
        public void RestoreLogAssert() {
            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        public void NullSourceThrows() {
            Assert.Throws<ArgumentNullException>(() => new TickClock(null, ObservationContext.Discard));
        }

        [Test]
        public void ASourceWithADefaultRateIsRefused() {
            Assert.Throws<ArgumentException>(() => new TickClock(new SetTickSource(default, new Tick(0)), ObservationContext.Discard));
        }

        [Test]
        public void ClockStartsAtTheSourceTick() {
            var source = new SetTickSource(TickRate.PerSecond(20), new Tick(7));
            var clock = new TickClock(source, ObservationContext.Discard);
            Assert.AreEqual(new Tick(7), clock.Current);
            Assert.AreSame(source, clock.Source);
            Assert.AreEqual(TickRate.PerSecond(20), clock.Rate);
        }

        [Test]
        public void AdvanceFollowsAHigherTick() {
            var source = new SetTickSource(TickRate.PerSecond(20), new Tick(7));
            var clock = new TickClock(source, ObservationContext.Discard);
            source.Next = new Tick(9);
            Assert.IsTrue(clock.Advance());
            Assert.AreEqual(new Tick(9), clock.Current);
        }

        [Test]
        public void NullObservationsThrow() {
            var source = new SetTickSource(TickRate.PerSecond(20), new Tick(7));
            Assert.Throws<ArgumentNullException>(() => new TickClock(source, null));
        }

        [Test]
        public void AnEqualTickReturnsFalseAndCountsNothing() {
            using var observations = CoreKit.Observations.CreateContext("clock_equal");
            var source = new SetTickSource(TickRate.PerSecond(20), new Tick(7));
            var clock = new TickClock(source, observations);
            Assert.IsFalse(clock.Advance());
            Assert.AreEqual(new Tick(7), clock.Current);
            Assert.AreSame(observations, clock.Observations);
            Assert.IsFalse(observations.TryRead(CoreObservations.ClockHeld.Key, out _));
            Assert.IsFalse(observations.TryRead(CoreObservations.ClockRateChanged.Key, out _));
        }

        [Test]
        public void ALowerTickHoldsTheClockAndCountsOneHeldStep() {
            using var observations = CoreKit.Observations.CreateContext("clock_held");
            var source = new SetTickSource(TickRate.PerSecond(20), new Tick(7));
            var clock = new TickClock(source, observations);
            source.Next = new Tick(4);
            Assert.IsFalse(clock.Advance());
            Assert.AreEqual(new Tick(7), clock.Current);
            Assert.IsTrue(observations.TryRead(CoreObservations.ClockHeld.Key, out var held));
            Assert.AreEqual(1, held.Value);
            source.Next = new Tick(8);
            Assert.IsTrue(clock.Advance());
            Assert.AreEqual(new Tick(8), clock.Current);
            Assert.IsTrue(observations.TryRead(CoreObservations.ClockHeld.Key, out held));
            Assert.AreEqual(1, held.Value);
        }

        [Test]
        public void AChangedRateIsCountedByItsNewRateAndTheStartingRateIsKept() {
            LogAssert.ignoreFailingMessages = true;
            using var observations = CoreKit.Observations.CreateContext("clock_rate");
            var source = new SetTickSource(TickRate.PerSecond(20), new Tick(7));
            var clock = new TickClock(source, observations);
            source.Rate = TickRate.PerSecond(30);
            clock.Advance();
            clock.Advance();
            Assert.IsTrue(observations.TryRead(CoreObservations.ClockRateChanged.Key, out var changed));
            Assert.AreEqual(1, changed.Value);
            source.Rate = TickRate.PerSecond(40);
            clock.Advance();
            source.Rate = TickRate.PerSecond(20);
            clock.Advance();
            Assert.IsTrue(observations.TryRead(CoreObservations.ClockRateChanged.Key, out changed));
            Assert.AreEqual(3, changed.Value);
            Assert.AreEqual(TickRate.PerSecond(20), clock.Rate);
            var reasons = new System.Collections.Generic.List<ReasonCount>();
            Assert.AreEqual(3, observations.ReadReasons(CoreObservations.ClockRateChanged.Key, reasons));
            CollectionAssert.Contains(reasons.ConvertAll(entry => entry.Reason.Name), TickRate.PerSecond(30).ToString());
        }

        [Test]
        public void TheDiscardContextRecordsNothing() {
            var source = new SetTickSource(TickRate.PerSecond(20), new Tick(7));
            var clock = new TickClock(source, ObservationContext.Discard);
            source.Next = new Tick(4);
            Assert.IsFalse(clock.Advance());
            Assert.IsFalse(ObservationContext.Discard.TryRead(CoreObservations.ClockHeld.Key, out _));
        }

        [Test]
        public void RaiseSteppedRaisesTheCurrentTick() {
            var source = new SetTickSource(TickRate.PerSecond(20), new Tick(7));
            var clock = new TickClock(source, ObservationContext.Discard);
            source.Next = new Tick(11);
            clock.Advance();
            var raised = new Tick[1];
            var calls = 0;
            clock.Stepped += tick => {
                raised[0] = tick;
                calls++;
            };

            clock.RaiseStepped();

            Assert.AreEqual(1, calls);
            Assert.AreEqual(new Tick(11), raised[0]);
        }

        [Test]
        public void ElapsedSecondsIsTheCurrentTickAtTheRate() {
            var source = new SetTickSource(TickRate.PerSecond(20), new Tick(30));
            var clock = new TickClock(source, ObservationContext.Discard);
            Assert.AreEqual(1.5, clock.ElapsedSeconds, 1e-9);
        }

        [Test]
        public void ITickSource_CustomImplementation() {
            var source = new CountingTickSource(TickRate.PerSecond(10));
            var clock = new TickClock(source, ObservationContext.Discard);
            Assert.AreEqual(TickRate.PerSecond(10), clock.Rate);
            Assert.AreEqual(new Tick(0), clock.Current);
            Assert.IsTrue(clock.Advance());
            Assert.AreEqual(new Tick(1), clock.Current);
            Assert.IsTrue(clock.Advance());
            Assert.AreEqual(new Tick(2), clock.Current);
            Assert.AreEqual(0.2, clock.ElapsedSeconds, 1e-9);
            Assert.AreEqual(3, source.Calls);
        }

        private sealed class CountingTickSource : ITickSource {
            public CountingTickSource(TickRate rate) {
                Rate = rate;
            }

            public TickRate Rate { get; }

            public int Calls { get; private set; }

            public Tick Current() => new Tick((ulong)Calls++);
        }

        private sealed class SetTickSource : ITickSource {
            public SetTickSource(TickRate rate, Tick next) {
                Rate = rate;
                Next = next;
            }

            public TickRate Rate { get; set; }

            public Tick Next { get; set; }

            public Tick Current() => Next;
        }
    }
}
