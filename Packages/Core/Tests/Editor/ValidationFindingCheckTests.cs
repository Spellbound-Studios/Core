// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Spellbound.Core.Authoring;

namespace Spellbound.Core.Tests {
    public class ValidationFindingCheckTests {
        [Test]
        public void AFindingCarriesNoCheckByDefault() {
            var finding = new ValidationFinding("fixture.note", FindingSeverity.Information, "Assets/One.asset",
                "a note");
            Assert.IsNull(finding.Check);
        }

        [Test]
        public void WithCheckSetsTheCheckAndKeepsEverythingElse() {
            var hints = new Dictionary<string, string> { { "blender", "Rename it in Blender" } };
            var finding = new ValidationFinding("fixture.title-empty", FindingSeverity.Error, "Assets/One.asset",
                "the title is empty", "title", "Type a title.", "fixture.set-title", hints);
            var stamped = finding.WithCheck("fixture.threshold (Fixture)");
            Assert.AreEqual("fixture.threshold (Fixture)", stamped.Check);
            Assert.AreEqual(finding.Code, stamped.Code);
            Assert.AreEqual(finding.Severity, stamped.Severity);
            Assert.AreEqual(finding.Subject, stamped.Subject);
            Assert.AreEqual(finding.Location, stamped.Location);
            Assert.AreEqual(finding.Message, stamped.Message);
            Assert.AreEqual(finding.Hint, stamped.Hint);
            Assert.AreEqual(finding.FixCode, stamped.FixCode);
            Assert.AreSame(finding.NamedHints, stamped.NamedHints);
            Assert.AreEqual(finding.ToText(), stamped.ToText());
            Assert.IsNull(finding.Check);
            Assert.AreEqual("fixture.threshold (Fixture)", stamped.WithSubject("Assets/Two.asset").Check);
        }

        [Test]
        public void StampCheckStampsFromTheIndexAndOnlyFindingsWithoutACheck() {
            var report = new ValidationReport("Assets/One.asset");
            report.Error("fixture.first", "before the check");
            report.Add(new ValidationFinding("fixture.second", FindingSeverity.Warning, report.Subject,
                "already stamped", check: "fixture.other (Fixture)"));
            report.Information("fixture.third", "after the check");
            report.StampCheck(1, "fixture.threshold (Fixture)");
            Assert.IsNull(report.Findings[0].Check);
            Assert.AreEqual("fixture.other (Fixture)", report.Findings[1].Check);
            Assert.AreEqual("fixture.threshold (Fixture)", report.Findings[2].Check);
            report.StampCheck(3, "fixture.nothing (Fixture)");
            Assert.AreEqual(3, report.Findings.Count);
            Assert.Throws<ArgumentOutOfRangeException>(() => report.StampCheck(-1, "fixture.threshold (Fixture)"));
        }
    }
}
