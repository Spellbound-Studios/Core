// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Spellbound.Core.Intents;
using Spellbound.Core.Observations;
using Spellbound.Core.Simulation;

namespace Spellbound.Core.Tests {
    public class IntentLedgerDepartureSoakTests {
        private const int ActorTotal = 20_000;
        private const int PresentActors = 5_000;
        private const int TicksBetweenArrivals = 10;
        private const int ActorsBetweenReadings = 1_000;
        private const int EntryBytes = 12;

        private static readonly ObservationReason Granted =
            CoreKit.Observations.Reason("intentdeparturesoaktests.granted");

        private static readonly IntentReceipt Receipt = new IntentReceipt(Granted, 1);

        private ObservationContext _context;

        [SetUp]
        public void CreateContext() {
            _context = CoreKit.Observations.CreateContext(
                "departuresoak_" + Guid.NewGuid().ToString("N").Substring(0, 8));
        }

        [TearDown]
        public void DisposeContext() {
            _context.Dispose();
        }

        [Test]
        public void TwentyThousandActorsOnFiveThousandPlacesKeepTheLedgerBounded() {
            var rate = TickRate.PerSecond(50);
            var ledger = CoreKit.Intents.CreateLedger(rate, _context);
            var grace = rate.FromSeconds(IntentLedgerSettings.DefaultDepartureGraceSeconds);
            var windowBytes = (long)IntentLedgerSettings.DefaultWindowSize * EntryBytes;
            var departures = new Queue<Tick>(ActorTotal);
            var arrived = 0;
            var departed = 0;
            var now = Tick.Zero;
            var readings = 0;

            while (arrived < ActorTotal) {
                now = new Tick(now.Value + 1);

                if (now.Value % TicksBetweenArrivals == 0) {
                    if (arrived >= PresentActors) {
                        ledger.Depart(Actor(arrived - PresentActors), now);
                        departures.Enqueue(now);
                        departed++;
                    }

                    Assert.IsTrue(ledger.Record(new IntentKey(Actor(arrived), 1), Receipt));
                    arrived++;
                }

                ledger.Expire(now);
                ForgetDropped(departures, grace, now);

                if (now.Value % TicksBetweenArrivals == 0 && arrived % ActorsBetweenReadings == 0) {
                    var present = Math.Min(arrived, PresentActors);
                    var bound = (present + departures.Count) * windowBytes;
                    Assert.LessOrEqual(Reading(CoreObservations.IntentLedgerBytes.Key), bound,
                        "after " + arrived + " actors");
                    Assert.LessOrEqual(ledger.Bytes, bound, "after " + arrived + " actors");
                    readings++;
                }
            }

            Assert.AreEqual(ActorTotal / ActorsBetweenReadings, readings);
            Assert.AreEqual(ActorTotal - PresentActors, departed);

            var end = now.Plus(grace);

            while (now < end) {
                now = new Tick(now.Value + 1);
                ledger.Expire(now);
            }

            Assert.AreEqual(ActorTotal - PresentActors, Reading(CoreObservations.IntentWindowsDropped.Key));
            Assert.AreEqual(PresentActors, ledger.ActorCount);
            Assert.AreEqual(PresentActors * windowBytes, Reading(CoreObservations.IntentLedgerBytes.Key));
        }

        private static IntentActor Actor(int index) => new IntentActor((ulong)index + 1);

        private static void ForgetDropped(Queue<Tick> departures, TickSpan grace, Tick now) {
            while (departures.Count > 0 && departures.Peek().Plus(grace) <= now) {
                departures.Dequeue();
            }
        }

        private double Reading(ObservationKey key) => _context.TryRead(key, out var reading) ? reading.Value : 0;
    }
}
