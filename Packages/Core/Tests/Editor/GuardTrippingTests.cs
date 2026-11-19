// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using NUnit.Framework;
using Spellbound.Core.Observations;
using Spellbound.Core.Simulation;
using Spellbound.Core.Testing;

namespace Spellbound.Core.Tests {
    public class GuardTrippingTests {
        private static readonly Guard Wedged = CoreKit.Observations.Guard("guardtrippingtests.containment.wedged",
            "A body was wedged between two solids and was pushed apart.", new GuardPolicyValues(4, 2, 5));

        private ObservationContext _context;

        [SetUp]
        public void CreateContext() {
            _context = CoreKit.Observations.CreateContext(
                "guardtripping_" + Guid.NewGuid().ToString("N").Substring(0, 8), null, TickRate.PerSecond(50));
        }

        [TearDown]
        public void DisposeContext() {
            _context.Dispose();
        }

        [Test]
        public void TripReturnsThePolicysRepairsAndLeavesTheVerdictAtHold() {
            Assert.AreEqual(4, GuardTripping.Trip(Wedged, _context, 9, new Tick(10)));
            Assert.AreEqual(GuardVerdict.Hold, Wedged.Check(_context, false, 9, new Tick(14)));
            Assert.IsTrue(_context.TryRead(Wedged.Key, out var reading));
            Assert.AreEqual(5.0, reading.Value);
        }

        [Test]
        public void TripRefusesANullGuardOrContext() {
            Assert.Throws<ArgumentNullException>(() => GuardTripping.Trip(null, _context, 1, new Tick(0)));
            Assert.Throws<ArgumentNullException>(() => GuardTripping.Trip(Wedged, null, 1, new Tick(0)));
        }

        [Test]
        public void TripAllTripsCoresReferenceFrameUpVaries() {
            Assert.GreaterOrEqual(GuardTripping.TripAll("core", _context), 1);
            var key = CoreObservations.ReferenceFrameUpVaries.Key;
            Assert.AreEqual("core.reference_frame.up_varies", key.Name);
            Assert.IsTrue(_context.TryRead(key, out var reading));
            Assert.AreEqual(GuardPolicyValues.Default.Repairs, reading.Value);
            Assert.AreEqual(GuardVerdict.Hold,
                CoreObservations.ReferenceFrameUpVaries.Check(_context, false, 0, new Tick(3)));
        }

        [Test]
        public void TripAllOfAPackageWithNoGuardsTripsNothing() {
            Assert.AreEqual(0, GuardTripping.TripAll("guardtrippingtests_none", _context));
            Assert.AreEqual(0, _context.GuardEntryCount);
        }
    }
}
