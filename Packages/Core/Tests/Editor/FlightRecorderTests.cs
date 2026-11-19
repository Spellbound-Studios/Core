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

namespace Spellbound.Core.Tests {
    public class FlightRecorderTests {
        private const ulong FirstSpan = 1UL << 32;

        private static readonly Occurrence Landed = CoreKit.Observations.Occurrence("flightrecordertests.body.landed",
            "A body came down onto a surface.");

        private readonly List<RecordedOccurrence> _records = new List<RecordedOccurrence>(16);
        private readonly List<ObservationContext> _contexts = new List<ObservationContext>(2);
        private readonly List<FlightRecorder> _recorders = new List<FlightRecorder>(2);
        private readonly List<ObservationSettings> _settings = new List<ObservationSettings>(1);

        [TearDown]
        public void DisposeEverything() {
            foreach (var recorder in _recorders) {
                recorder.Dispose();
            }

            foreach (var context in _contexts) {
                context.Dispose();
            }

            foreach (var settings in _settings) {
                UnityEngine.Object.DestroyImmediate(settings);
            }

            _recorders.Clear();
            _contexts.Clear();
            _settings.Clear();
        }

        [Test]
        public void OneRecordReadsBackWithItsKeyTickAndSubject() {
            var recorder = CreateRecorder(64);

            recorder.Write(0xCAFEF00Du, new Tick(1234), 0x0102030405060708UL, ReadOnlySpan<uint>.Empty);

            Assert.AreEqual(1, recorder.Read(_records));
            Assert.AreEqual(1, _records.Count);
            Assert.AreEqual(0xCAFEF00Du, _records[0].KeyHash);
            Assert.AreEqual(new Tick(1234), _records[0].Tick);
            Assert.AreEqual(0x0102030405060708UL, _records[0].Subject);
            Assert.AreEqual(0, _records[0].ValueCount);
            Assert.IsFalse(_records[0].Flagged);
            Assert.AreEqual(1, recorder.Count);
            Assert.AreEqual(new Tick(1234), recorder.LatestTick);
        }

        [Test]
        public void ThreeValuesMakeOneArgumentCell() {
            var recorder = CreateRecorder(64);

            recorder.Write(1u, new Tick(1), 2UL, new uint[] { 10, 20, 30 });

            Assert.AreEqual(2, recorder.UsedCells);
            Assert.AreEqual(1, recorder.Read(_records));
            Assert.AreEqual(3, _records[0].ValueCount);
            Assert.AreEqual(10u, _records[0].Value(0));
            Assert.AreEqual(20u, _records[0].Value(1));
            Assert.AreEqual(30u, _records[0].Value(2));
        }

        [Test]
        public void FiveValuesMakeTwoArgumentCells() {
            var recorder = CreateRecorder(64);

            recorder.Write(1u, new Tick(1), 2UL, new uint[] { 1, 2, 3, 4, 5 });

            Assert.AreEqual(3, recorder.UsedCells);
            Assert.AreEqual(1, recorder.Read(_records));
            Assert.AreEqual(5, _records[0].ValueCount);

            for (var i = 0; i < 5; i++) {
                Assert.AreEqual((uint)(i + 1), _records[0].Value(i));
            }
        }

        [Test]
        public void TwelveValuesFillThreeArgumentCellsAndThirteenAreRefused() {
            var recorder = CreateRecorder(64);
            var twelve = new uint[12];
            var thirteen = new uint[13];

            recorder.Write(1u, new Tick(1), 2UL, twelve);

            Assert.AreEqual(4, recorder.UsedCells);
            Assert.Throws<ArgumentException>(() => recorder.Write(1u, new Tick(2), 2UL, thirteen));
            Assert.AreEqual(1, recorder.Count);
        }

        [Test]
        public void ARingOfEightCellsAfterTenHeaderWritesHoldsTheNewestEight() {
            var recorder = CreateRecorder(8);

            for (var i = 0; i < 10; i++) {
                recorder.Write(5u, new Tick((ulong)i), (ulong)i, ReadOnlySpan<uint>.Empty);
            }

            Assert.AreEqual(8, recorder.Count);
            Assert.AreEqual(8, recorder.Read(_records));

            for (var i = 0; i < 8; i++) {
                Assert.AreEqual(new Tick((ulong)(i + 2)), _records[i].Tick);
                Assert.AreEqual((ulong)(i + 2), _records[i].Subject);
            }
        }

        [Test]
        public void AWrappingRecordDropsWholeOlderRecordsAndKeepsItsValues() {
            var recorder = CreateRecorder(8);

            for (var i = 0; i < 5; i++) {
                recorder.Write(5u, new Tick((ulong)i), (ulong)i, new uint[] { (uint)i, (uint)i + 100 });
            }

            Assert.AreEqual(4, recorder.Count);
            Assert.AreEqual(4, recorder.Read(_records));

            for (var i = 0; i < 4; i++) {
                Assert.AreEqual(new Tick((ulong)(i + 1)), _records[i].Tick);
                Assert.AreEqual(2, _records[i].ValueCount);
                Assert.AreEqual((uint)(i + 1), _records[i].Value(0));
                Assert.AreEqual((uint)(i + 101), _records[i].Value(1));
            }
        }

