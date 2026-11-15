// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Spellbound.Core.Observations;
using Spellbound.Core.Simulation;
using Unity.Collections.LowLevel.Unsafe;

namespace Spellbound.Core.Tests {
    public class RecorderCellSizeTests {
        [Test]
        public void ARecorderCellIsSixteenBytes() {
            Assert.AreEqual(16, UnsafeUtility.SizeOf<RecorderCell>());
        }

        [Test]
        public void ACellKeepsTheKeyTheTickLowPartAndTheSubject() {
            var cell = new RecorderCell(0xCAFEF00Du, 0xFFFFFFFEu, 0x0102030405060708UL);

            Assert.AreEqual(0xCAFEF00Du, cell.Key);
            Assert.AreEqual(0xFFFFFFFEu, cell.TickLow);
            Assert.AreEqual(0x0102030405060708UL, cell.Subject);
        }

        [Test]
        public void ANewRecorderHasItsCapacityAndNoRecords() {
            using (var recorder = new FlightRecorder(64)) {
                Assert.AreEqual(64, recorder.Capacity);
                Assert.AreEqual(0, recorder.Count);
            }
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ARecorderNeedsAtLeastOneCell(int capacity) {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FlightRecorder(capacity));
        }

        [Test]
        public void ARecorderDisposesTwiceWithoutComplaint() {
            var recorder = new FlightRecorder(8);
            recorder.Dispose();
            Assert.DoesNotThrow(() => recorder.Dispose());
        }

        [Test]
        public void ReadFillsAListOfRecordedOccurrences() {
            var read = typeof(FlightRecorder).GetMethod(nameof(FlightRecorder.Read),
                new[] { typeof(List<RecordedOccurrence>) });

            Assert.IsNotNull(read);
            Assert.AreEqual(typeof(int), read.ReturnType);
        }

        [Test]
        public void ARecordedOccurrenceKeepsWhatItWasGiven() {
            var values = new uint[] { 10, 20, 30, 40, 50 };
            var recorded = new RecordedOccurrence(0xABCDu, new Tick(5000000000UL), 77UL, values, true);

            Assert.AreEqual(0xABCDu, recorded.KeyHash);
            Assert.AreEqual(5000000000UL, recorded.Tick.Value);
            Assert.AreEqual(77UL, recorded.Subject);
            Assert.AreEqual(5, recorded.ValueCount);
            Assert.IsTrue(recorded.Flagged);

            for (var i = 0; i < values.Length; i++) {
                Assert.AreEqual(values[i], recorded.Value(i));
            }
        }

        [Test]
        public void ARecordedOccurrenceHoldsTwelveValues() {
            var values = new uint[12];

            for (var i = 0; i < values.Length; i++) {
                values[i] = (uint)(i + 1) * 3u;
            }

            var recorded = new RecordedOccurrence(1u, new Tick(1), 2UL, values, false);

            Assert.AreEqual(12, recorded.ValueCount);
            Assert.AreEqual(12, RecordedOccurrence.MaximumValues);
            Assert.IsFalse(recorded.Flagged);

            for (var i = 0; i < values.Length; i++) {
                Assert.AreEqual(values[i], recorded.Value(i));
            }
        }

        [Test]
        public void ARecordedOccurrenceRefusesThirteenValues() {
            Assert.Throws<ArgumentException>(() =>
                new RecordedOccurrence(1u, new Tick(1), 2UL, new uint[13], false));
        }

        [TestCase(-1)]
        [TestCase(2)]
        public void AValueOutsideTheCountIsRefused(int index) {
            var recorded = new RecordedOccurrence(1u, new Tick(1), 2UL, new uint[] { 4, 5 }, false);
            Assert.Throws<ArgumentOutOfRangeException>(() => recorded.Value(index));
        }

        [Test]
        public void AnOccurrenceRecordsWithOneValueOrWithASpanOfValues() {
            var oneValue = typeof(Occurrence).GetMethod(nameof(Occurrence.Record),
                new[] { typeof(ObservationContext), typeof(Tick), typeof(ulong), typeof(ulong) });
            var spanOfValues = typeof(Occurrence).GetMethod(nameof(Occurrence.Record),
                new[] { typeof(ObservationContext), typeof(Tick), typeof(ulong), typeof(ReadOnlySpan<uint>) });

            Assert.IsNotNull(oneValue);
            Assert.AreEqual(typeof(void), oneValue.ReturnType);
            Assert.IsNotNull(spanOfValues);
            Assert.AreEqual(typeof(void), spanOfValues.ReturnType);
        }
    }
}
