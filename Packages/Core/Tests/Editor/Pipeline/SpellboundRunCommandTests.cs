// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Spellbound.Core.Observations;
using Spellbound.Core.Operations;
using Spellbound.Core.Pipeline;
using UnityEngine;

namespace Spellbound.Core.Tests.Pipeline {
    public class SpellboundRunCommandTests {
        private const string EchoLine = "fixture echo one";
        private const string FixtureDurationKey = "fixture.operations";

        private readonly List<ObservationContext> _contexts = new List<ObservationContext>();

        [TearDown]
        public void TearDown() {
            foreach (var context in _contexts) {
                context.Dispose();
            }

            _contexts.Clear();
        }

        [Test]
        public void ASucceededLineReturnsItsText() {
            var text = Answer("core help", null);
            StringAssert.StartsWith("succeeded: ", text);
        }

        [Test]
        public void ALineThatDoesNotBindThrowsArgumentException() {
            var exception = Assert.Throws<ArgumentException>(() => Answer("core nope", null));
            StringAssert.StartsWith("invalid: no operation 'core nope'", exception.Message);
        }

        [Test]
        public void AFailedLineThrowsInvalidOperationException() {
            var exception = Assert.Throws<InvalidOperationException>(() => Answer("fixture boom", null));
            StringAssert.StartsWith("failed: No match score exists.", exception.Message);
        }

        [Test]
        public void ARefusedLineThrowsInvalidOperationException() {
            var exception = Assert.Throws<InvalidOperationException>(() => Answer("fixture wipe", null));
            StringAssert.StartsWith("refused: this operation deletes data", exception.Message);
        }

        [Test]
        public void ADryRunReturnsThePreview() {
            StringAssert.StartsWith("previewed: would wipe 3 things", Answer("fixture wipe --dry-run", null));
        }

        [Test]
        public void TheCallerHoldsTheBridgeAudiences() {
            var settings = ScriptableObject.CreateInstance<OperationSettings>();
            Assert.IsTrue(PipelineCaller.From(settings).Holds(OperationAudiences.Developer));
            Assert.IsFalse(PipelineCaller.From(settings).Holds(OperationAudiences.Approver));
            settings.bridgeAudiences = new[] { OperationAudiences.Developer, OperationAudiences.Approver };
            Assert.IsTrue(PipelineCaller.From(settings).Holds(OperationAudiences.Approver));
            Assert.AreEqual("pipeline", PipelineCaller.Current.Name);
        }

        [Test]
        public void TheApproverAudienceIsRefusedUntilTheSettingGrantsIt() {
            var exception = Assert.Throws<InvalidOperationException>(() => Answer("fixture secret", null));
            StringAssert.StartsWith("refused: fixture secret needs audience 'approver'", exception.Message);
        }

        [Test]
        public void WithNoLiveWorldTheLineRunsAndNothingIsRecorded() {
            Assume.That(ObservationContexts.All, Is.Empty, "a live context from elsewhere");
            StringAssert.StartsWith("succeeded: ", Answer(EchoLine, null));
        }

        [Test]
        public void TheOneLiveWorldReceivesTheGroupDuration() {
            Assume.That(ObservationContexts.All, Is.Empty, "a live context from elsewhere");
            var world = World("only");
            Answer(EchoLine, null);
            Assert.IsTrue(world.TryRead(DurationKey(), out var reading));
            Assert.AreEqual(1, reading.Count);
        }

        [Test]
        public void SeveralLiveWorldsRecordNothingUntilOneIsNamed() {
            var first = World("first");
            var second = World("second");
            Answer(EchoLine, null);
            Assert.IsFalse(HasDuration(first));
            Assert.IsFalse(HasDuration(second));

            Answer(EchoLine, "second");
            Assert.IsFalse(HasDuration(first));
            Assert.IsTrue(HasDuration(second));
        }

        [Test]
        public void AWorldThatIsNotLiveThrowsArgumentExceptionNamingTheLiveWorlds() {
            World("first");
            World("second");
            var exception = Assert.Throws<ArgumentException>(() => Answer(EchoLine, "nowhere"));
            StringAssert.StartsWith("invalid: no world named 'nowhere' is live", exception.Message);
            StringAssert.Contains("\nLive worlds: ", exception.Message);
            StringAssert.EndsWith("first, second", exception.Message);
        }

        [Test]
        public void TheDeclarationNamesTheCommand() {
#if SPELLBOUND_PIPELINE_0_8
            Assert.AreEqual("spellbound_run", SpellboundRunCommandPipeline08.CommandName);
#elif SPELLBOUND_PIPELINE_0_9
            Assert.AreEqual("spellbound run",
                SpellboundRunCommandPipeline09.Group + " " + SpellboundRunCommandPipeline09.CommandName);
#elif SPELLBOUND_PIPELINE_0_10
            Assert.AreEqual("spellbound run",
                SpellboundRunCommandPipeline10.Group + " " + SpellboundRunCommandPipeline10.CommandName);
#else
            Assert.Fail("no Pipeline range define is set");
#endif
        }

        private static string Answer(string line, string world) {
            return SpellboundRunCommand.Run(line, world).GetAwaiter().GetResult();
        }

        private ObservationContext World(string name) {
            var context = CoreKit.Observations.CreateContext(name);
            _contexts.Add(context);
            return context;
        }

        private static bool HasDuration(ObservationContext context) => context.TryRead(DurationKey(), out _);

        private static ObservationKey DurationKey() {
            Assert.IsTrue(ObservationKey.TryParse(FixtureDurationKey, out var key, out var error), error);
            return key;
        }
    }
}
