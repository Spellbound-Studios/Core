// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using NUnit.Framework;
using Spellbound.Core.Observations;
using Spellbound.Core.ReferenceFrames;
using Spellbound.Core.Simulation;
using Unity.Collections;

namespace Spellbound.Core.Tests {
    public class ReferenceFrameChecksTests {
        private static readonly ReferenceFrameId Platform = new ReferenceFrameId(41);

        private ObservationContext _context;

        [SetUp]
        public void CreateContext() {
            _context = CoreKit.Observations.CreateContext(
                "referenceframechecks_" + Guid.NewGuid().ToString("N").Substring(0, 8), null, TickRate.PerSecond(50));
        }

        [TearDown]
        public void DisposeContext() {
            _context.Dispose();
        }

        [Test]
        public void AConstantUpIsAllowedAndTripsNothing() {
            Assert.IsTrue(ReferenceFrameChecks.RequireConstantUp(_context, new Tick(1), Platform, UpField.WorldUp));
            Assert.IsFalse(_context.TryRead(CoreObservations.ReferenceFrameUpVaries.Key, out _));
            Assert.AreEqual(0, _context.GuardEntryCount);
        }

        [Test]
        public void ARadialUpIsRefusedAndCountedOnce() {
            var field = UpField.RadialFrom(0f, 0f, 0f);
            Assert.IsFalse(ReferenceFrameChecks.RequireConstantUp(_context, new Tick(1), Platform, field));
            AssertCountedOnce();
        }

        [Test]
        public void ASampledUpIsRefusedAndCountedOnce() {
            var directions = new NativeArray<UpDirection>(8, Allocator.Temp);

            try {
                for (var i = 0; i < directions.Length; i++) {
                    directions[i] = UpDirection.WorldUp;
                }

                var field = UpField.Sampled(new UpGrid(directions, 0f, 0f, 0f, 1f, 2, 2, 2), UpDirection.WorldUp);
                Assert.IsFalse(ReferenceFrameChecks.RequireConstantUp(_context, new Tick(1), Platform, field));
                AssertCountedOnce();
            }
            finally {
                directions.Dispose();
            }
        }

        private void AssertCountedOnce() {
            Assert.IsTrue(_context.TryRead(CoreObservations.ReferenceFrameUpVaries.Key, out var reading));
            Assert.AreEqual(1.0, reading.Value);
            Assert.AreEqual(1, reading.Count);
            Assert.AreEqual(1, _context.GuardEntryCount);
        }
    }
}
