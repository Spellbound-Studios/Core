// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Spellbound.Core.Logging;
using Spellbound.Core.Observations;
using Spellbound.Core.Simulation;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Spellbound.Core.Tests {
    public class ObservationContextTests {
        private static readonly Counter Rescues = CoreKit.Observations.Counter("contexttests.containment.rescues",
            "A body was moved out of terrain. The reason says why.");

        private static readonly Occurrence Bound = CoreKit.Observations.Occurrence("contexttests.reference_frame.bound",
            "A body started riding a reference frame.");

        private static readonly Gauge RowsAwake = CoreKit.Observations.Gauge("contexttests.index.rows_awake",
            "Rows awake in the store.", ObservationUnit.Named("rows"));

        private static readonly Cost AwakenJob = CoreKit.Observations.Cost("contexttests.index.awaken_job",
            "The awakening job's milliseconds.", ObservationUnit.Milliseconds, 0.5, 1);

        private static readonly Gauge OtherPackage = CoreKit.Observations.Gauge("othercontext.draw.visible",
            "Instances visible after culling.", ObservationUnit.Count);

        private readonly List<ObservationReading> _readings = new List<ObservationReading>(64);
        private readonly List<ReasonCount> _reasons = new List<ReasonCount>(16);
        private readonly List<ObservationRatio> _ratios = new List<ObservationRatio>(16);
        private ObservationContext _context;

        [SetUp]
        public void CreateContext() {
            _context = CoreKit.Observations.CreateContext("tests_" + Guid.NewGuid().ToString("N").Substring(0, 8));
        }

        [TearDown]
        public void DisposeContext() {
            _context.Dispose();
        }

        [Test]
        public void ANameIsRequired() {
            Assert.Throws<ArgumentException>(() => CoreKit.Observations.CreateContext(" "));
        }

        [Test]
        public void ACounterAddsAndReads() {
            Rescues.Add(_context);
            Rescues.Add(_context, 3);
            Assert.IsTrue(_context.TryRead(Rescues.Key, out var reading));
            Assert.AreEqual(ObservationKind.Counter, reading.Kind);
            Assert.AreEqual(4, reading.Value);
            Assert.AreEqual(4, reading.Total);
            Assert.AreEqual(3, reading.Peak);
            Assert.AreEqual(2, reading.Count);
            Assert.AreEqual(ObservationUnit.Count, reading.Unit);
            Assert.IsFalse(reading.HasBudget);
        }

        [Test]
        public void ACounterCountsByReason() {
            var embedded = CoreKit.Observations.Reason("embedded");
            var belowFloor = CoreKit.Observations.Reason("below_floor");
            Rescues.Add(_context, embedded);
            Rescues.Add(_context, embedded);
            Rescues.Add(_context, belowFloor);
            Assert.AreEqual(2, _context.ReadReasons(Rescues.Key, _reasons));
            var byName = new Dictionary<string, long>();

            foreach (var entry in _reasons) {
                byName[entry.Reason.Name] = entry.Count;
            }

            Assert.AreEqual(2, byName["embedded"]);
            Assert.AreEqual(1, byName["below_floor"]);
            Assert.IsTrue(_context.TryRead(Rescues.Key, out var reading));
            Assert.AreEqual(3, reading.Value);
        }

        [Test]
        public void AnOccurrenceCountsItsRecords() {
            Bound.Record(_context, new Tick(40), 7);
            Bound.Record(_context, new Tick(41), 7);
            Assert.IsTrue(_context.TryRead(Bound.Key, out var reading));
            Assert.AreEqual(ObservationKind.Occurrence, reading.Kind);
            Assert.AreEqual(2, reading.Value);
            Assert.AreEqual(2, reading.Count);
        }

        [Test]
        public void AGaugeHoldsTheLastValueAndItsPeak() {
            RowsAwake.Set(_context, 120);
            RowsAwake.Set(_context, 80);
            Assert.IsTrue(_context.TryRead(RowsAwake.Key, out var reading));
            Assert.AreEqual(ObservationKind.Gauge, reading.Kind);
            Assert.AreEqual(80, reading.Value);
            Assert.AreEqual(120, reading.Peak);
            Assert.AreEqual(2, reading.Count);
            Assert.AreEqual("rows", reading.Unit.Name);
        }

        [Test]
        public void ACostRecordsLastTotalPeakAndBudget() {
            AwakenJob.Record(_context, 0.2);
            AwakenJob.Record(_context, 0.7);
            AwakenJob.Record(_context, 0.3);
            Assert.IsTrue(_context.TryRead(AwakenJob.Key, out var reading));
            Assert.AreEqual(ObservationKind.Cost, reading.Kind);
            Assert.AreEqual(0.3, reading.Value, 1e-9);
            Assert.AreEqual(1.2, reading.Total, 1e-9);
            Assert.AreEqual(0.7, reading.Peak, 1e-9);
            Assert.AreEqual(3, reading.Count);
            Assert.AreEqual(0.5, reading.Budget);
            Assert.IsTrue(reading.HasBudget);
            Assert.IsFalse(reading.OverBudget);
            Assert.AreEqual(1, reading.QualityLevel);
        }

        [Test]
        public void AMeasureScopeRecordsElapsedMilliseconds() {
            using (AwakenJob.Measure(_context)) {
                var spin = 0;

                for (var i = 0; i < 1000; i++) {
                    spin += i;
                }

                Assert.Greater(spin, 0);
            }

            Assert.IsTrue(_context.TryRead(AwakenJob.Key, out var reading));
            Assert.AreEqual(1, reading.Count);
            Assert.GreaterOrEqual(reading.Value, 0);
            Assert.Less(reading.Value, 1000);
        }

        [Test]
        public void TheVersionChangesOnlyWhenTheKeyChanges() {
            Assert.AreEqual(0u, _context.Version(RowsAwake.Key));
            RowsAwake.Set(_context, 5);
            var afterFirst = _context.Version(RowsAwake.Key);
            Assert.Greater(afterFirst, 0u);
            RowsAwake.Set(_context, 5);
            Assert.AreEqual(afterFirst, _context.Version(RowsAwake.Key));
            RowsAwake.Set(_context, 6);
            Assert.Greater(_context.Version(RowsAwake.Key), afterFirst);
            var beforeBudget = _context.Version(RowsAwake.Key);
            _context.SetBudget(RowsAwake.Key, 10);
            Assert.Greater(_context.Version(RowsAwake.Key), beforeBudget);
            var beforeLevel = _context.Version(RowsAwake.Key);
            _context.SetQualityLevel(RowsAwake.Key, 2);
            Assert.Greater(_context.Version(RowsAwake.Key), beforeLevel);
            Assert.AreEqual(_context.Version(RowsAwake.Key), ReadOne(RowsAwake.Key).Version);
        }

        [Test]
        public void ThePackageVersionChangesWithAnyOfItsKeys() {
            Assert.AreEqual(0u, _context.PackageVersion("contexttests"));
            Rescues.Add(_context);
            var afterRescue = _context.PackageVersion("contexttests");
            Assert.Greater(afterRescue, 0u);
            OtherPackage.Set(_context, 3);
            Assert.AreEqual(afterRescue, _context.PackageVersion("contexttests"));
            Assert.Greater(_context.PackageVersion("othercontext"), 0u);
            RowsAwake.Set(_context, 1);
            Assert.Greater(_context.PackageVersion("contexttests"), afterRescue);
        }

        [Test]
        public void ReadFiltersByPackageAndSkipsKeysWithoutAReading() {
            Rescues.Add(_context);
            OtherPackage.Set(_context, 3);
            _context.SetBudget(AwakenJob.Key, 0.9);
            Assert.AreEqual(1, _context.Read("contexttests", _readings));
            Assert.AreEqual(Rescues.Key, _readings[0].Key);
            Assert.AreEqual(2, _context.Read(_readings));
            Assert.AreEqual(0, _context.Read("nothing", _readings));
        }

        [Test]
        public void BudgetAndLevelOverridesWinOverTheDeclaration() {
            Assert.AreEqual(0.5, _context.Budget(AwakenJob.Key));
            Assert.AreEqual(1, _context.QualityLevel(AwakenJob.Key));
            _context.SetBudget(AwakenJob.Key, 0.9);
            _context.SetQualityLevel(AwakenJob.Key, 3);
            Assert.AreEqual(0.9, _context.Budget(AwakenJob.Key));
            Assert.AreEqual(3, _context.QualityLevel(AwakenJob.Key));
            AwakenJob.Record(_context, 1.0);
            var reading = ReadOne(AwakenJob.Key);
            Assert.AreEqual(0.9, reading.Budget);
            Assert.AreEqual(3, reading.QualityLevel);
            Assert.IsTrue(reading.OverBudget);
        }

        [Test]
        public void ASettingsAssetAppliesItsOverridesOnCreate() {
            var settings = ScriptableObject.CreateInstance<ObservationSettings>();
            settings.budgets.Add(new ObservationBudgetOverride { key = AwakenJob.Key.Name, budget = 0.25 });
            settings.qualityLevels.Add(new ObservationQualityLevelOverride { key = RowsAwake.Key.Name, level = 4 });
            settings.budgets.Add(new ObservationBudgetOverride { key = "not a key", budget = 1 });

            try {
                using var quiet = Log.SuspendSinks();
                using var context = CoreKit.Observations.CreateContext("settings_world", settings);
                Assert.AreEqual(0.25, context.Budget(AwakenJob.Key));
                Assert.AreEqual(4, context.QualityLevel(RowsAwake.Key));
                Assert.AreEqual(0.5, _context.Budget(AwakenJob.Key));
            }
            finally {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void DiscardRecordsNothingAndHoldsNoState() {
            var discard = ObservationContext.Discard;
            Assert.IsTrue(discard.IsDiscard);
            Rescues.Add(discard);
            RowsAwake.Set(discard, 9);
            AwakenJob.Record(discard, 2);
            Bound.Record(discard, new Tick(1), 1);
            discard.SetBudget(AwakenJob.Key, 7);
            Assert.AreEqual(0, discard.Read(_readings));
            Assert.IsFalse(discard.TryRead(Rescues.Key, out _));
            Assert.AreEqual(0u, discard.Version(Rescues.Key));
            Assert.AreEqual(0.5, discard.Budget(AwakenJob.Key));
            Assert.AreEqual(1, discard.QualityLevel(AwakenJob.Key));
            discard.Dispose();
            Assert.IsFalse(discard.IsDisposed);
            CollectionAssert.DoesNotContain(ObservationContexts.All, discard);
        }

        [Test]
        public void AnOverrideSetBeforeTheKeyIsDeclaredStillLearnsTheDefaults() {
            var key = ObservationKey.Parse("contexttests.late.declared");
            _context.SetQualityLevel(key, 3);
            Assert.AreEqual(3, _context.QualityLevel(key));
            Assert.IsTrue(double.IsNaN(_context.Budget(key)));
            var late = CoreKit.Observations.Cost("contexttests.late.declared", "Declared after the override.",
                ObservationUnit.Bytes, 0.7, 1);
            Assert.AreEqual(0.7, _context.Budget(key));
            Assert.AreEqual(3, _context.QualityLevel(key));
            late.Record(_context, 2);
            Assert.IsTrue(_context.TryRead(key, out var reading));
            Assert.AreEqual(ObservationKind.Cost, reading.Kind);
            Assert.AreEqual(ObservationUnit.Bytes, reading.Unit);
            Assert.AreEqual(0.7, reading.Budget);
        }

        [Test]
        public void ANegativeAmountIsRefusedAndAZeroAmountStillShowsTheKey() {
            Assert.Throws<ArgumentOutOfRangeException>(() => Rescues.Add(_context, -1));
            Rescues.Add(_context, 0);
            Assert.IsTrue(_context.TryRead(Rescues.Key, out var reading));
            Assert.AreEqual(0, reading.Value);
            Assert.Greater(reading.Version, 0u);
            Assert.Greater(_context.PackageVersion("contexttests"), 0u);
        }

        [Test]
        public void ALiveNameOrTheDiscardNameIsRefused() {
            Assert.Throws<ArgumentException>(() => CoreKit.Observations.CreateContext(_context.Name));
            Assert.Throws<ArgumentException>(() => CoreKit.Observations.CreateContext("discard"));
            _context.Dispose();
            using var again = CoreKit.Observations.CreateContext(_context.Name);
            Assert.AreEqual(_context.Name, again.Name);
        }

        [Test]
        public void TwoContextsNeverShareAReading() {
            using var other = CoreKit.Observations.CreateContext("other_world");
            Rescues.Add(_context);
            Rescues.Add(_context);
            Rescues.Add(other);
            other.SetBudget(AwakenJob.Key, 0.1);
            Assert.AreEqual(2, ReadOne(Rescues.Key).Value);
            Assert.IsTrue(other.TryRead(Rescues.Key, out var otherReading));
            Assert.AreEqual(1, otherReading.Value);
            Assert.AreEqual(0.5, _context.Budget(AwakenJob.Key));
        }

        [Test]
        public void TheLiveListFollowsCreateAndDispose() {
            CollectionAssert.Contains(ObservationContexts.All, _context);
            var created = CoreKit.Observations.CreateContext("short_lived");
            CollectionAssert.Contains(ObservationContexts.All, created);
            Assert.IsFalse(ObservationContexts.All is List<ObservationContext>);
            created.Dispose();
            Assert.IsTrue(created.IsDisposed);
            CollectionAssert.DoesNotContain(ObservationContexts.All, created);
            created.Dispose();
        }

        [Test]
        public void CompareGivesTheRatioPerKey() {
            Rescues.Add(_context, 4);
            RowsAwake.Set(_context, 10);
            var before = new List<ObservationReading>();
            _context.Read(before);
            Rescues.Add(_context, 4);
            RowsAwake.Set(_context, 5);
            OtherPackage.Set(_context, 1);
            var after = new List<ObservationReading>();
            _context.Read(after);
            Assert.AreEqual(2, CoreKit.Observations.Compare(before, after, _ratios));

            foreach (var ratio in _ratios) {
                if (ratio.Key == Rescues.Key) {
                    Assert.AreEqual(2.0, ratio.Ratio, 1e-9);
                }
                else {
                    Assert.AreEqual(RowsAwake.Key, ratio.Key);
                    Assert.AreEqual(0.5, ratio.Ratio, 1e-9);
                }
            }
        }

        [Test]
        public void Read_NoAllocation() {
            Rescues.Add(_context);
            RowsAwake.Set(_context, 1);
            AwakenJob.Record(_context, 0.1);
            AssertNoAllocation(() => { _context.Read(_readings); });
        }

        [Test]
        public void ReadOfOnePackage_NoAllocation() {
            Rescues.Add(_context);
            RowsAwake.Set(_context, 1);
            AwakenJob.Record(_context, 0.1);
            AssertNoAllocation(() => { _context.Read("contexttests", _readings); });
        }

        [Test]
        public void TryRead_NoAllocation() {
            Rescues.Add(_context);
            AssertNoAllocation(() => { _context.TryRead(Rescues.Key, out _); });
        }

        private static void AssertNoAllocation(TestDelegate action) {
            for (var warm = 0; warm < 3; warm++) {
                action();
            }

            var constraint = ((IResolveConstraint)Is.Not.AllocatingGCMemory()).Resolve();
            constraint.ApplyTo(action);
            Assert.That(action, constraint);
        }

        [Test]
        public void Report_NoAllocation() {
            var reason = CoreKit.Observations.Reason("warm");
            Rescues.Add(_context, reason);
            RowsAwake.Set(_context, 1);
            AwakenJob.Record(_context, 0.1);
            Bound.Record(_context, new Tick(1), 1);
            AssertNoAllocation(() => Rescues.Add(_context, reason));
            AssertNoAllocation(() => RowsAwake.Set(_context, 2));
            AssertNoAllocation(() => AwakenJob.Record(_context, 0.2));
            AssertNoAllocation(() => Bound.Record(_context, new Tick(2), 1));
        }

        private ObservationReading ReadOne(ObservationKey key) {
            Assert.IsTrue(_context.TryRead(key, out var reading));
            return reading;
        }
    }
}
