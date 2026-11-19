// Copyright (c) Spellbound Studios. All rights reserved.

using NUnit.Framework;
using Spellbound.Core.UnityLedger;

namespace Spellbound.Core.Tests {
    public class LedgerSourcesTests {
        private const string Sample = "# Spellbound.Sample\r\n\r\n## Purpose\r\n\r\nDoes one thing.\r\n\r\n"
                                      + "## Ledger\r\n\r\n"
                                      + "- Builds on `UnityEngine.Pool.ObjectPool<T>`: pooling.\r\n"
                                      + "- Moved `Thing` to Spellbound.Other.\r\n";

        [Test]
        public void RowsInAreTheBulletsUnderLedger() {
            CollectionAssert.AreEqual(
                new[] { "Builds on `UnityEngine.Pool.ObjectPool<T>`: pooling.", "Moved `Thing` to Spellbound.Other." },
                LedgerSources.RowsIn(Sample));
            Assert.IsNull(LedgerSources.RowsIn("## Purpose\n\nx"));
        }

        [Test]
        public void RowsInRenderTableRowsBesideBullets() {
            var text = "## Ledger\r\n\r\n- Builds on `UnityEngine.Pool.ObjectPool<T>`: pooling.\r\n\r\n"
                       + "| Id | Ours | Ours (type) | Unity item | Verdict | Judged at | Reason | Evidence "
                       + "| Watch packages | Watch words |\r\n|---|---|---|---|---|---|---|---|---|---|\r\n"
                       + "| U-004 | Core ObjectPool | | UnityEngine.Pool.ObjectPool | Moved | 6000.4 | r | e | | |\r\n";

            CollectionAssert.AreEqual(new[] {
                "Builds on `UnityEngine.Pool.ObjectPool<T>`: pooling.",
                "U-004 Core ObjectPool: Moved (UnityEngine.Pool.ObjectPool)"
            }, LedgerSources.RowsIn(text));
        }
    }
}
