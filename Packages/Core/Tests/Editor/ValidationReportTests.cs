// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using NUnit.Framework;
using Spellbound.Core.Authoring;

namespace Spellbound.Core.Tests {
    public class ValidationReportTests {
        [Test]
        public void AFindingWritesCodeFirstThenSeveritySubjectLocationMessageHintAndFix() {
            var finding = new ValidationFinding("fixture.title-empty", FindingSeverity.Error, "Assets/One.asset", "the title is empty", "title",
                "Type a title.", "fixture.set-title");
            Assert.AreEqual("fixture.title-empty error Assets/One.asset at title: the title is empty (Type a title.) fix: fixture.set-title", finding.ToText());
            Assert.AreEqual("fixture.note information: a note", new ValidationFinding("fixture.note", FindingSeverity.Information, null, "a note").ToText());
        }

        [Test]
        public void ACodeIsPackageDotKebabNameInLowerCase() {
            Assert.IsTrue(ValidationFinding.IsCode("navigation.reachable-deposits"));
            Assert.IsTrue(ValidationFinding.IsCode("core.unknown-key"));
            Assert.IsFalse(ValidationFinding.IsCode("NoDot"));
            Assert.IsFalse(ValidationFinding.IsCode("Core.UnknownKey"));
            Assert.IsFalse(ValidationFinding.IsCode(".leading"));
            Assert.IsFalse(ValidationFinding.IsCode("trailing."));
            Assert.IsFalse(ValidationFinding.IsCode("has space.x"));
            Assert.IsFalse(ValidationFinding.IsCode(null));
            Assert.Throws<ArgumentException>(() => new ValidationFinding("NoDot", FindingSeverity.Error, "s", "m"));
            Assert.Throws<ArgumentException>(() => new ValidationFinding("fixture.ok", FindingSeverity.Error, "s", " "));
        }

        [Test]
        public void TheReportOrdersErrorsThenWarningsThenInformationKeepingInsertionOrderWithinEach() {
            var report = new ValidationReport("Assets/One.asset");
            report.Information("fixture.first-note", "first note");
            report.Warning("fixture.first-warning", "first warning");
            report.Error("fixture.first-error", "first error");
            report.Information("fixture.second-note", "second note");
            report.Error("fixture.second-error", "second error");
            var lines = report.ToText().Split('\n');
            Assert.AreEqual(5, lines.Length);
            StringAssert.StartsWith("fixture.first-error error", lines[0]);
            StringAssert.StartsWith("fixture.second-error error", lines[1]);
            StringAssert.StartsWith("fixture.first-warning warning", lines[2]);
            StringAssert.StartsWith("fixture.first-note information", lines[3]);
            StringAssert.StartsWith("fixture.second-note information", lines[4]);
            Assert.AreEqual(5, report.Findings.Count);
            Assert.AreEqual("fixture.first-note", report.Findings[0].Code);
        }

        [Test]
        public void TheReportCountsAndSummarises() {
            var report = new ValidationReport("Assets/One.asset");
            Assert.AreEqual("no findings", report.Summary());
            Assert.IsTrue(report.IsEmpty);
            Assert.IsFalse(report.HasErrors);
            report.Error("fixture.a", "a");
            report.Error("fixture.b", "b");
            report.Warning("fixture.c", "c");
            report.Information("fixture.d", "d");
            report.Information("fixture.e", "e");
            Assert.AreEqual("2 errors, 1 warning, 2 information", report.Summary());
            Assert.IsTrue(report.HasErrors);
            Assert.AreEqual(2, report.Count(FindingSeverity.Error));
            Assert.AreEqual(1, report.Count(FindingSeverity.Warning));
            Assert.AreEqual(2, report.Count(FindingSeverity.Information));
        }

        [Test]
        public void ConvenienceMethodsStampTheReportSubject() {
            var report = new ValidationReport("Assets/One.asset");
            var finding = report.Warning("fixture.w", "w", "field");
            Assert.AreEqual("Assets/One.asset", finding.Subject);
            Assert.AreEqual("field", finding.Location);
            Assert.AreEqual("Assets/Two.asset", finding.WithSubject("Assets/Two.asset").Subject);
        }

        [Test]
        public void MergeTakesTheOtherReportsFindingsAndIgnoresItself() {
            var first = new ValidationReport("Assets/One.asset");
            first.Error("fixture.a", "a");
            var second = new ValidationReport("Assets/Two.asset");
            second.Warning("fixture.b", "b");
            first.Merge(second);
            first.Merge(first);
            first.Merge(null);
            Assert.AreEqual(2, first.Findings.Count);
            Assert.AreEqual("Assets/Two.asset", first.Findings[1].Subject);
        }
    }
}
