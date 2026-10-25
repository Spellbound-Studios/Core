// Copyright (c) Spellbound Studios. All rights reserved.

using System.IO;
using NUnit.Framework;
using Spellbound.Core.Operations.Editor;

namespace Spellbound.Core.Tests {
    public class PipelineBridgeCheckTests {
        [Test]
        public void TheRangesAreReadFromTheBridgeAssemblyDefinition() {
            var path = PipelineBridgeRanges.AssemblyDefinitionPath();
            Assert.IsTrue(File.Exists(path), path);
            CollectionAssert.AreEqual(new[] { "[0.8.0-exp.1,0.9.0-exp.1)", "[0.9.0-exp.1,0.10.0-exp.1)" }, PipelineBridgeRanges.Read());
        }

        [Test]
        public void ReadFindsEveryPipelineExpressionInTheText() {
            const string text = "{ \"versionDefines\": [ { \"name\": \"com.unity.pipeline\", \"expression\": \"[1,2)\", \"define\": \"A\" }, { \"name\": \"com.other\", \"expression\": \"[3,4)\", \"define\": \"B\" } ] }";
            CollectionAssert.AreEqual(new[] { "[1,2)" }, PipelineBridgeRanges.Read(text));
            CollectionAssert.IsEmpty(PipelineBridgeRanges.Read(string.Empty));
        }

        [Test]
        public void NoMessageWithoutPipelineOrWithTheBridgeLoaded() {
            var ranges = new[] { "[0.8.0-exp.1,0.9.0-exp.1)" };
            Assert.IsNull(PipelineBridgeCheck.Describe(null, false, ranges));
            Assert.IsNull(PipelineBridgeCheck.Describe("0.7.0-exp.1", true, ranges));
        }

        [Test]
        public void TheMessageNamesTheVersionAndTheRanges() {
            var message = PipelineBridgeCheck.Describe("0.7.0-exp.1", false, new[] { "[0.8.0-exp.1,0.9.0-exp.1)", "[0.9.0-exp.1,0.10.0-exp.1)" });
            Assert.AreEqual("Spellbound agent road off: com.unity.pipeline 0.7.0-exp.1 is outside the supported [0.8.0-exp.1,0.9.0-exp.1) or [0.9.0-exp.1,0.10.0-exp.1); update Spellbound.Core or install a Pipeline inside [0.8.0-exp.1,0.9.0-exp.1) or [0.9.0-exp.1,0.10.0-exp.1).", message);
        }

        [Test]
        public void TheBridgeIsLoadedExactlyWhenTheInstalledPipelineIsInsideARange() {
            var installed = EditorInstalledPackages.VersionOf(PipelineBridgeRanges.PipelinePackageName);
            var expected = PipelineBridgeRanges.Supports(installed, PipelineBridgeRanges.Read());
            Assert.AreEqual(expected, PipelineBridgeCheck.BridgeLoaded(), "installed com.unity.pipeline: " + (installed ?? "none"));
        }
    }
}
