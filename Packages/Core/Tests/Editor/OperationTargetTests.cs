// Copyright (c) Spellbound Studios. All rights reserved.

using System.Reflection;
using NUnit.Framework;
using Spellbound.Core.Logging;
using Spellbound.Core.Observations;
using Spellbound.Core.Operations;
using Spellbound.Core.Tests.OperationFixture;
using UnityEngine;

namespace Spellbound.Core.Tests {
    public class OperationTargetTests {
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
        public void AMarkedParameterIsTheOperationTarget() {
            var description = _host.Catalog.Describe("fixture", "inspect");
            Assert.IsNotNull(description.Target);
            Assert.AreEqual("instance", description.Target.Name);
            Assert.IsTrue(description.Target.IsTarget);
            Assert.AreSame(description.Arguments[0], description.Target);

            foreach (var argument in description.Arguments) {
                Assert.AreEqual(argument == description.Target, argument.IsTarget, argument.Name);
            }

            Assert.AreEqual(2, description.Arguments.Count);
            Assert.AreEqual(0, _recording.CountOf(LogLevel.Error));
        }

        [Test]
        public void AnOperationWithoutAMarkHasNoTarget() {
            var description = _host.Catalog.Describe("fixture", "echo");
            Assert.IsNull(description.Target);
        }

        [Test]
        public void TwoMarkedParametersLogOneErrorAndKeepTheFirst() {
            var twoTargets = OperationCatalogTests.DynamicOperations.AssemblyWithTwoTargets("Zeta.TwoTargets",
                "fixture", "two-targets", "source", "destination");
            var catalog = new OperationCatalog(() => new[] { Fixture, twoTargets, Core });
            var description = catalog.Describe("fixture", "two-targets");
            Assert.IsNotNull(description);
            Assert.AreEqual(1, _recording.CountOf(LogLevel.Error));
            var message = _recording.Entries[0].Message;
            StringAssert.Contains("fixture two-targets", message);
            StringAssert.Contains("source", message);
            StringAssert.Contains("destination", message);
            Assert.AreEqual("source", description.Target.Name);
            Assert.IsTrue(description.Arguments[0].IsTarget);
            Assert.IsFalse(description.Arguments[1].IsTarget);
        }

        [Test]
        public void ParseCarriesTheTypedTargetValue() {
            var invocation = _host.Parse("fixture inspect --instance 7", null, out var failure);
            Assert.IsNull(failure);
            Assert.AreEqual(7, invocation.Target);
        }

        [Test]
        public void ParseOfAnOperationWithoutAMarkCarriesNoTarget() {
            var invocation = _host.Parse("fixture echo one", null, out var failure);
            Assert.IsNull(failure);
            Assert.IsNull(invocation.Target);
        }

        [Test]
        public void AnInvocationWithoutATargetRunsAndIsHandedOnCarryingIt() {
            var description = _host.Catalog.Describe("fixture", "inspect");
            var invocation = new OperationInvocation(description.Identifier, new object[] { 7, "size" });
            var run = _host.Run(invocation, _developer, ObservationContext.Discard);
            Assert.AreEqual(OperationStatus.Succeeded, run.Result.Status);
            Assert.AreEqual("succeeded: 7 size", run.Result.Text);
            var handedOn = OperationHost.CarryingTarget(invocation, description, new object[] { 7, "size" });
            Assert.AreEqual(7, handedOn.Target);
            Assert.AreEqual(invocation.Identifier, handedOn.Identifier);
            CollectionAssert.AreEqual(invocation.Values, handedOn.Values);
        }

        [Test]
        public void AnInvocationThatCarriesATargetIsHandedOnUnchanged() {
            var description = _host.Catalog.Describe("fixture", "inspect");
            var invocation = new OperationInvocation(description.Identifier, new object[] { 7, "size" }, null, false,
                false, 7);
            Assert.AreSame(invocation,
                OperationHost.CarryingTarget(invocation, description, new object[] { 7, "size" }));
        }
    }
}
