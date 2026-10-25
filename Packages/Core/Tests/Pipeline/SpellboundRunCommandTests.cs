// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using NUnit.Framework;
using Spellbound.Core.Operations;
using Spellbound.Core.Pipeline;
using UnityEngine;

namespace Spellbound.Core.Tests.Pipeline {
    public class SpellboundRunCommandTests {
        [Test]
        public void ASucceededLineReturnsItsText() {
            var text = SpellboundRunCommand.Run("core help").GetAwaiter().GetResult();
            StringAssert.StartsWith("succeeded: ", text);
        }

        [Test]
        public void ALineThatDoesNotBindThrowsArgumentException() {
            var exception = Assert.Throws<ArgumentException>(() => SpellboundRunCommand.Run("core nope").GetAwaiter().GetResult());
            StringAssert.StartsWith("invalid: no operation 'core nope'", exception.Message);
        }

        [Test]
        public void AFailedLineThrowsInvalidOperationException() {
            var exception = Assert.Throws<InvalidOperationException>(() => SpellboundRunCommand.Run("fixture boom").GetAwaiter().GetResult());
            StringAssert.StartsWith("failed: No match score exists.", exception.Message);
        }

        [Test]
        public void ARefusedLineThrowsInvalidOperationException() {
            var exception = Assert.Throws<InvalidOperationException>(() => SpellboundRunCommand.Run("fixture wipe").GetAwaiter().GetResult());
            StringAssert.StartsWith("refused: this operation deletes data", exception.Message);
        }

        [Test]
        public void ADryRunReturnsThePreview() {
            StringAssert.StartsWith("previewed: would wipe 3 things", SpellboundRunCommand.Run("fixture wipe --dry-run").GetAwaiter().GetResult());
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
            var exception = Assert.Throws<InvalidOperationException>(() => SpellboundRunCommand.Run("fixture secret").GetAwaiter().GetResult());
            StringAssert.StartsWith("refused: fixture secret needs audience 'approver'", exception.Message);
        }

        [Test]
        public void TheDeclarationNamesTheCommand() {
#if SPELLBOUND_PIPELINE_0_8
            Assert.AreEqual("spellbound_run", SpellboundRunCommandPipeline08.CommandName);
#elif SPELLBOUND_PIPELINE_0_9
            Assert.AreEqual("spellbound run", SpellboundRunCommandPipeline09.Group + " " + SpellboundRunCommandPipeline09.CommandName);
#else
            Assert.Fail("no Pipeline range define is set");
#endif
        }
    }
}
