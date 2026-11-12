// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections;
using NUnit.Framework;
using Spellbound.Core.Logging;
using Spellbound.Core.Observations;
using Spellbound.Core.Simulation;
using UnityEngine;
using UnityEngine.TestTools;

namespace Spellbound.Core.PlayModeTests {
    public class TickClockDriverTests {
        private const string AdvanceStepTypeName = "TickClockAdvanceStep";

        private float _fixedDeltaTime;
        private float _timeScale;
        private TickClock _clock;

        [SetUp]
        public void RememberTime() {
            _fixedDeltaTime = Time.fixedDeltaTime;
            _timeScale = Time.timeScale;
        }

        [TearDown]
        public void RestoreTime() {
            CoreKit.Time.StopClock(_clock);
            _clock = null;
            Time.fixedDeltaTime = _fixedDeltaTime;
            Time.timeScale = _timeScale;
        }

        [UnityTest]
        public IEnumerator FiftyFixedStepsGiveTwentyTicks() {
            Time.fixedDeltaTime = 0.02f;
            _clock = CoreKit.Time.StartClock(TickRate.PerSecond(20), ObservationContext.Discard);
            var start = _clock.Current;
            var stepped = 0;
            _clock.Stepped += _ => stepped++;

            for (var step = 0; step < 50; step++) {
                yield return new WaitForFixedUpdate();
            }

            var advanced = start.Until(_clock.Current).Steps;
            Log.Emit(LogLevel.Info, "TickClockDriverTests: 50 fixed steps gave " + advanced
                + " ticks at 20 a second with fixedDeltaTime 0.02");
            Assert.AreEqual(20UL, advanced);
            Assert.GreaterOrEqual(stepped, 50);
        }

        [UnityTest]
        public IEnumerator TimeScaleZeroStopsTheDefaultSource() {
            _clock = CoreKit.Time.StartClock(TickRate.PerSecond(20), ObservationContext.Discard);
            yield return new WaitForFixedUpdate();
            var start = _clock.Current;
            Time.timeScale = 0;

            for (var frame = 0; frame < 30; frame++) {
                yield return null;
            }

            var advanced = start.Until(_clock.Current).Steps;
            Time.timeScale = _timeScale;
            Log.Emit(LogLevel.Info, "TickClockDriverTests: at time scale 0 the default source counted " + advanced
                + " ticks over 30 frames");
            Assert.AreEqual(0UL, advanced);
        }

        [UnityTest]
        public IEnumerator StopClockLeavesNoDriverAlive() {
            _clock = CoreKit.Time.StartClock(TickRate.PerSecond(20), ObservationContext.Discard);
            yield return null;
            Assert.AreEqual(1, LiveAdvanceSteps());

            CoreKit.Time.StopClock(_clock);
            _clock = null;
            yield return null;

            Assert.AreEqual(0, LiveAdvanceSteps());
        }

        [UnityTest]
        public IEnumerator IFixedStepTickSource_CustomImplementation() {
            var source = new StepCountingTickSource(TickRate.PerSecond(20));
            _clock = CoreKit.Time.StartClock(source, ObservationContext.Discard);
            var start = _clock.Current;

            for (var step = 0; step < 10; step++) {
                yield return new WaitForFixedUpdate();
            }

            Assert.GreaterOrEqual(source.Steps, 10);
            Assert.AreEqual((ulong)source.Steps, start.Until(_clock.Current).Steps);
            Assert.AreSame(source, _clock.Source);
        }

        private sealed class StepCountingTickSource : IFixedStepTickSource {
            public StepCountingTickSource(TickRate rate) {
                Rate = rate;
            }

            public TickRate Rate { get; }

            public int Steps { get; private set; }

            public void FixedStep(double stepSeconds) {
                Steps++;
            }

            public Tick Current() => new Tick((ulong)Steps);
        }

        private static int LiveAdvanceSteps() {
            var count = 0;

            foreach (var behaviour in Resources.FindObjectsOfTypeAll<MonoBehaviour>()) {
                if (behaviour != null && behaviour.GetType().Name == AdvanceStepTypeName) {
                    count++;
                }
            }

            return count;
        }
    }
}
