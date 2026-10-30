// Copyright (c) Spellbound Studios. All rights reserved.

using NUnit.Framework;
using Spellbound.Core.UnityLedger;

namespace Spellbound.Core.Tests {
    public class LedgerStalenessTests {
        private const string Search = "https://docs.unity3d.com/6000.7/Documentation/Manual/30_search.html?q=";

        [Test]
        public void EditorMajorMinorDropsThePatchAndTheBuild() {
            Assert.AreEqual("6000.4", LedgerStaleness.EditorMajorMinor("6000.4.8f1"));
            Assert.AreEqual("6000.7", LedgerStaleness.EditorMajorMinor("6000.7.0b6"));
            Assert.AreEqual("6000.4", LedgerStaleness.EditorMajorMinor("6000.4"));
            Assert.IsNull(LedgerStaleness.EditorMajorMinor("six"));
        }

        [Test]
        public void EditorVersionsBetweenListsEveryMinorPassed() {
            CollectionAssert.AreEqual(new[] { "6000.5", "6000.6", "6000.7" },
                LedgerStaleness.EditorVersionsBetween("6000.4", "6000.7"));
            CollectionAssert.AreEqual(new[] { "6000.5", "6000.6", "6000.7" },
                LedgerStaleness.EditorVersionsBetween("6000.4", LedgerStaleness.EditorMajorMinor("6000.7.0b6")));
        }

        [Test]
        public void TheSameOrANewerJudgementGivesNothing() {
            CollectionAssert.IsEmpty(LedgerStaleness.EditorVersionsBetween("6000.4", "6000.4"));
            CollectionAssert.IsEmpty(LedgerStaleness.EditorVersionsBetween("6000.5", "6000.4"));
        }

        [Test]
        public void ANewerMajorGivesJustTheCurrent() {
            CollectionAssert.AreEqual(new[] { "7000.0" }, LedgerStaleness.EditorVersionsBetween("6000.4", "7000.0"));
        }

        [Test]
        public void TheSearchAddressEncodesTheWords() {
            var words = new[] { "BatchRendererGroup", "GPU Resident Drawer" };

            Assert.AreEqual(Search + "BatchRendererGroup+GPU+Resident+Drawer",
                LedgerStaleness.DocumentationSearchAddress("6000.7", words));
            Assert.AreEqual(Search + "Pool.ObjectPool%3CT%3E",
                LedgerStaleness.DocumentationSearchAddress("6000.7", new[] { "Pool.ObjectPool<T>" }));
        }

        [Test]
        public void TheSearchAddressWithNoWordsAsksWhatIsNew() {
            Assert.AreEqual("https://docs.unity3d.com/6000.4/Documentation/Manual/30_search.html?q=What%27s+new",
                LedgerStaleness.DocumentationSearchAddress("6000.4", new string[0]));
        }
    }
}
