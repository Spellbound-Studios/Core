// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Spellbound.Core.Observations;
using Spellbound.Core.Simulation;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;
using Object = UnityEngine.Object;

namespace Spellbound.Core.Tests {
    public class GuardCheckTests {
        private const ulong Subject = 7;
        private const ulong WindowTicks = 100;
        private const ulong HoldTicks = 250;

        private static readonly Guard Embedded = CoreKit.Observations.Guard("guardchecktests.containment.embedded",
            "A body was found inside solid terrain and was moved out.");

        private static readonly Guard Patient = CoreKit.Observations.Guard("guardchecktests.containment.patient",
            "A body was found inside solid terrain; this guard repairs it without end.",
            new GuardPolicyValues(int.MaxValue, 2, 5));

        private readonly List<ReasonCount> _reasons = new List<ReasonCount>(8);
        private readonly List<ObservationContext> _contexts = new List<ObservationContext>(4);
        private readonly List<Object> _assets = new List<Object>(4);
        private ObservationContext _context;

        [SetUp]
        public void CreateContext() {
            _context = Create(null, TickRate.PerSecond(50));
        }

        [TearDown]
        public void DisposeContexts() {
            foreach (var context in _contexts) {
                context.Dispose();
            }

            _contexts.Clear();

            foreach (var asset in _assets) {
                Object.DestroyImmediate(asset);
            }

            _assets.Clear();
        }

        [Test]
        public void AHoldingCheckAnswersHoldsAndCountsNothing() {
            Assert.AreEqual(GuardVerdict.Holds, Embedded.Check(_context, true, Subject, new Tick(0)));
            Assert.IsFalse(_context.TryRead(Embedded.Key, out _));
            Assert.AreEqual(0, _context.GuardEntryCount);
        }

        [Test]
        public void TheDiscardContextAlwaysHolds() {
            for (var tick = 0ul; tick < 5; tick++) {
                Assert.AreEqual(GuardVerdict.Holds, Embedded.Check(ObservationContext.Discard, false, Subject,
                    new Tick(tick)));
            }

            Assert.AreEqual(0, ObservationContext.Discard.GuardEntryCount);
        }

        [Test]
        public void TwoRepairsInsideTheWindowAndTheThirdHolds() {
            Assert.AreEqual(GuardVerdict.Repair, Embedded.Check(_context, false, Subject, new Tick(0)));
            Assert.AreEqual(GuardVerdict.Repair, Embedded.Check(_context, false, Subject, new Tick(1)));
            Assert.AreEqual(GuardVerdict.Hold, Embedded.Check(_context, false, Subject, new Tick(2)));
            Assert.AreEqual(1, _context.GuardEntryCount);
        }

        [Test]
        public void AFailingCheckDuringTheHoldAnswersHold() {
            Trip(_context, 0);
            Assert.AreEqual(GuardVerdict.Hold, Embedded.Check(_context, false, Subject, new Tick(3)));
            Assert.AreEqual(GuardVerdict.Hold, Embedded.Check(_context, false, Subject, new Tick(2 + HoldTicks - 1)));
        }

        [Test]
        public void AfterTheHoldAndTheWindowTheEntryIsGone() {
            Trip(_context, 0);
            Assert.AreEqual(GuardVerdict.Holds, Embedded.Check(_context, true, Subject, new Tick(50)));
            Assert.AreEqual(1, _context.GuardEntryCount);
            Assert.AreEqual(GuardVerdict.Holds, Embedded.Check(_context, true, Subject, new Tick(2 + HoldTicks)));
            Assert.AreEqual(0, _context.GuardEntryCount);
        }

        [Test]
        public void AHealthySubjectAfterTheWindowLeavesNoEntry() {
            Assert.AreEqual(GuardVerdict.Repair, Embedded.Check(_context, false, Subject, new Tick(0)));
            Assert.AreEqual(GuardVerdict.Holds, Embedded.Check(_context, true, Subject, new Tick(WindowTicks - 1)));
            Assert.AreEqual(1, _context.GuardEntryCount);
            Assert.AreEqual(GuardVerdict.Holds, Embedded.Check(_context, true, Subject, new Tick(WindowTicks)));
            Assert.AreEqual(0, _context.GuardEntryCount);
        }

        [Test]
        public void AFailingCheckAfterTheWindowRestartsAtOneRepair() {
            Assert.AreEqual(GuardVerdict.Repair, Embedded.Check(_context, false, Subject, new Tick(0)));
            Assert.AreEqual(GuardVerdict.Repair, Embedded.Check(_context, false, Subject, new Tick(1)));
            Assert.AreEqual(GuardVerdict.Repair, Embedded.Check(_context, false, Subject, new Tick(WindowTicks)));
            Assert.AreEqual(GuardVerdict.Repair, Embedded.Check(_context, false, Subject, new Tick(WindowTicks + 1)));
            Assert.AreEqual(GuardVerdict.Hold, Embedded.Check(_context, false, Subject, new Tick(WindowTicks + 2)));
        }

