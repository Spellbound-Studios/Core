// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Spellbound.Core.Observations;
using Spellbound.Core.Simulation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Spellbound.Core.Tests {
    public class RecorderRateCapTests {
        private const string NoisyKey = "recorderratecaptests.body.slipped";
        private const string QuietKey = "recorderratecaptests.body.settled";
        private const ulong Subject = 11;

        private static readonly Occurrence Noisy = CoreKit.Observations.Occurrence(NoisyKey,
            "A body slipped on a slope.");

        private static readonly Occurrence Quiet = CoreKit.Observations.Occurrence(QuietKey,
            "A body came to rest.");

        private static readonly Guard Embedded = CoreKit.Observations.Guard(
            "recorderratecaptests.containment.embedded", "A body was found inside solid terrain and was moved out.");

        private readonly List<RecordedOccurrence> _records = new List<RecordedOccurrence>(16);
        private readonly List<ReasonCount> _reasons = new List<ReasonCount>(4);
        private readonly List<ObservationContext> _contexts = new List<ObservationContext>(2);
        private readonly List<Object> _assets = new List<Object>(2);

        [TearDown]
        public void DisposeEverything() {
            foreach (var context in _contexts) {
                context.Dispose();
            }

            foreach (var asset in _assets) {
                Object.DestroyImmediate(asset);
            }

            _contexts.Clear();
            _assets.Clear();
        }

        [Test]
        public void ACapOfTwoASecondAtFiftyTicksDropsTheSecondRecordInATick() {
            var context = Create(Cap(NoisyKey, 2));

            Noisy.Record(context, new Tick(10), Subject);
            Noisy.Record(context, new Tick(10), Subject);

            Assert.AreEqual(1, context.ReadRecords(_records));
            Assert.IsTrue(context.TryRead(CoreObservations.RecorderDropped.Key, out var dropped));
            Assert.AreEqual(1.0, dropped.Value);
            Assert.AreEqual(1, context.ReadReasons(CoreObservations.RecorderDropped.Key, _reasons));
            Assert.AreEqual(NoisyKey, CoreKit.Observations.ReasonName(_reasons[0].Reason.Id));
            Assert.AreEqual(1, _reasons[0].Count);
        }

        [Test]
        public void ACapOfTwoASecondAtFiftyTicksTakesOneRecordEveryTwentyFiveTicks() {
            var context = Create(Cap(NoisyKey, 2));

            Noisy.Record(context, new Tick(10), Subject);
            Noisy.Record(context, new Tick(11), Subject);
            Noisy.Record(context, new Tick(34), Subject);
            Noisy.Record(context, new Tick(35), Subject);

            Assert.AreEqual(2, context.ReadRecords(_records));
            Assert.AreEqual(new Tick(10), _records[0].Tick);
            Assert.AreEqual(new Tick(35), _records[1].Tick);
            Assert.IsTrue(context.TryRead(CoreObservations.RecorderDropped.Key, out var dropped));
            Assert.AreEqual(2.0, dropped.Value);
        }

        [Test]
        public void ACapAboveTheTickRateTakesSeveralRecordsInATick() {
            var context = Create(Cap(NoisyKey, 150));

            for (var i = 0; i < 5; i++) {
                Noisy.Record(context, new Tick(10), Subject);
            }

            Assert.AreEqual(3, context.ReadRecords(_records));
        }

        [Test]
        public void ACapOfZeroMeansNoCap() {
            var context = Create(Cap(NoisyKey, 0));

            for (var i = 0; i < 5; i++) {
                Noisy.Record(context, new Tick(10), Subject);
            }

            Assert.AreEqual(5, context.ReadRecords(_records));
            Assert.IsFalse(context.TryRead(CoreObservations.RecorderDropped.Key, out _));
        }

        [Test]
        public void ACapOnOneKeyLeavesAnotherAlone() {
            var context = Create(Cap(NoisyKey, 2));

            for (var i = 0; i < 4; i++) {
                Noisy.Record(context, new Tick(10), Subject);
                Quiet.Record(context, new Tick(10), Subject);
            }

            Assert.AreEqual(5, context.ReadRecords(_records));
        }

        [Test]
        public void ADroppedRecordStillCountsTheOccurrence() {
            var context = Create(Cap(NoisyKey, 2));

            Noisy.Record(context, new Tick(10), Subject);
            Noisy.Record(context, new Tick(10), Subject, 4UL);

            Assert.IsTrue(context.TryRead(Noisy.Key, out var reading));
            Assert.AreEqual(2, reading.Count);
        }

        [Test]
        public void ATripFlagsTheRecordsOfThePrecedingWindowAndNotTheOneBefore() {
            var context = Create(null);

            Quiet.Record(context, new Tick(50), 1UL);
            Quiet.Record(context, new Tick(99), 2UL);
            Quiet.Record(context, new Tick(150), 3UL);
            Quiet.Record(context, new Tick(190), 4UL);

            Assert.AreEqual(GuardVerdict.Repair, Embedded.Check(context, false, Subject, new Tick(198)));
            Assert.AreEqual(GuardVerdict.Repair, Embedded.Check(context, false, Subject, new Tick(199)));
            Assert.AreEqual(GuardVerdict.Hold, Embedded.Check(context, false, Subject, new Tick(200)));

            Quiet.Record(context, new Tick(201), 5UL);

            Assert.AreEqual(5, context.ReadRecords(_records));
            Assert.IsFalse(_records[0].Flagged);
            Assert.IsFalse(_records[1].Flagged);
            Assert.IsTrue(_records[2].Flagged);
            Assert.IsTrue(_records[3].Flagged);
            Assert.IsFalse(_records[4].Flagged);
        }

        [Test]
        public void ARepairThatDoesNotTripFlagsNothing() {
            var context = Create(null);

            Quiet.Record(context, new Tick(150), 1UL);
            Assert.AreEqual(GuardVerdict.Repair, Embedded.Check(context, false, Subject, new Tick(160)));

            Assert.AreEqual(1, context.ReadRecords(_records));
            Assert.IsFalse(_records[0].Flagged);
        }

        private ObservationSettings Cap(string key, double recordsPerSecond) {
            var settings = ScriptableObject.CreateInstance<ObservationSettings>();
            settings.rateCaps.Add(new ObservationRateCap { key = key, recordsPerSecond = recordsPerSecond });
            _assets.Add(settings);
            return settings;
        }

        private ObservationContext Create(ObservationSettings settings) {
            var context = CoreKit.Observations.CreateContext(
                "recorderratecap_" + Guid.NewGuid().ToString("N").Substring(0, 8), settings, TickRate.PerSecond(50));
            _contexts.Add(context);
            return context;
        }
    }
}
