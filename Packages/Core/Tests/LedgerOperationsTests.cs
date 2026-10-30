// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections.Generic;
using NUnit.Framework;
using Spellbound.Core.Observations;
using Spellbound.Core.Operations;
using Spellbound.Core.UnityLedger;

namespace Spellbound.Core.Tests {
    public class LedgerOperationsTests {
        [Test]
        public void UnityLedgerReadsThisProjectsLedgers() {
            var host = new OperationHost();
            var problems = new List<string>();
            var rows = LedgerSources.All(host.Settings, problems);
            var expected = LedgerCheck.ForThisEditor().Run(rows, problems,
                LedgerSources.SourceNames(host.Settings).Count);
            var text = host.Run("core unity-ledger", OperationCaller.Development(), ObservationContext.Discard).Result.Text;
            var firstWord = text.Split(':')[0];

            Assert.AreEqual(expected.Passed ? "succeeded" : "failed", firstWord, text);
            StringAssert.Contains(expected.Summary(), text);
            Assert.Greater(expected.SourceCount, 0);
        }

        [Test]
        public void UnityLedgerIsInTheCoreGroup() {
            Assert.IsNotNull(new OperationHost().Catalog.Describe("core", "unity-ledger"));
        }
    }
}
