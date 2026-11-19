// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Diagnostics;
using System.Globalization;
using NUnit.Framework;
using Spellbound.Core.Observations;
using Spellbound.Core.Simulation;
using Unity.Collections.LowLevel.Unsafe;

namespace Spellbound.Core.Tests {
    [Category("Measurement")]
    public class ObservationCostMeasurementTests {
        private const int WarmCalls = 10_000;
        private const int TimedCalls = 1_000_000;
        private const double TargetNanoseconds = 1000;
        private const ulong Subject = 11;

        private static readonly Occurrence Landed = CoreKit.Observations.Occurrence(
            "observationcostmeasurementtests.body.landed", "A body landed on the ground.");

        private static readonly Counter Stepped = CoreKit.Observations.Counter(
            "observationcostmeasurementtests.body.stepped", "A body took one step.");

        private static readonly Guard Embedded = CoreKit.Observations.Guard(
            "observationcostmeasurementtests.containment.embedded",
            "A body was found inside solid terrain and was moved out.", new GuardPolicyValues(3, 2, 5));

        private ObservationContext _context;

        [SetUp]
        public void CreateContext() {
            _context = CoreKit.Observations.CreateContext(
                "observationcost_" + Guid.NewGuid().ToString("N").Substring(0, 8), null, TickRate.PerSecond(50));
        }

        [TearDown]
        public void DisposeContext() {
            _context.Dispose();
        }

        [Test]
        public void AnOccurrenceWriteIsUnderOneMicrosecond() {
            RecordOccurrences(WarmCalls);
            var nanoseconds = Time(RecordOccurrences);
            var figure = Report("OCCURRENCE_WRITE_NS", nanoseconds);
            Assert.Less(nanoseconds, TargetNanoseconds, figure);
            Assert.Pass(figure);
        }

        [Test]
        public void ACounterAddIsUnderOneMicrosecond() {
            AddCounts(WarmCalls);
            var nanoseconds = Time(AddCounts);
            var figure = Report("COUNTER_ADD_NS", nanoseconds);
            Assert.Less(nanoseconds, TargetNanoseconds, figure);
            Assert.Pass(figure);
        }

        [Test]
        public void AGuardCheckOnASubjectWithAnEntryIsUnderOneMicrosecond() {
            Assert.AreEqual(GuardVerdict.Repair, Embedded.Check(_context, false, Subject, Tick.Zero));
            Assert.AreEqual(1, _context.GuardEntryCount);

            CheckGuards(WarmCalls);
            var nanoseconds = Time(CheckGuards);
            var figure = Report("GUARD_CHECK_NS", nanoseconds);
            Assert.AreEqual(1, _context.GuardEntryCount);
            Assert.Less(nanoseconds, TargetNanoseconds, figure);
            Assert.Pass(figure);
        }

        private void RecordOccurrences(int calls) {
            for (var index = 0; index < calls; index++) {
                Landed.Record(_context, new Tick((ulong)index), Subject);
            }
        }

        private void AddCounts(int calls) {
            for (var index = 0; index < calls; index++) {
                Stepped.Add(_context);
            }
        }

        private void CheckGuards(int calls) {
            for (var index = 0; index < calls; index++) {
                Embedded.Check(_context, true, Subject, Tick.Zero);
            }
        }

        private static double Time(Action<int> calls) {
            var stopwatch = Stopwatch.StartNew();
            calls(TimedCalls);
            stopwatch.Stop();
            return stopwatch.ElapsedTicks * 1_000_000_000.0 / Stopwatch.Frequency / TimedCalls;
        }

        private static string Report(string name, double nanoseconds) {
            var figure = name + "=" + nanoseconds.ToString("F1", CultureInfo.InvariantCulture)
                         + " GUARD_STATE_BYTES=" + UnsafeUtility.SizeOf<GuardState>()
                         + " RECORDER_CELL_BYTES=" + UnsafeUtility.SizeOf<RecorderCell>();
            TestContext.WriteLine(figure);
            return figure;
        }
    }
}
