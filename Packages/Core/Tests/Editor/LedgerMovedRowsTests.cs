// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections.Generic;
using NUnit.Framework;
using Spellbound.Core.Operations;
using Spellbound.Core.UnityLedger;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Spellbound.Core.Tests {
    public class LedgerMovedRowsTests {
        [Test]
        public void NoMovedRowOfCoreNamesATypeThatStillExists() {
            var package = PackageInfo.FindForAssembly(typeof(LedgerCheck).Assembly);
            Assert.IsNotNull(package, "Core's package was not found.");
            var section = AgentContextFile.Section(AgentContextFile.Read(package.resolvedPath),
                AgentContextFile.LedgerHeading);
            Assert.IsNotNull(section, "Core's AGENTS.md has no Ledger section.");
            var problems = new List<string>();
            var rows = LedgerTable.Parse(section, AgentContextFile.FileName, problems);
            CollectionAssert.IsEmpty(problems);

            var moved = new List<LedgerRow>();

            foreach (var row in rows) {
                if (row.Verdict == LedgerVerdict.Moved && row.OursType != null) {
                    moved.Add(row);
                }
            }

            Assert.IsNotEmpty(moved, "Core's ledger has no Moved row with a type to check.");
            var result = LedgerCheck.ForThisEditor().Run(moved, problems, 1);
            var wrong = new List<string>();

            foreach (var rowResult in result.Rows) {
                if (rowResult.IsWrong) {
                    wrong.Add(rowResult.Row.Id + ": " + string.Join("; ", rowResult.Lines));
                }
            }

            CollectionAssert.IsEmpty(wrong);
        }
    }
}