        [Test]
        public void TicksOnBothSidesOfATwoToTheThirtyTwoBoundaryReconstructExactly() {
            var recorder = CreateRecorder(16);
            var before = new Tick(FirstSpan - 3);
            var after = new Tick(FirstSpan + 5);

            recorder.Write(9u, before, 1UL, ReadOnlySpan<uint>.Empty);
            recorder.Write(9u, after, 2UL, ReadOnlySpan<uint>.Empty);

            Assert.AreEqual(after, recorder.LatestTick);
            Assert.AreEqual(2, recorder.Read(_records));
            Assert.AreEqual(before, _records[0].Tick);
            Assert.AreEqual(after, _records[1].Tick);
        }

        [Test]
        public void ARecordFromTheSameSpanAsTheLatestKeepsItsHighPart() {
            var recorder = CreateRecorder(16);
            var early = new Tick(3 * FirstSpan + 7);
            var late = new Tick(3 * FirstSpan + 900);

            recorder.Write(9u, early, 1UL, ReadOnlySpan<uint>.Empty);
            recorder.Write(9u, late, 2UL, ReadOnlySpan<uint>.Empty);

            Assert.AreEqual(2, recorder.Read(_records));
            Assert.AreEqual(early, _records[0].Tick);
            Assert.AreEqual(late, _records[1].Tick);
        }

        [Test]
        public void FlagWindowMarksOnlyTheRecordsInsideIt() {
            var recorder = CreateRecorder(16);

            for (var i = 0; i < 6; i++) {
                recorder.Write(3u, new Tick((ulong)(i * 10)), 0UL, new uint[] { (uint)i });
            }

            recorder.FlagWindow(new Tick(15), new Tick(30));

            Assert.AreEqual(6, recorder.Read(_records));
            Assert.IsFalse(_records[0].Flagged);
            Assert.IsFalse(_records[1].Flagged);
            Assert.IsTrue(_records[2].Flagged);
            Assert.IsTrue(_records[3].Flagged);
            Assert.IsFalse(_records[4].Flagged);
            Assert.IsFalse(_records[5].Flagged);
            Assert.AreEqual(3u, _records[3].Value(0));
        }

        [Test]
        public void ReadReplacesWhatTheListHeld() {
            var recorder = CreateRecorder(16);
            recorder.Write(3u, new Tick(1), 0UL, ReadOnlySpan<uint>.Empty);
            recorder.Read(_records);

            Assert.AreEqual(1, recorder.Read(_records));
            Assert.AreEqual(1, _records.Count);
        }

        [Test]
        public void ADisposedRecorderReadsNothing() {
            var recorder = CreateRecorder(16);
            recorder.Write(3u, new Tick(1), 0UL, ReadOnlySpan<uint>.Empty);
            recorder.Dispose();

            Assert.AreEqual(0, recorder.Count);
            Assert.AreEqual(0, recorder.Read(_records));
            Assert.DoesNotThrow(() => recorder.Write(3u, new Tick(2), 0UL, ReadOnlySpan<uint>.Empty));
        }

        [Test]
        public void TheDefaultSizeFromSettingsIsThirtyTwoThousandSevenHundredSixtyEightCells() {
            var settings = ScriptableObject.CreateInstance<ObservationSettings>();
            _settings.Add(settings);
            var context = CreateContext(settings);

            Assert.AreEqual(32768, ObservationSettings.DefaultRecorderCells);
            Assert.AreEqual(32768, settings.recorderCells);
            Assert.AreEqual(32768, context.Recorder.Capacity);
            Assert.AreEqual(32768, CreateContext(null).Recorder.Capacity);
        }

        [Test]
        public void TheSettingsSizeTheRing() {
            var settings = ScriptableObject.CreateInstance<ObservationSettings>();
            settings.recorderCells = 128;
            _settings.Add(settings);

            Assert.AreEqual(128, CreateContext(settings).Recorder.Capacity);
        }

        [Test]
        public void TheDiscardContextHoldsNoRecords() {
            Landed.Record(ObservationContext.Discard, new Tick(1), 1UL);

            Assert.IsNull(ObservationContext.Discard.Recorder);
            Assert.AreEqual(0, ObservationContext.Discard.RecordCount);
            Assert.AreEqual(0, ObservationContext.Discard.ReadRecords(_records));
            Assert.AreEqual(0, _records.Count);
        }

        [Test]
        public void DisposingTheContextDisposesItsRing() {
            var context = CreateContext(null);
            Landed.Record(context, new Tick(1), 1UL);
            Assert.AreEqual(1, context.RecordCount);

            context.Dispose();

            Assert.AreEqual(0, context.RecordCount);
            Assert.AreEqual(0, context.ReadRecords(_records));
        }

        [Test]
        public void OccurrenceRecord_NoAllocation() {
            var context = CreateContext(null);
            var values = new uint[] { 1, 2, 3, 4, 5 };

            AssertNoAllocation(() => {
                for (var i = 0; i < 10000; i++) {
                    var tick = new Tick((ulong)i);
                    Landed.Record(context, tick, 3UL);
                    Landed.Record(context, tick, 3UL, 0x1122334455667788UL);
                    Landed.Record(context, tick, 3UL, values);
                }
            });

            Assert.Greater(context.RecordCount, 0);
        }

        private FlightRecorder CreateRecorder(int cells) {
            var recorder = new FlightRecorder(cells);
            _recorders.Add(recorder);
            return recorder;
        }

        private ObservationContext CreateContext(ObservationSettings settings) {
            var context = CoreKit.Observations.CreateContext(
                "flightrecorder_" + Guid.NewGuid().ToString("N").Substring(0, 8), settings);
            _contexts.Add(context);
            return context;
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
