// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Spellbound.Core.Intents;
using Spellbound.Core.Observations;
using Spellbound.Core.Simulation;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Spellbound.Core.Tests {
    public class IntentLedgerTests {
        private static readonly IntentActor First = new IntentActor(1);
        private static readonly IntentActor Second = new IntentActor(2);
        private static readonly ObservationReason Granted = CoreKit.Observations.Reason("intentledgertests.granted");
        private static readonly ObservationReason Refused = CoreKit.Observations.Reason("intentledgertests.refused");

        private ObservationContext _context;

        [SetUp]
        public void CreateContext() {
            _context = CoreKit.Observations.CreateContext("intents_" + Guid.NewGuid().ToString("N").Substring(0, 8));
        }

        [TearDown]
        public void DisposeContext() {
            _context.Dispose();
        }

        private IntentLedger Ledger(int windowSize) => new IntentLedger(windowSize, new TickSpan(100), _context);

        private double Read(ObservationKey key) => _context.TryRead(key, out var reading) ? reading.Value : 0;

        [Test]
        public void TheCheckDefaultIsNeverApplied() {
            Assert.AreEqual(IntentLedgerCheck.Unseen, default(IntentLedgerCheck));
            Assert.AreNotEqual(IntentLedgerCheck.Applied, default(IntentLedgerCheck));
        }

        [Test]
        public void AKeyIsUnseenThenAppliedWithItsReceipt() {
            var ledger = Ledger(8);
            var key = new IntentKey(First, 1);
            var receipt = new IntentReceipt(Granted, 3);

            Assert.AreEqual(IntentLedgerCheck.Unseen, ledger.Check(key, out var before));
            Assert.AreEqual(IntentReceipt.None, before);
            Assert.IsTrue(ledger.Record(key, receipt));
            Assert.AreEqual(IntentLedgerCheck.Applied, ledger.Check(key, out var after));
            Assert.AreEqual(receipt, after);
            Assert.AreEqual(IntentLedgerCheck.Applied, ledger.Check(key, out var again));
            Assert.AreEqual(receipt, again);
            Assert.AreEqual(2, Read(CoreObservations.IntentRepeats.Key));
        }

        [Test]
        public void ADifferentReceiptDoesNotOverwrite() {
            var ledger = Ledger(8);
            var key = new IntentKey(First, 4);
            Assert.IsTrue(ledger.Record(key, new IntentReceipt(Granted, 1)));
            Assert.IsFalse(ledger.Record(key, new IntentReceipt(Refused, 9)));
            ledger.Check(key, out var receipt);
            Assert.AreEqual(new IntentReceipt(Granted, 1), receipt);
        }

        [Test]
        public void AKeyBelowTheWindowIsStale() {
            var ledger = Ledger(8);
            Assert.IsTrue(ledger.Record(new IntentKey(First, 20), new IntentReceipt(Granted, 1)));

            Assert.AreEqual(IntentLedgerCheck.Stale, ledger.Check(new IntentKey(First, 12), out var receipt));
            Assert.AreEqual(IntentReceipt.None, receipt);
            Assert.AreEqual(IntentLedgerCheck.Stale, ledger.Check(new IntentKey(First, 1), out _));
            Assert.AreEqual(IntentLedgerCheck.Unseen, ledger.Check(new IntentKey(First, 13), out _));
            Assert.IsFalse(ledger.Record(new IntentKey(First, 12), new IntentReceipt(Granted, 1)));
            Assert.AreEqual(2, Read(CoreObservations.IntentStale.Key));
        }

        [Test]
        public void AStaleKeyWhoseSlotWasReusedIsStillStale() {
            var ledger = Ledger(8);
            Assert.IsTrue(ledger.Record(new IntentKey(First, 3), new IntentReceipt(Granted, 1)));
            Assert.IsTrue(ledger.Record(new IntentKey(First, 11), new IntentReceipt(Refused, 2)));
            Assert.AreEqual(IntentLedgerCheck.Stale, ledger.Check(new IntentKey(First, 3), out _));
            Assert.AreEqual(IntentLedgerCheck.Applied, ledger.Check(new IntentKey(First, 11), out var receipt));
            Assert.AreEqual(new IntentReceipt(Refused, 2), receipt);
        }

        [Test]
        public void AKeyWithNoActorIsStale() {
            var ledger = Ledger(8);
            var key = new IntentKey(IntentActor.None, 1);
            Assert.AreEqual(IntentLedgerCheck.Stale, ledger.Check(key, out _));
            Assert.IsFalse(ledger.Record(key, new IntentReceipt(Granted, 1)));
            Assert.AreEqual(0, ledger.ActorCount);
        }

        [Test]
        public void SequenceZeroIsStale() {
            var ledger = Ledger(8);
            Assert.AreEqual(IntentLedgerCheck.Stale, ledger.Check(new IntentKey(First, 0), out _));
            Assert.IsFalse(ledger.Record(new IntentKey(First, 0), new IntentReceipt(Granted, 1)));
            Assert.AreEqual(0, ledger.ActorCount);
        }

        [Test]
        public void OutOfOrderSequencesInsideTheWindowAreFine() {
            var ledger = Ledger(8);
            uint[] arrival = { 5, 2, 8, 1, 7, 3, 6, 4 };

            foreach (var sequence in arrival) {
                var key = new IntentKey(First, sequence);
                Assert.AreEqual(IntentLedgerCheck.Unseen, ledger.Check(key, out _), "sequence " + sequence);
                Assert.IsTrue(ledger.Record(key, new IntentReceipt(Granted, sequence)));
            }

            foreach (var sequence in arrival) {
                var check = ledger.Check(new IntentKey(First, sequence), out var receipt);
                Assert.AreEqual(IntentLedgerCheck.Applied, check);
                Assert.AreEqual(sequence, receipt.Count);
            }
        }

        [Test]
        public void TwoActorsKeepApartWindows() {
            var ledger = Ledger(8);
            Assert.IsTrue(ledger.Record(new IntentKey(First, 1), new IntentReceipt(Granted, 1)));
            Assert.AreEqual(IntentLedgerCheck.Unseen, ledger.Check(new IntentKey(Second, 1), out _));
            Assert.IsTrue(ledger.Record(new IntentKey(Second, 1), new IntentReceipt(Refused, 2)));
            Assert.IsTrue(ledger.Record(new IntentKey(Second, 40), new IntentReceipt(Refused, 2)));

            Assert.AreEqual(IntentLedgerCheck.Applied, ledger.Check(new IntentKey(First, 1), out var first));
            Assert.AreEqual(new IntentReceipt(Granted, 1), first);
            Assert.AreEqual(IntentLedgerCheck.Stale, ledger.Check(new IntentKey(Second, 1), out _));
            Assert.AreEqual(2, ledger.ActorCount);
        }

        [Test]
        public void TwoLedgersNeverShareAWindow() {
            var one = Ledger(8);
            var other = Ledger(8);
            var key = new IntentKey(First, 1);
            Assert.IsTrue(one.Record(key, new IntentReceipt(Granted, 1)));
            Assert.AreEqual(IntentLedgerCheck.Unseen, other.Check(key, out _));
            Assert.IsTrue(other.Record(key, new IntentReceipt(Refused, 5)));
            one.Check(key, out var fromOne);
            other.Check(key, out var fromOther);
            Assert.AreEqual(new IntentReceipt(Granted, 1), fromOne);
            Assert.AreEqual(new IntentReceipt(Refused, 5), fromOther);
        }

        [Test]
        public void BytesAreActorsTimesTheWindowTimesTwelve() {
            var ledger = Ledger(32);
            Assert.AreEqual(0, ledger.Bytes);

            for (ulong actor = 1; actor <= 5; actor++) {
                ledger.Record(new IntentKey(new IntentActor(actor), 1), new IntentReceipt(Granted, 1));
                ledger.Record(new IntentKey(new IntentActor(actor), 2), new IntentReceipt(Granted, 1));
            }

            Assert.AreEqual(5, ledger.ActorCount);
            Assert.AreEqual(5L * 32 * 12, ledger.Bytes);
            Assert.AreEqual(5 * 32 * 12, Read(CoreObservations.IntentLedgerBytes.Key));
        }

        [Test]
        public void TheCountersAndTheGaugeAreReadFromTheContext() {
            var ledger = Ledger(4);
            ledger.Record(new IntentKey(First, 10), new IntentReceipt(Granted, 1));
            ledger.Check(new IntentKey(First, 10), out _);
            ledger.Check(new IntentKey(First, 2), out _);
            ledger.Check(new IntentKey(First, 3), out _);
            ledger.Check(new IntentKey(First, 9), out _);

            Assert.AreEqual(1, Read(CoreObservations.IntentRepeats.Key));
            Assert.AreEqual(2, Read(CoreObservations.IntentStale.Key));
            Assert.AreEqual(4 * 12, Read(CoreObservations.IntentLedgerBytes.Key));
            Assert.AreEqual(ObservationKind.Gauge, CoreObservations.IntentLedgerBytes.Declaration.Kind);
            Assert.AreEqual(ObservationUnit.Bytes, CoreObservations.IntentLedgerBytes.Declaration.Unit);
        }

        [Test]
        public void AWindowSizeUnderOneThrows() {
            Assert.Throws<ArgumentOutOfRangeException>(() => new IntentLedger(0, TickSpan.Zero, _context));
            Assert.Throws<ArgumentNullException>(() => new IntentLedger(8, TickSpan.Zero, null));
        }

        [Test]
        public void Check_NoAllocation() {
            var ledger = Ledger(8);
            ledger.Record(new IntentKey(First, 9), new IntentReceipt(Granted, 1));
            TestDelegate action = () => {
                ledger.Check(new IntentKey(First, 9), out _);
                ledger.Check(new IntentKey(First, 1), out _);
                ledger.Check(new IntentKey(First, 8), out _);
                ledger.Check(new IntentKey(Second, 1), out _);
            };

            AssertNoAllocation(action);
        }

        [Test]
        public void Record_NoAllocation() {
            var ledger = Ledger(64);
            var receipt = new IntentReceipt(Granted, 1);
            ledger.Record(new IntentKey(First, 1), receipt);
            uint next = 2;
            TestDelegate action = () => {
                for (var i = 0; i < 16; i++) {
                    ledger.Record(new IntentKey(First, next++), receipt);
                }
            };

            AssertNoAllocation(action);
        }

        internal static void AssertNoAllocation(TestDelegate action) {
            action();
            var constraint = ((IResolveConstraint)Is.Not.AllocatingGCMemory()).Resolve();
            constraint.ApplyTo(action);
            Assert.That(action, constraint);
        }
    }
}
