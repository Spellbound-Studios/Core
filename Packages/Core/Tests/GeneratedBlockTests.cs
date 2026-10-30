// Copyright (c) Spellbound Studios. All rights reserved.

using NUnit.Framework;
using Spellbound.Core.Operations;

namespace Spellbound.Core.Tests {
    public class GeneratedBlockTests {
        [Test]
        public void RenderWrapsTheBodyInDelimitersThatCarryTheHash() {
            var rendered = GeneratedBlock.Render("operations", "line one\nline two");
            Assert.AreEqual("<!-- spellbound:operations " + GeneratedBlock.Hash("line one\nline two") + " -->\nline one\nline two\n<!-- /spellbound:operations -->", rendered);
        }

        [Test]
        public void HashIgnoresLineEndingsAndSurroundingBlankLines() {
            Assert.AreEqual(GeneratedBlock.Hash("a\nb"), GeneratedBlock.Hash("\r\na\r\nb\r\n"));
            Assert.AreNotEqual(GeneratedBlock.Hash("a\nb"), GeneratedBlock.Hash("a\nc"));
        }

        [Test]
        public void BodyAndStoredHashReadBackWhatRenderWrote() {
            var text = "# Title\n\n" + GeneratedBlock.Render("operations", "body text") + "\n\ntrailing";
            Assert.AreEqual("body text", GeneratedBlock.Body(text, "operations"));
            Assert.AreEqual(GeneratedBlock.Hash("body text"), GeneratedBlock.StoredHash(text, "operations"));
            Assert.IsTrue(GeneratedBlock.IsCurrent(text, "operations"));
        }

        [Test]
        public void AnEditedBodyIsNotCurrent() {
            var text = GeneratedBlock.Render("operations", "body text").Replace("body text", "edited text");
            Assert.IsFalse(GeneratedBlock.IsCurrent(text, "operations"));
        }

        [Test]
        public void ReplaceKeepsTheTextAroundTheBlock() {
            var text = "before\n\n" + GeneratedBlock.Render("context", "old") + "\n\nafter\n";
            var replaced = GeneratedBlock.Replace(text, "context", "new");
            Assert.AreEqual("before\n\n" + GeneratedBlock.Render("context", "new") + "\n\nafter\n", replaced);
        }

        [Test]
        public void ReplaceAppendsWhenNoBlockExists() {
            Assert.AreEqual("notes\n\n" + GeneratedBlock.Render("context", "new") + "\n", GeneratedBlock.Replace("notes", "context", "new"));
            Assert.AreEqual(GeneratedBlock.Render("context", "new") + "\n", GeneratedBlock.Replace(string.Empty, "context", "new"));
        }

        [Test]
        public void ReplaceFollowsTheFileLineEndings() {
            var text = "before\r\n\r\n" + GeneratedBlock.Render("context", "old").Replace("\n", "\r\n") + "\r\n";
            var replaced = GeneratedBlock.Replace(text, "context", "new");
            StringAssert.Contains("\r\nnew\r\n<!-- /spellbound:context -->", replaced);
            StringAssert.DoesNotContain("\n\n", replaced.Replace("\r\n", string.Empty));
        }

        [Test]
        public void InsertUnderHeadingPutsANewBlockAtTheEndOfThatSection() {
            var text = "# Package\n\n## Front door\n\n`PackageKit`\n\n## Rules\n\nrules\n";
            var inserted = GeneratedBlock.InsertUnderHeading(text, "Front door", "operations", "ops");
            Assert.AreEqual("# Package\n\n## Front door\n\n`PackageKit`\n\n" + GeneratedBlock.Render("operations", "ops") + "\n\n## Rules\n\nrules\n", inserted);
        }

        [Test]
        public void InsertUnderHeadingReplacesAnExistingBlockWhereverItIs() {
            var text = "## Rules\n\n" + GeneratedBlock.Render("operations", "old") + "\n\n## Front door\n\ndoor\n";
            var inserted = GeneratedBlock.InsertUnderHeading(text, "Front door", "operations", "new");
            Assert.AreEqual("## Rules\n\n" + GeneratedBlock.Render("operations", "new") + "\n\n## Front door\n\ndoor\n", inserted);
        }

        [Test]
        public void InsertUnderHeadingAppendsWhenTheHeadingIsMissing() {
            var inserted = GeneratedBlock.InsertUnderHeading("## Rules\n\nrules\n", "Front door", "operations", "ops");
            Assert.AreEqual("## Rules\n\nrules\n\n" + GeneratedBlock.Render("operations", "ops") + "\n", inserted);
        }
    }
}
