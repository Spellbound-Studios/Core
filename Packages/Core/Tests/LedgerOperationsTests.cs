// Copyright (c) Spellbound Studios. All rights reserved.

using NUnit.Framework;
using Spellbound.Core.Operations;

namespace Spellbound.Core.Tests {
    public class LedgerOperationsTests {
        [Test]
        public void UnityLedgerReadsThisProjectsLedgers() {
            var text = new OperationHost().Run("core unity-ledger", OperationCaller.Development()).Result.Text;
            var firstWord = text.Split(':')[0];

            Assert.That(firstWord, Is.EqualTo("succeeded").Or.EqualTo("failed"), text);
            StringAssert.Contains("rows stale", text);
        }

        [Test]
        public void UnityLedgerIsInTheCoreGroup() {
            Assert.IsNotNull(new OperationHost().Catalog.Describe("core", "unity-ledger"));
        }
    }
}
