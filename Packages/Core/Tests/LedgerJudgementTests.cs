// Copyright (c) Spellbound Studios. All rights reserved.

using NUnit.Framework;
using Spellbound.Core.UnityLedger;

namespace Spellbound.Core.Tests {
    public class LedgerJudgementTests {
        [Test]
        public void ASingleVersionIsAnEditorJudgement() {
            Assert.IsTrue(LedgerJudgement.TryParse(" 6000.4 ", out var judgement));
            Assert.IsTrue(judgement.IsEditor);
            Assert.IsNull(judgement.Package);
            Assert.AreEqual("6000.4", judgement.Version);
            Assert.AreEqual("6000.4", judgement.ToString());
        }

        [Test]
        public void APackageAndVersionIsAPackageJudgement() {
            Assert.IsTrue(LedgerJudgement.TryParse("com.unity.render-pipelines.universal 17.4.0", out var judgement));
            Assert.IsFalse(judgement.IsEditor);
            Assert.AreEqual("com.unity.render-pipelines.universal", judgement.Package);
            Assert.AreEqual("17.4.0", judgement.Version);
            Assert.AreEqual("com.unity.render-pipelines.universal 17.4.0", judgement.ToString());
        }

        [Test]
        public void OtherShapesDoNotParse() {
            Assert.IsFalse(LedgerJudgement.TryParse("soon", out _));
            Assert.IsFalse(LedgerJudgement.TryParse("a b c", out _));
            Assert.IsFalse(LedgerJudgement.TryParse("   ", out _));
            Assert.IsFalse(LedgerJudgement.TryParse(null, out _));
        }
    }
}
