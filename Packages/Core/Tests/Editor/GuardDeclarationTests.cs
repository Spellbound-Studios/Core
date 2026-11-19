// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Linq;
using NUnit.Framework;
using Spellbound.Core.Observations;
using Spellbound.Core.ReferenceFrames;
using Spellbound.Core.Simulation;
using Spellbound.Core.Testing;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Spellbound.Core.Tests {
    public class GuardDeclarationTests {
        [Test]
        public void AGuardIsACounterMarkedAsAGuard() {
            var guard = CoreKit.Observations.Guard("guardtests.declaration.kind", "A guard.");

            Assert.AreEqual(ObservationKind.Counter, guard.Declaration.Kind);
            Assert.IsTrue(guard.Declaration.IsGuard);
            Assert.AreEqual("guardtests.declaration.kind", guard.Key.Name);
            Assert.AreEqual(GuardPolicyValues.Default.Repairs, guard.Defaults.Repairs);
            Assert.AreEqual(GuardPolicyValues.Default.WindowSeconds, guard.Defaults.WindowSeconds);
            Assert.AreEqual(GuardPolicyValues.Default.HoldSeconds, guard.Defaults.HoldSeconds);
            StringAssert.Contains("guardtests.declaration.kind", guard.ToString());
        }

        [Test]
        public void APlainCounterIsNotAGuard() {
            var counter = CoreKit.Observations.Counter("guardtests.declaration.plain", "A plain counter.");
            Assert.IsFalse(counter.Declaration.IsGuard);
        }

        [Test]
        public void AGuardKeepsTheDefaultsItIsDeclaredWith() {
            var guard = CoreKit.Observations.Guard("guardtests.declaration.defaults", "A guard with its own policy.",
                new GuardPolicyValues(4, 1.5, 6));

            Assert.AreEqual(4, guard.Defaults.Repairs);
            Assert.AreEqual(1.5, guard.Defaults.WindowSeconds);
            Assert.AreEqual(6.0, guard.Defaults.HoldSeconds);
        }

        [Test]
        public void TheSameGuardTwiceReturnsTheFirst() {
            var first = CoreKit.Observations.Guard("guardtests.duplicate.same", "Declared twice the same way.");
            var second = CoreKit.Observations.Guard("guardtests.duplicate.same", "Declared twice the same way.");
            Assert.AreSame(first.Declaration, second.Declaration);
        }

        [Test]
        public void AGuardKeyDeclaredAgainAsAPlainCounterThrows() {
            CoreKit.Observations.Guard("guardtests.duplicate.guard_first", "One key, two kinds.");
            Assert.Throws<ArgumentException>(() =>
                CoreKit.Observations.Counter("guardtests.duplicate.guard_first", "One key, two kinds."));
        }

        [Test]
        public void APlainCounterKeyDeclaredAgainAsAGuardThrows() {
            CoreKit.Observations.Counter("guardtests.duplicate.counter_first", "One key, two kinds.");
            Assert.Throws<ArgumentException>(() =>
                CoreKit.Observations.Guard("guardtests.duplicate.counter_first", "One key, two kinds."));
        }

        [Test]
        public void DeclaredListsTheReferenceFrameUpGuard() {
            var declared = CoreKit.Observations.Declared();
            var upVaries = declared.FirstOrDefault(declaration =>
                declaration.Key.Name == "core.reference_frame.up_varies");

            Assert.IsNotNull(upVaries);
            Assert.IsTrue(upVaries.IsGuard);
            Assert.AreEqual(ObservationKind.Counter, upVaries.Kind);
            Assert.AreSame(CoreObservations.ReferenceFrameUpVaries.Declaration, upVaries);
        }

        [Test]
        public void TheRecorderAndQueueDropCountersAreDeclared() {
            Assert.AreEqual("core.recorder.dropped", CoreObservations.RecorderDropped.Key.Name);
            Assert.AreEqual("core.queue.dropped", CoreObservations.QueueDropped.Key.Name);
            Assert.IsFalse(CoreObservations.RecorderDropped.Declaration.IsGuard);
            Assert.IsFalse(CoreObservations.QueueDropped.Declaration.IsGuard);

            var names = CoreKit.Observations.Declared().Select(declaration => declaration.Key.Name).ToList();
            CollectionAssert.Contains(names, "core.recorder.dropped");
            CollectionAssert.Contains(names, "core.queue.dropped");
        }

        [Test]
        public void TheDefaultPolicyIsThreeRepairsTwoSecondsAndFiveSeconds() {
            Assert.AreEqual(3, GuardPolicyValues.Default.Repairs);
            Assert.AreEqual(2.0, GuardPolicyValues.Default.WindowSeconds);
            Assert.AreEqual(5.0, GuardPolicyValues.Default.HoldSeconds);
        }

        [TestCase(0, 2.0, 5.0)]
        [TestCase(-1, 2.0, 5.0)]
        [TestCase(3, -0.5, 5.0)]
        [TestCase(3, 2.0, -1.0)]
        [TestCase(3, double.NaN, 5.0)]
        [TestCase(3, 2.0, double.PositiveInfinity)]
        public void PolicyValuesRefuseFewerThanOneRepairOrABadTime(int repairs, double windowSeconds,
            double holdSeconds) {
            Assert.Throws<ArgumentException>(() => new GuardPolicyValues(repairs, windowSeconds, holdSeconds));
        }

        [Test]
        public void PolicyValuesAcceptZeroSeconds() {
            var values = new GuardPolicyValues(1, 0, 0);
            Assert.AreEqual(1, values.Repairs);
            Assert.AreEqual(0.0, values.WindowSeconds);
            Assert.AreEqual(0.0, values.HoldSeconds);
        }

        [Test]
        public void ThePolicyAssetDefaultsToTheDefaultValues() {
            var policy = ScriptableObject.CreateInstance<GuardPolicy>();

            try {
                Assert.AreEqual(3, policy.repairs);
                Assert.AreEqual(2.0, policy.windowSeconds);
                Assert.AreEqual(5.0, policy.holdSeconds);
                Assert.AreEqual(GuardPolicyValues.Default.Repairs, policy.Values.Repairs);
                Assert.AreEqual(GuardPolicyValues.Default.WindowSeconds, policy.Values.WindowSeconds);
                Assert.AreEqual(GuardPolicyValues.Default.HoldSeconds, policy.Values.HoldSeconds);
            }
            finally {
                Object.DestroyImmediate(policy);
            }
        }

        [Test]
        public void ThePolicyAssetValuesReadItsFields() {
            var policy = ScriptableObject.CreateInstance<GuardPolicy>();

            try {
                policy.repairs = 7;
                policy.windowSeconds = 0.25;
                policy.holdSeconds = 12;
                Assert.AreEqual(7, policy.Values.Repairs);
                Assert.AreEqual(0.25, policy.Values.WindowSeconds);
                Assert.AreEqual(12.0, policy.Values.HoldSeconds);
            }
            finally {
                Object.DestroyImmediate(policy);
            }
        }

        [Test]
        public void TheVerdictsAreHoldsRepairAndHold() {
            Assert.AreEqual(typeof(byte), Enum.GetUnderlyingType(typeof(GuardVerdict)));
            Assert.AreEqual(0, (byte)GuardVerdict.Holds);
            Assert.AreEqual(1, (byte)GuardVerdict.Repair);
            Assert.AreEqual(2, (byte)GuardVerdict.Hold);
            Assert.AreEqual(3, Enum.GetValues(typeof(GuardVerdict)).Length);
        }

        [Test]
        public void CheckTakesTheContextTheResultTheSubjectAndTheTick() {
            var check = typeof(Guard).GetMethod(nameof(Guard.Check),
                new[] { typeof(ObservationContext), typeof(bool), typeof(ulong), typeof(Tick) });

            Assert.IsNotNull(check);
            Assert.AreEqual(typeof(GuardVerdict), check.ReturnType);
        }

        [Test]
        public void RequireConstantUpTakesTheContextTheTickTheFrameAndTheField() {
            var require = typeof(ReferenceFrameChecks).GetMethod(nameof(ReferenceFrameChecks.RequireConstantUp),
                new[] {
                    typeof(ObservationContext), typeof(Tick), typeof(ReferenceFrameId),
                    typeof(UpField).MakeByRefType()
                });

            Assert.IsNotNull(require);
            Assert.AreEqual(typeof(bool), require.ReturnType);
        }

        [Test]
        public void TheTripHelperTripsOneGuardOrEveryGuardOfAPackage() {
            var trip = typeof(GuardTripping).GetMethod(nameof(GuardTripping.Trip),
                new[] { typeof(Guard), typeof(ObservationContext), typeof(ulong), typeof(Tick) });
            var tripAll = typeof(GuardTripping).GetMethod(nameof(GuardTripping.TripAll),
                new[] { typeof(string), typeof(ObservationContext) });

            Assert.IsNotNull(trip);
            Assert.AreEqual(typeof(int), trip.ReturnType);
            Assert.IsNotNull(tripAll);
            Assert.AreEqual(typeof(int), tripAll.ReturnType);
        }
    }
}
