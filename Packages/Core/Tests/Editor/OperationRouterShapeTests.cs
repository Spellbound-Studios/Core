// Copyright (c) Spellbound Studios. All rights reserved.

using System.Reflection;
using NUnit.Framework;
using Spellbound.Core.Logging;
using Spellbound.Core.Observations;
using Spellbound.Core.Operations;
using Spellbound.Core.Tests.OperationFixture;
using UnityEngine;

namespace Spellbound.Core.Tests {
    public class OperationRouterShapeTests {
        private static readonly Assembly Fixture = typeof(FixtureOperations).Assembly;
        private static readonly Assembly Core = typeof(OperationHost).Assembly;

        private OperationHost _host;
        private OperationCaller _developer;
        private RecordingLogSink _recording;
        private Log.SinkSuspension _suspension;

        [SetUp]
        public void SetUp() {
            _suspension = Log.SuspendSinks();
            _recording = new RecordingLogSink(LogLevel.Verbose);
            Log.AddSink(_recording, null, LogLevel.Verbose);
            _host = new OperationHost(new OperationCatalog(() => new[] { Fixture, Core }),
                () => ScriptableObject.CreateInstance<OperationSettings>());
            _developer = OperationCaller.Development();
            FixtureOperations.Reset();
        }

        [TearDown]
        public void TearDown() {
            Log.RemoveSink(_recording);
            _suspension.Dispose();
        }

        [Test]
        public void ANewHostHasNoRouter() {
            Assert.IsNull(_host.Router);
        }

        [Test]
        public void IOperationRouter_CustomImplementation() {
            var router = new CountingRouter();
            _host.Router = router;
            Assert.AreSame(router, _host.Router);
            _host.Router = null;
            Assert.IsNull(_host.Router);
        }

        [Test]
        public void RunHereOfALineAnswersAsRunDoes() {
            var here = _host.RunHere("fixture echo one", _developer, ObservationContext.Discard);
            var run = _host.Run("fixture echo one", _developer, ObservationContext.Discard);
            Assert.IsTrue(here.IsDone);
            Assert.AreEqual(OperationStatus.Succeeded, here.Result.Status);
            Assert.AreEqual(run.Result.Text, here.Result.Text);
        }

        [Test]
        public void RunHereOfAParsedInvocationAnswersAsRunDoes() {
            var invocation = _host.Parse("fixture echo one", null, out var failure);
            Assert.IsNull(failure);
            var here = _host.RunHere(invocation, _developer, ObservationContext.Discard);
            var run = _host.Run(invocation, _developer, ObservationContext.Discard);
            Assert.IsTrue(here.IsDone);
            Assert.AreEqual(OperationStatus.Succeeded, here.Result.Status);
            Assert.AreEqual(run.Result.Text, here.Result.Text);
        }

        [Test]
        public void RunHereRecordsInTheHostHistory() {
            _host.RunHere("fixture echo one", _developer, ObservationContext.Discard);
            Assert.AreEqual(1, _host.History.Count);
            Assert.AreEqual("fixture echo one", _host.History[0].Line);
        }

        [Test]
        public void RunHereNeverAsksTheRouter() {
            var router = new CountingRouter();
            _host.Router = router;
            var here = _host.RunHere("fixture stamp", _developer, ObservationContext.Discard);
            var invocation = _host.Parse("fixture stamp", null, out _);
            _host.RunHere(invocation, _developer, ObservationContext.Discard);
            Assert.AreEqual(OperationStatus.Succeeded, here.Result.Status);
            Assert.AreEqual(0, router.Asked);
        }

        private sealed class CountingRouter : IOperationRouter {
            public int Asked { get; private set; }

            public bool TryRoute(OperationInvocation invocation, OperationDescription description,
                OperationCaller caller, out OperationRun run) {
                Asked++;
                run = OperationRun.Completed(OperationResult.Success("routed"));
                return true;
            }
        }
    }
}
