// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using NUnit.Framework;
using Spellbound.Core.Intents;
using Spellbound.Core.Observations;
using Spellbound.Core.Simulation;

namespace Spellbound.Core.Tests {
    public class IntentLedgerDepartureTests {
        private static readonly IntentActor First = new IntentActor(1);
        private static readonly IntentActor Second = new IntentActor(2);
        private static readonly ObservationReason Granted = CoreKit.Observations.Reason("intentdeparturetests.granted");
        private static readonly IntentReceipt Receipt = new IntentReceipt(Granted, 1);

        private ObservationContext _context;

        [SetUp]
        public void CreateContext() {
            _context = CoreKit.Observations.CreateContext("departures_" + Guid.NewGuid().ToString("N").Substring(0, 8));
        }

        [TearDown]
        public void DisposeContext() {
            _context.Dispose();
        }

        private IntentLedger Ledger(int windowSize, ulong graceTicks) =>
            new IntentLedger(windowSize, new TickSpan(graceTicks), _context);

        private double Dropped() =>
            _context.TryRead(CoreObservations.IntentWindowsDropped.Key, out var reading) ? reading.Value : 0;

        [Test]
        public void ADepartedWindowDropsWhenTheGraceHasPassed() {
            var ledger = Ledger(8, 10);
            ledger.Record(new IntentKey(First, 1), Receipt);
            ledger.Depart(First, new Tick(5));

            Assert.AreEqual(0, ledger.Expire(new Tick(14)));
            Assert.AreEqual(1, ledger.ActorCount);
            Assert.AreEqual(1, ledger.Expire(new Tick(15)));
            Assert.AreEqual(0, ledger.ActorCount);
            Assert.AreEqual(0, ledger.Bytes);
            Assert.AreEqual(1, Dropped());
            Assert.AreEqual(IntentLedgerCheck.Unseen, ledger.Check(new IntentKey(First, 1), out _));
        }

        [Test]
        public void DepartMarksAndOnlyExpireDrops() {
            var ledger = Ledger(8, 10);
            ledger.Record(new IntentKey(First, 1), Receipt);
            ledger.Record(new IntentKey(Second, 1), Receipt);
            ledger.Depart(First, new Tick(0));
            ledger.Depart(Second, new Tick(10));

            Assert.AreEqual(2, ledger.ActorCount);
            Assert.AreEqual(0, Dropped());
            Assert.AreEqual(1, ledger.Expire(new Tick(10)));
            Assert.AreEqual(1, ledger.ActorCount);
            Assert.AreEqual(IntentLedgerCheck.Unseen, ledger.Check(new IntentKey(First, 1), out _));
            Assert.AreEqual(IntentLedgerCheck.Applied, ledger.Check(new IntentKey(Second, 1), out _));
            Assert.AreEqual(1, Dropped());
        }

        [Test]
        public void AReturnBeforeTheDropKeepsTheWindow() {
            var ledger = Ledger(8, 10);
            ledger.Record(new IntentKey(First, 1), Receipt);
            ledger.Depart(First, new Tick(0));
            ledger.Return(First);

            Assert.AreEqual(0, ledger.Expire(new Tick(1000)));
            Assert.AreEqual(IntentLedgerCheck.Applied, ledger.Check(new IntentKey(First, 1), out var receipt));
            Assert.AreEqual(Receipt, receipt);
        }

        [Test]
        public void AReturnAfterTheDropStartsAnEmptyWindow() {
            var ledger = Ledger(8, 10);
            ledger.Record(new IntentKey(First, 1), Receipt);
            ledger.Depart(First, new Tick(0));
            Assert.AreEqual(1, ledger.Expire(new Tick(10)));
            ledger.Return(First);

            Assert.AreEqual(0, ledger.ActorCount);
            Assert.AreEqual(IntentLedgerCheck.Unseen, ledger.Check(new IntentKey(First, 1), out _));
            Assert.IsTrue(ledger.Record(new IntentKey(First, 1), Receipt));
            Assert.AreEqual(1, ledger.ActorCount);
        }

        [Test]
        public void ARecordIntoADepartedWindowIsKeptAndCancelsNothing() {
            var ledger = Ledger(8, 10);
            ledger.Record(new IntentKey(First, 1), Receipt);
            ledger.Depart(First, new Tick(0));
            Assert.IsTrue(ledger.Record(new IntentKey(First, 2), Receipt));
            Assert.AreEqual(IntentLedgerCheck.Applied, ledger.Check(new IntentKey(First, 2), out _));

            Assert.AreEqual(1, ledger.Expire(new Tick(10)));
            Assert.AreEqual(0, ledger.ActorCount);
        }

        [Test]
        public void ARecordAfterTheDropTickIsKeptUntilExpireDropsTheWindow() {
            var ledger = Ledger(8, 10);
            ledger.Record(new IntentKey(First, 1), Receipt);
            ledger.Depart(First, new Tick(0));

            Assert.IsTrue(ledger.Record(new IntentKey(First, 2), Receipt));
            Assert.AreEqual(0, Dropped());
            Assert.AreEqual(IntentLedgerCheck.Applied, ledger.Check(new IntentKey(First, 2), out _));
            Assert.AreEqual(1, ledger.Expire(new Tick(100)));
            Assert.AreEqual(0, ledger.ActorCount);
        }

