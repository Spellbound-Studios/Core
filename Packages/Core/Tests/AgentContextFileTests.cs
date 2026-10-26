// Copyright (c) Spellbound Studios. All rights reserved.

using NUnit.Framework;
using Spellbound.Core.Operations;

namespace Spellbound.Core.Tests {
    public class AgentContextFileTests {
        private const string Sample = "# Spellbound.Sample\r\n\r\n## Purpose\r\n\r\nDoes one thing.\r\n\r\n## Front door\r\n\r\n`SampleKit`\r\n\r\n## Rules\r\n\r\n- one\r\n\r\n## Not for\r\n\r\n- two\r\n\r\n## Gaps\r\n\r\n## Ledger\r\n\r\n- Builds on `UnityEngine.Pool.ObjectPool<T>`: pooling.\r\n- Moved `Thing` to Spellbound.Other.\r\n";

        [Test]
        public void SectionReturnsTheBodyUntilTheNextHeading() {
            Assert.AreEqual("Does one thing.", AgentContextFile.Section(Sample, "Purpose"));
            Assert.AreEqual(string.Empty, AgentContextFile.Section(Sample, "Gaps"));
            Assert.IsNull(AgentContextFile.Section(Sample, "Nowhere"));
        }

        [Test]
        public void FrontDoorIsTheFirstBacktickedIdentifier() {
            Assert.AreEqual("SampleKit", AgentContextFile.FrontDoor(Sample));
            Assert.IsNull(AgentContextFile.FrontDoor("## Front door\n\nnone named"));
        }

        [Test]
        public void MissingHeadingsNamesEachAbsentOne() {
            CollectionAssert.IsEmpty(AgentContextFile.MissingHeadings(Sample));
            CollectionAssert.AreEqual(new[] { "Purpose", "Front door", "Rules", "Not for", "Gaps", "Ledger" }, AgentContextFile.MissingHeadings("# Nothing"));
        }

        [Test]
        public void LedgerRowsAreTheBulletsUnderLedger() {
            CollectionAssert.AreEqual(new[] { "Builds on `UnityEngine.Pool.ObjectPool<T>`: pooling.", "Moved `Thing` to Spellbound.Other." }, AgentContextFile.LedgerRows(Sample));
            Assert.IsNull(AgentContextFile.LedgerRows("## Purpose\n\nx"));
        }

        [Test]
        public void LedgerRowsRenderTableRowsBesideBullets() {
            var text = "## Ledger\r\n\r\n- Builds on `UnityEngine.Pool.ObjectPool<T>`: pooling.\r\n\r\n"
                       + "| Id | Ours | Ours (type) | Unity item | Verdict | Judged at | Reason | Evidence "
                       + "| Watch packages | Watch words |\r\n|---|---|---|---|---|---|---|---|---|---|\r\n"
                       + "| U-004 | Core ObjectPool | | UnityEngine.Pool.ObjectPool | Moved | 6000.4 | r | e | | |\r\n";

            CollectionAssert.AreEqual(new[] {
                "Builds on `UnityEngine.Pool.ObjectPool<T>`: pooling.",
                "U-004 Core ObjectPool: Moved (UnityEngine.Pool.ObjectPool)"
            }, AgentContextFile.LedgerRows(text));
        }
    }
}
