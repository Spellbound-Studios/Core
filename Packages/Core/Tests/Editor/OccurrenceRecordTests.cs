// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Spellbound.Core.Observations;
using Spellbound.Core.Simulation;

namespace Spellbound.Core.Tests {
    public class OccurrenceRecordTests {
        private const ulong Subject = 0xA1B2C3D4E5F60718UL;

        private static readonly Occurrence Bound = CoreKit.Observations.Occurrence("occurrencerecordtests.body.bound",
            "A body bound itself to a reference frame.");

        private readonly List<RecordedOccurrence> _records = new List<RecordedOccurrence>(4);
        private ObservationContext _context;

        [SetUp]
        public void CreateContext() {
            _context = CoreKit.Observations.CreateContext(
                "occurrencerecord_" + Guid.NewGuid().ToString("N").Substring(0, 8));
        }

        [TearDown]
        public void DisposeContext() {
            _context.Dispose();
        }

        [Test]
        public void TheTickAndSubjectFormLandsWithNoValues() {
            Bound.Record(_context, new Tick(42), Subject);

            Assert.AreEqual(1, _context.ReadRecords(_records));
            Assert.AreEqual(Bound.Key.Hash, _records[0].KeyHash);
            Assert.AreEqual(new Tick(42), _records[0].Tick);
            Assert.AreEqual(Subject, _records[0].Subject);
            Assert.AreEqual(0, _records[0].ValueCount);
        }

        [Test]
        public void TheOneValueFormLandsAsTwoValuesLowThenHigh() {
            Bound.Record(_context, new Tick(43), Subject, 0x1122334455667788UL);

            Assert.AreEqual(1, _context.ReadRecords(_records));
            Assert.AreEqual(new Tick(43), _records[0].Tick);
            Assert.AreEqual(2, _records[0].ValueCount);
            Assert.AreEqual(0x55667788u, _records[0].Value(0));
            Assert.AreEqual(0x11223344u, _records[0].Value(1));
        }

        [Test]
        public void TheSpanFormLandsWithEveryValue() {
            var values = new uint[] { 7, 6, 5, 4, 3, 2, 1 };

            Bound.Record(_context, new Tick(44), Subject, values);

            Assert.AreEqual(1, _context.ReadRecords(_records));
            Assert.AreEqual(7, _records[0].ValueCount);

            for (var i = 0; i < values.Length; i++) {
                Assert.AreEqual(values[i], _records[0].Value(i));
            }
        }

        [Test]
        public void TheThreeFormsLandInTheOrderTheyWereRecorded() {
            Bound.Record(_context, new Tick(1), 1UL);
            Bound.Record(_context, new Tick(2), 2UL, 9UL);
            Bound.Record(_context, new Tick(3), 3UL, new uint[] { 8, 8, 8 });

            Assert.AreEqual(3, _context.ReadRecords(_records));
            Assert.AreEqual(1UL, _records[0].Subject);
            Assert.AreEqual(2UL, _records[1].Subject);
            Assert.AreEqual(3UL, _records[2].Subject);
            Assert.AreEqual(3, _records[2].ValueCount);
        }

        [Test]
        public void ThirteenValuesThrow() {
            var values = new uint[13];

            Assert.Throws<ArgumentException>(() => Bound.Record(_context, new Tick(1), Subject, values));
            Assert.AreEqual(0, _context.RecordCount);
        }

        [Test]
        public void ANullContextThrowsForEveryForm() {
            Assert.Throws<ArgumentNullException>(() => Bound.Record(null, new Tick(1), Subject));
            Assert.Throws<ArgumentNullException>(() => Bound.Record(null, new Tick(1), Subject, 1UL));
            Assert.Throws<ArgumentNullException>(() => Bound.Record(null, new Tick(1), Subject, new uint[1]));
        }

        [Test]
        public void TheDiscardContextTakesEveryFormAndKeepsNothing() {
            Bound.Record(ObservationContext.Discard, new Tick(1), Subject);
            Bound.Record(ObservationContext.Discard, new Tick(1), Subject, 1UL);
            Bound.Record(ObservationContext.Discard, new Tick(1), Subject, new uint[3]);

            Assert.AreEqual(0, ObservationContext.Discard.ReadRecords(_records));
        }

        [Test]
        public void TheOccurrenceStillCountsBesideItsRecord() {
            Bound.Record(_context, new Tick(1), Subject);
            Bound.Record(_context, new Tick(2), Subject, 5UL);

            Assert.IsTrue(_context.TryRead(Bound.Key, out var reading));
            Assert.AreEqual(2, reading.Count);
        }
    }
}
