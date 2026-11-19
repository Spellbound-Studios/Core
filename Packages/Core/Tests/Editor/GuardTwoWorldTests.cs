// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using NUnit.Framework;
using Spellbound.Core.Observations;
using Spellbound.Core.Simulation;

namespace Spellbound.Core.Tests {
    public class GuardTwoWorldTests {
        private static readonly Guard Embedded = CoreKit.Observations.Guard("guardtwoworldtests.containment.embedded",
            "A body was found inside solid terrain and was moved out.");

        private ObservationContext _first;
        private ObservationContext _second;

        [SetUp]
        public void CreateContexts() {
            var suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            _first = CoreKit.Observations.CreateContext("guardworld_first_" + suffix, null, TickRate.PerSecond(50));
            _second = CoreKit.Observations.CreateContext("guardworld_second_" + suffix, null, TickRate.PerSecond(50));
        }

        [TearDown]
        public void DisposeContexts() {
            _first.Dispose();
            _second.Dispose();
        }

        [Test]
        public void ATripInOneContextChangesNothingInTheOther() {
            Assert.AreEqual(3, Trip(_first, 1));
            Assert.IsFalse(_second.TryRead(Embedded.Key, out _));
            Assert.AreEqual(0, _second.Version(Embedded.Key));
            Assert.AreEqual(0, _second.GuardEntryCount);
            Assert.AreEqual(GuardVerdict.Repair, Embedded.Check(_second, false, 1, new Tick(3)));
            Assert.AreEqual(GuardVerdict.Hold, Embedded.Check(_first, false, 1, new Tick(3)));
        }

        [Test]
        public void ThreeSubjectsFailingTogetherNeverPutAFourthOnHold() {
            for (var subject = 1ul; subject <= 3; subject++) {
                Assert.AreEqual(3, Trip(_first, subject));
            }

            Assert.AreEqual(GuardVerdict.Repair, Embedded.Check(_first, false, 4, new Tick(3)));
            Assert.AreEqual(GuardVerdict.Holds, Embedded.Check(_first, true, 5, new Tick(3)));
            Assert.AreEqual(4, _first.GuardEntryCount);
        }

        private static int Trip(ObservationContext context, ulong subject) {
            var checks = 0;
            var verdict = GuardVerdict.Holds;

            while (verdict != GuardVerdict.Hold && checks < 10) {
                verdict = Embedded.Check(context, false, subject, new Tick((ulong)checks));
                checks++;
            }

            return checks;
        }
    }
}
