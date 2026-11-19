// Copyright (c) Spellbound Studios. All rights reserved.

using System.Reflection;
using NUnit.Framework;
using Spellbound.Core.Logging;
using Spellbound.Core.Operations;
using Spellbound.Core.Tests.OperationFixture;
using UnityEngine;

namespace Spellbound.Core.Tests {
    public class OperationTargetShapeTests {
        private static readonly Assembly Fixture = typeof(FixtureOperations).Assembly;
        private static readonly Assembly Core = typeof(OperationHost).Assembly;

        private OperationHost _host;
        private RecordingLogSink _recording;
        private Log.SinkSuspension _suspension;

        [SetUp]
        public void SetUp() {
            _suspension = Log.SuspendSinks();
            _recording = new RecordingLogSink(LogLevel.Verbose);
            Log.AddSink(_recording, null, LogLevel.Verbose);
            _host = new OperationHost(new OperationCatalog(() => new[] { Fixture, Core }),
                () => ScriptableObject.CreateInstance<OperationSettings>());
            FixtureOperations.Reset();
        }

        [TearDown]
        public void TearDown() {
            Log.RemoveSink(_recording);
            _suspension.Dispose();
        }

        [Test]
        public void TheArgumentMarkIsOffByDefault() {
            var argument = new ArgumentAttribute("The thing acted on.");
            Assert.IsFalse(argument.Target);
            Assert.AreEqual("The thing acted on.", argument.Description);
        }

        [Test]
        public void TheArgumentMarkIsSettableByName() {
            var argument = new ArgumentAttribute("The thing acted on.") { Target = true };
            Assert.IsTrue(argument.Target);
        }

        [Test]
        public void AnOperationWithoutAMarkDescribesNoTarget() {
            var description = _host.Catalog.Describe("fixture", "echo");
            Assert.IsNull(description.Target);
            Assert.AreEqual(1, description.Arguments.Count);
            Assert.IsFalse(description.Arguments[0].IsTarget);
            Assert.AreEqual(0, _recording.CountOf(LogLevel.Error));
        }

        [Test]
        public void AnInvocationCarriesNoTargetByDefault() {
            var identifier = _host.Catalog.Describe("fixture", "echo").Identifier;
            var invocation = new OperationInvocation(identifier, new object[] { "one" });
            Assert.IsNull(invocation.Target);
        }

        [Test]
        public void TheTrailingParameterSetsTheInvocationTarget() {
            var identifier = _host.Catalog.Describe("fixture", "echo").Identifier;
            var invocation = new OperationInvocation(identifier, new object[] { "one" }, "fixture echo one",
                false, false, "one");
            Assert.AreEqual("one", invocation.Target);
            Assert.AreEqual("fixture echo one", invocation.Line);
            CollectionAssert.AreEqual(new object[] { "one" }, invocation.Values);
        }
    }
}
