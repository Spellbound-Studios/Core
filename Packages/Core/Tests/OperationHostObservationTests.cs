// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Spellbound.Core.Logging;
using Spellbound.Core.Observations;
using Spellbound.Core.Operations;
using Spellbound.Core.Tests.OperationFixture;
using UnityEngine;

namespace Spellbound.Core.Tests {
    public class OperationHostObservationTests {
        private static readonly Assembly Fixture = typeof(FixtureOperations).Assembly;
        private static readonly Assembly Core = typeof(OperationHost).Assembly;
        private static readonly ObservationKey Duration = ObservationKey.Parse("fixture.operations");
        private static readonly ObservationKey Failures = ObservationKey.Parse("fixture.operation_failures");

        private readonly List<ReasonCount> _reasons = new List<ReasonCount>();
        private OperationHost _host;
        private OperationCaller _developer;
        private ObservationContext _context;
        private Log.SinkSuspension _suspension;

        [SetUp]
        public void SetUp() {
            _suspension = Log.SuspendSinks();
            _host = new OperationHost(new OperationCatalog(() => new[] { Fixture, Core }),
                () => ScriptableObject.CreateInstance<OperationSettings>());
            _developer = OperationCaller.Development();
            _context = CoreKit.Observations.CreateContext("host_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            FixtureOperations.Reset();
        }

        [TearDown]
        public void TearDown() {
            _context.Dispose();
            _suspension.Dispose();
        }

        [Test]
        public void ANullContextIsRefusedByEveryDoor() {
            Assert.Throws<ArgumentNullException>(() => _host.Run("fixture echo hi", _developer, null));
            Assert.Throws<ArgumentNullException>(() => _host.Run("fixture echo hi", _developer, null, null));
            Assert.Throws<ArgumentNullException>(() => _host.RunAll(new[] { "fixture echo hi" }, _developer, null));
            var invocation = _host.Parse("fixture echo hi", null, out _);
            Assert.Throws<ArgumentNullException>(() => _host.Run(invocation, _developer, null));
        }

        [Test]
        public void EachRunRecordsTheGroupsDuration() {
            _host.Run("fixture echo one", _developer, _context);
            _host.Run("fixture add 1 2", _developer, _context);
            Assert.IsTrue(_context.TryRead(Duration, out var reading));
            Assert.AreEqual(ObservationKind.Cost, reading.Kind);
            Assert.AreEqual(ObservationUnit.Milliseconds, reading.Unit);
            Assert.AreEqual(2, reading.Count);
            Assert.GreaterOrEqual(reading.Peak, reading.Value);
            Assert.GreaterOrEqual(reading.Total, reading.Value);
            Assert.IsFalse(_context.TryRead(Failures, out _));
        }

        [Test]
        public void AFailedOrRefusedRunCountsUnderTheOperationLine() {
            _host.Run("fixture boom", _developer, _context);
            _host.Run("fixture secret", _developer, _context);
            Assert.IsTrue(_context.TryRead(Failures, out var reading));
            Assert.AreEqual(2, reading.Value);
            Assert.AreEqual(2, _context.ReadReasons(Failures, _reasons));
            var names = _reasons.ConvertAll(entry => entry.Reason.Name);
            CollectionAssert.Contains(names, "fixture boom");
            CollectionAssert.Contains(names, "fixture secret");
        }

        [Test]
        public void ALineThatDoesNotBindOrResolveRecordsNoGroup() {
            _host.Run("nothing here", _developer, _context);
            _host.Run("fixture add x", _developer, _context);
            Assert.IsFalse(_context.TryRead(Duration, out _));
            Assert.IsFalse(_context.TryRead(Failures, out _));
        }

        [Test]
        public void TheContextReachesTheOperation() {
            var run = _host.Run("fixture observe", _developer, _context);
            Assert.AreEqual("succeeded: observed " + _context.Name, run.Result.Text);
            Assert.AreSame(_context, FixtureOperations.LastObservations);
        }

        [Test]
        public void TheDiscardContextRecordsNothing() {
            _host.Run("fixture echo one", _developer, ObservationContext.Discard);
            Assert.IsFalse(ObservationContext.Discard.TryRead(Duration, out _));
        }

        [Test]
        public void RunAllRecordsEveryLineOfTheChain() {
            var chain = _host.RunAll(new[] { "fixture echo one", "fixture echo two" }, _developer, _context);
            Assert.IsTrue(chain.Result.Succeeded);
            Assert.IsTrue(_context.TryRead(Duration, out var reading));
            Assert.AreEqual(2, reading.Count);
        }

        [Test]
        public void TheGroupKeysAreDeclaredWhenTheHostIsMadeBeforeAnyRun() {
            var declared = CoreKit.Observations.Declared();
            var found = 0;

            foreach (var declaration in declared) {
                if (declaration.Key == Duration || declaration.Key == Failures) {
                    found++;
                }
            }

            Assert.AreEqual(2, found);
        }
    }
}
