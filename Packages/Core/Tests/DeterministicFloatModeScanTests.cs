// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.IO;
using NUnit.Framework;
using Spellbound.Core.Testing;

namespace Spellbound.Core.Tests {
    public class DeterministicFloatModeScanTests {
        private const string ListedJobSource =
            "using Unity.Burst;\nusing Unity.Jobs;\n\n"
            + "[BurstCompile(FloatMode = FloatMode.Deterministic)]\npublic struct ListedJob : IJob {\n"
            + "    public void Execute() {}\n}\n";

        private const string PlainJobSource =
            "using Unity.Burst;\nusing Unity.Jobs;\n\n"
            + "[BurstCompile]\npublic struct PlainJob : IJob {\n    public void Execute() {}\n}\n";

        private const string HiddenJobSource =
            "using Unity.Burst;\nusing Unity.Jobs;\n\n"
            + "[BurstCompile(FloatMode = FloatMode.Deterministic)]\npublic struct HiddenJob : IJob {\n"
            + "    public void Execute() {}\n}\n";

        private string _sourceRoot;

        [SetUp]
        public void CreateFixtureSources() {
            _sourceRoot = Path.Combine(Path.GetTempPath(), "SpellboundFloatModeScan" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_sourceRoot);
            File.WriteAllText(Path.Combine(_sourceRoot, "ListedJob.cs"), ListedJobSource);
            File.WriteAllText(Path.Combine(_sourceRoot, "PlainJob.cs"), PlainJobSource);
            var hidden = Path.Combine(_sourceRoot, "Hidden~");
            Directory.CreateDirectory(hidden);
            File.WriteAllText(Path.Combine(hidden, "HiddenJob.cs"), HiddenJobSource);
        }

        [TearDown]
        public void DeleteFixtureSources() {
            if (Directory.Exists(_sourceRoot)) {
                Directory.Delete(_sourceRoot, true);
            }
        }

        [Test]
        public void DeterministicJobReportsNothing() {
            var report = DeterministicFloatModeScan.Check(new[] { "ListedJob" }, _sourceRoot);
            Assert.IsTrue(report.IsEmpty, report.ToText());
            Assert.AreEqual(_sourceRoot, report.Subject);
        }

        [Test]
        public void JobWithoutDeterministicFloatModeIsReported() {
            var report = DeterministicFloatModeScan.Check(new[] { "PlainJob" }, _sourceRoot);
            Assert.AreEqual(1, report.Findings.Count, report.ToText());
            Assert.AreEqual("core.float-mode-missing", report.Findings[0].Code);
            Assert.AreEqual(Path.Combine(_sourceRoot, "PlainJob.cs"), report.Findings[0].Location);
            Assert.IsTrue(report.HasErrors);
        }

        [Test]
        public void JobNoSourceDeclaresIsReported() {
            var report = DeterministicFloatModeScan.Check(new[] { "MissingJob" }, _sourceRoot);
            Assert.AreEqual(1, report.Findings.Count, report.ToText());
            Assert.AreEqual("core.float-mode-job-missing", report.Findings[0].Code);
            Assert.IsTrue(report.HasErrors);
        }

        [Test]
        public void FolderEndingInTildeIsSkipped() {
            var report = DeterministicFloatModeScan.Check(new[] { "HiddenJob" }, _sourceRoot);
            Assert.AreEqual(1, report.Findings.Count, report.ToText());
            Assert.AreEqual("core.float-mode-job-missing", report.Findings[0].Code);
        }

        [Test]
        public void EveryListedJobIsChecked() {
            var report = DeterministicFloatModeScan.Check(new[] { "ListedJob", "PlainJob", "MissingJob" },
                _sourceRoot);
            Assert.AreEqual(2, report.Findings.Count, report.ToText());
        }
    }
}