        [Test]
        public void TheCounterReadsTheTripsWithTheirReasons() {
            Trip(_context, 0);
            Embedded.Check(_context, false, Subject, new Tick(3));
            Assert.IsTrue(_context.TryRead(Embedded.Key, out var reading));
            Assert.AreEqual(4.0, reading.Value);
            Assert.AreEqual(4, reading.Count);
            _context.ReadReasons(Embedded.Key, _reasons);
            Assert.AreEqual(3, ReasonTotal("repair"));
            Assert.AreEqual(1, ReasonTotal("hold"));
        }

        [Test]
        public void ThePolicyFromTheSettingsOverridesTheDefaults() {
            var policy = ScriptableObject.CreateInstance<GuardPolicy>();
            policy.repairs = 1;
            policy.windowSeconds = 1;
            policy.holdSeconds = 1;
            var settings = ScriptableObject.CreateInstance<ObservationSettings>();
            settings.guardPolicies.Add(new ObservationGuardPolicyOverride {
                key = Embedded.Key.Name,
                policy = policy
            });
            _assets.Add(policy);
            _assets.Add(settings);
            var overridden = Create(settings, TickRate.PerSecond(50));

            Assert.AreEqual(GuardVerdict.Hold, Embedded.Check(overridden, false, Subject, new Tick(0)));
            Assert.AreEqual(GuardVerdict.Hold, Embedded.Check(overridden, false, Subject, new Tick(49)));
            Assert.AreEqual(GuardVerdict.Holds, Embedded.Check(overridden, true, Subject, new Tick(50)));
            Assert.AreEqual(0, overridden.GuardEntryCount);
            Assert.AreEqual(GuardVerdict.Repair, Embedded.Check(_context, false, Subject, new Tick(0)));
        }

        [Test]
        public void ContextsAtTwentyAndFiftyHoldForTheSameSeconds() {
            var slow = Create(null, TickRate.PerSecond(20));
            var fast = Create(null, TickRate.PerSecond(50));
            AssertHoldLasts(slow, 100);
            AssertHoldLasts(fast, 250);
        }

        [Test]
        public void GuardCheck_NoAllocation() {
            Trip(_context, 0);
            Patient.Check(_context, false, Subject, new Tick(0));
            AssertNoAllocation(() => {
                for (var i = 0; i < 1000; i++) {
                    Embedded.Check(_context, false, Subject, new Tick(3));
                }
            });
            AssertNoAllocation(() => {
                for (var i = 0; i < 1000; i++) {
                    Patient.Check(_context, false, Subject, new Tick(1));
                }
            });
            AssertNoAllocation(() => {
                for (var i = 0; i < 1000; i++) {
                    Embedded.Check(_context, true, Subject, new Tick(4));
                }
            });
        }

        private void AssertHoldLasts(ObservationContext context, ulong holdTicks) {
            Trip(context, 0);
            Assert.AreEqual(GuardVerdict.Hold, Embedded.Check(context, false, Subject, new Tick(2 + holdTicks - 1)));
            Assert.AreEqual(GuardVerdict.Holds, Embedded.Check(context, true, Subject, new Tick(2 + holdTicks)));
            Assert.AreEqual(0, context.GuardEntryCount);
        }

        private static void Trip(ObservationContext context, ulong from) {
            Assert.AreEqual(GuardVerdict.Repair, Embedded.Check(context, false, Subject, new Tick(from)));
            Assert.AreEqual(GuardVerdict.Repair, Embedded.Check(context, false, Subject, new Tick(from + 1)));
            Assert.AreEqual(GuardVerdict.Hold, Embedded.Check(context, false, Subject, new Tick(from + 2)));
        }

        private ObservationContext Create(ObservationSettings settings, TickRate rate) {
            var context = CoreKit.Observations.CreateContext(
                "guardcheck_" + Guid.NewGuid().ToString("N").Substring(0, 8), settings, rate);
            _contexts.Add(context);
            return context;
        }

        private long ReasonTotal(string name) {
            foreach (var reason in _reasons) {
                if (string.Equals(reason.Reason.Name, name, StringComparison.Ordinal)) {
                    return reason.Count;
                }
            }

            return 0;
        }

        private static void AssertNoAllocation(TestDelegate action) {
            for (var warm = 0; warm < 3; warm++) {
                action();
            }

            var constraint = ((IResolveConstraint)Is.Not.AllocatingGCMemory()).Resolve();
            constraint.ApplyTo(action);
            Assert.That(action, constraint);
        }
    }
}
