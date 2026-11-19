// Copyright (c) Spellbound Studios. All rights reserved.

using System.Reflection;
using NUnit.Framework;
using Spellbound.Core.Logging;
using Spellbound.Core.Observations;
using Spellbound.Core.Operations;
using Spellbound.Core.Tests.OperationFixture;
using UnityEngine;

namespace Spellbound.Core.Tests {
    public class OperationRouterTests {
        private static readonly Assembly Fixture = typeof(FixtureOperations).Assembly;
        private static readonly Assembly Core = typeof(OperationHost).Assembly;

        private OperationHost _sender;
        private OperationHost _receiver;
        private HandingRouter _router;
        private OperationCaller _developer;
        private RecordingLogSink _recording;
        private Log.SinkSuspension _suspension;

        [SetUp]
        public void SetUp() {
            _suspension = Log.SuspendSinks();
            _recording = new RecordingLogSink(LogLevel.Verbose);
            Log.AddSink(_recording, null, LogLevel.Verbose);
            _sender = NewHost();
            _receiver = NewHost();
            _router = new HandingRouter(_receiver, ObservationContext.Discard);
            _developer = OperationCaller.Development();
            FixtureOperations.Reset();
        }

        [TearDown]
        public void TearDown() {
            Log.RemoveSink(_recording);
            _suspension.Dispose();
        }

        private static OperationHost NewHost() {
            return new OperationHost(new OperationCatalog(() => new[] { Fixture, Core }),
                () => ScriptableObject.CreateInstance<OperationSettings>());
        }

        private OperationRun Run(string line) => _sender.Run(line, _developer, ObservationContext.Discard);

        [Test]
        public void WithoutARouterAWritingLineRunsHereAndIsRecorded() {
            var run = Run("fixture stamp");
            Assert.AreEqual(OperationStatus.Succeeded, run.Result.Status);
            Assert.AreEqual(1, FixtureOperations.StampCount);
            Assert.AreEqual(1, _sender.History.Count);
            Assert.AreEqual("fixture stamp", _sender.History[0].Line);
        }

        [Test]
        public void ARoutedWritingLineIsRecordedOnTheReceivingHostOnly() {
            _sender.Router = _router;
            var run = Run("fixture stamp");
            Assert.AreEqual(1, _router.Calls);
            Assert.AreEqual(OperationStatus.Succeeded, run.Result.Status);
            Assert.AreEqual(1, FixtureOperations.StampCount);
            Assert.AreEqual(0, _sender.History.Count);
            Assert.AreEqual(1, _receiver.History.Count);
            Assert.AreEqual("fixture stamp", _receiver.History[0].Line);
        }

        [Test]
        public void AReadingLineNeverReachesTheRouter() {
            _sender.Router = _router;
            var run = Run("fixture echo one");
            Assert.AreEqual(0, _router.Calls);
            Assert.AreEqual("succeeded: one", run.Result.Text);
            Assert.AreEqual(1, _sender.History.Count);
            Assert.AreEqual(0, _receiver.History.Count);
        }

        [Test]
        public void ADryRunOfAnOperationThatPreviewsReachesTheRouter() {
            _sender.Router = _router;
            var run = Run("fixture wipe --dry-run");
            Assert.AreEqual(1, _router.Calls);
            Assert.IsTrue(_router.LastInvocation.DryRun);
            Assert.AreEqual("previewed: would wipe 3 things", run.Result.Text);
            Assert.AreEqual(0, FixtureOperations.WipeCount);
            Assert.AreEqual(0, _sender.History.Count);
            Assert.AreEqual(1, _receiver.History.Count);
        }

        [Test]
        public void AConfirmedDestroyingLineReachesTheRouterOnce() {
            _sender.Router = _router;
            var run = Run("fixture purge --confirm");
            Assert.AreEqual(1, _router.Calls);
            Assert.IsTrue(_router.LastInvocation.Confirmed);
            Assert.AreEqual("succeeded: purged 3 things", run.Result.Text);
            Assert.AreEqual(1, FixtureOperations.PurgeCount);
            Assert.AreEqual(0, _sender.History.Count);
            Assert.AreEqual(1, _receiver.History.Count);
        }

        [Test]
        public void ARouterThatWouldBounceTheCallBackIsNeverAskedOnRunHere() {
            var bouncing = new BouncingRouter(_sender, ObservationContext.Discard);
            _sender.Router = _router;
            _receiver.Router = bouncing;
            var run = Run("fixture stamp");
            Assert.AreEqual(1, _router.Calls);
            Assert.AreEqual(0, bouncing.Calls);
            Assert.AreEqual(OperationStatus.Succeeded, run.Result.Status);
            Assert.AreEqual(1, FixtureOperations.StampCount);
            Assert.AreEqual(1, _receiver.History.Count);
            Assert.AreEqual(0, _sender.History.Count);
        }

        [Test]
        public void ARoutedInvocationCarriesItsIdentifierAndTypedValues() {
            var sendingCounter = new FixtureLiveCounter();
            var receivingCounter = new FixtureLiveCounter();
            _sender.Catalog.Register(sendingCounter);
            _receiver.Catalog.Register(receivingCounter);
            _sender.Router = _router;
            var description = _sender.Catalog.Describe("fixture", "count-up");
            var invocation = new OperationInvocation(description.Identifier, new object[] { 5 });
            var run = _sender.Run(invocation, _developer, ObservationContext.Discard);
            Assert.AreEqual(1, _router.Calls);
            Assert.AreEqual(description.Identifier, _router.LastInvocation.Identifier);
            CollectionAssert.AreEqual(new object[] { 5 }, _router.LastInvocation.Values);
            Assert.AreSame(description, _router.LastDescription);
            Assert.AreEqual("succeeded: 5", run.Result.Text);
            Assert.AreEqual(0, sendingCounter.Count);
            Assert.AreEqual(5, receivingCounter.Count);
        }

        [Test]
        public void ALineTheAudienceRefusesNeverReachesTheRouter() {
            _sender.Router = _router;
            var run = Run("fixture secret");
            Assert.AreEqual(OperationStatus.Refused, run.Result.Status);
            Assert.AreEqual(0, _router.Calls);
            Assert.AreEqual(1, _sender.History.Count);
        }

        [Test]
        public void AnUnconfirmedDestroyingLineNeverReachesTheRouter() {
            _sender.Router = _router;
            var run = Run("fixture purge");
            Assert.AreEqual(OperationStatus.Refused, run.Result.Status);
            Assert.AreEqual(0, _router.Calls);
            Assert.AreEqual(0, FixtureOperations.PurgeCount);
            Assert.AreEqual(1, _sender.History.Count);
        }
    }
}