        [Test]
        public void DepartingOrReturningAnUnknownActorChangesNothing() {
            var ledger = Ledger(8, 10);
            ledger.Depart(First, new Tick(0));
            ledger.Return(Second);
            Assert.AreEqual(0, ledger.ActorCount);
            Assert.AreEqual(0, ledger.Expire(new Tick(100)));
        }

        [Test]
        public void TheGraceIsConvertedFromSecondsAtCreation() {
            var ledger = CoreKit.Intents.CreateLedger(TickRate.PerSecond(10), _context);
            ledger.Record(new IntentKey(First, 1), Receipt);
            ledger.Depart(First, new Tick(0));
            Assert.AreEqual(0, ledger.Expire(new Tick(2999)));
            Assert.AreEqual(1, ledger.Expire(new Tick(3000)));
            Assert.AreEqual(0, ledger.Bytes);
        }

        [Test]
        public void ASinglePlayerGameAppliesARepeatedKeyOnce() {
            var sequence = CoreKit.Intents.Sequence(new IntentActor(1));
            var ledger = CoreKit.Intents.CreateLedger(TickRate.PerSecond(30), _context);
            var applied = 0;

            IntentReceipt Apply(in IntentKey key) {
                if (ledger.Check(key, out var answered) != IntentLedgerCheck.Unseen) {
                    return answered;
                }

                applied++;
                var receipt = new IntentReceipt(Granted, (uint)applied);
                ledger.Record(key, receipt);
                return receipt;
            }

            var first = sequence.Next();
            var answer = Apply(first);
            Assert.AreEqual(answer, Apply(first));
            Assert.AreEqual(1, applied);
            Apply(sequence.Next());
            Assert.AreEqual(2, applied);
            Assert.AreEqual(1, ledger.ActorCount);
        }

        [Test]
        public void TwoThousandActorsStayWithinTheHeldFigure() {
            const int connected = 500;
            const ulong graceTicks = 50;
            const int windowSize = 8;
            var ledger = Ledger(windowSize, graceTicks);
            var limit = (connected + (long)graceTicks + 1) * windowSize * 12;
            var cycle = new ConnectAndDepartCycle(ledger, 2000, connected, graceTicks);

            cycle.Run();
            Assert.LessOrEqual(cycle.LargestBytes, limit);
            Assert.Greater(cycle.LargestBytes, (long)connected * windowSize * 12 - 1);
            Assert.AreEqual(0, ledger.ActorCount);
            Assert.AreEqual(2000, Dropped());

            IntentLedgerTests.AssertNoAllocation(cycle.Run);
            Assert.LessOrEqual(cycle.LargestBytes, limit);
            Assert.AreEqual(0, ledger.ActorCount);
        }

        [Test]
        public void Depart_NoAllocation() {
            var ledger = Ledger(8, 10);
            ulong now = 0;
            TestDelegate action = () => {
                ledger.Record(new IntentKey(First, 1), Receipt);
                ledger.Depart(First, new Tick(now));
                ledger.Expire(new Tick(now + 10));
                now += 20;
            };

            IntentLedgerTests.AssertNoAllocation(action);
        }

        [Test]
        public void Return_NoAllocation() {
            var ledger = Ledger(8, 10);
            ledger.Record(new IntentKey(First, 1), Receipt);
            TestDelegate action = () => {
                ledger.Depart(First, new Tick(0));
                ledger.Return(First);
            };

            IntentLedgerTests.AssertNoAllocation(action);
        }

        [Test]
        public void Expire_NoAllocation() {
            var ledger = Ledger(8, 10);
            ulong now = 0;
            TestDelegate action = () => {
                for (ulong actor = 1; actor <= 32; actor++) {
                    ledger.Record(new IntentKey(new IntentActor(actor), 1), Receipt);
                    ledger.Depart(new IntentActor(actor), new Tick(now));
                }

                ledger.Expire(new Tick(now + 5));
                ledger.Expire(new Tick(now + 10));
                now += 20;
            };

            IntentLedgerTests.AssertNoAllocation(action);
        }

        private sealed class ConnectAndDepartCycle {
            private readonly IntentLedger _ledger;
            private readonly int _actors;
            private readonly int _connected;
            private readonly ulong _graceTicks;
            private ulong _firstActor = 1;
            private ulong _now;

            public ConnectAndDepartCycle(IntentLedger ledger, int actors, int connected, ulong graceTicks) {
                _ledger = ledger;
                _actors = actors;
                _connected = connected;
                _graceTicks = graceTicks;
            }

            public long LargestBytes { get; private set; }

            public void Run() {
                LargestBytes = 0;
                var steps = _actors + _connected + (int)_graceTicks + 1;

                for (var step = 0; step < steps; step++) {
                    var now = new Tick(_now + (ulong)step);

                    if (step < _actors) {
                        var actor = new IntentActor(_firstActor + (ulong)step);

                        for (uint sequence = 1; sequence <= 3; sequence++) {
                            _ledger.Record(new IntentKey(actor, sequence), Receipt);
                        }
                    }

                    var leaving = step - _connected;

                    if (leaving >= 0 && leaving < _actors) {
                        _ledger.Depart(new IntentActor(_firstActor + (ulong)leaving), now);
                    }

                    _ledger.Expire(now);

                    if (_ledger.Bytes > LargestBytes) {
                        LargestBytes = _ledger.Bytes;
                    }
                }

                _firstActor += (ulong)_actors;
                _now += (ulong)steps;
            }
        }
    }
}
