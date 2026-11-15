// Copyright (c) Spellbound Studios. All rights reserved.

using System.Reflection;
using NUnit.Framework;
using Spellbound.Core.Authoring;
using Spellbound.Core.Authoring.Editor;
using Spellbound.Core.Observations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Spellbound.Core.Tests {
    public class GuardPolicyCheckTests {
        private GuardPolicy _policy;
        private GuardPolicyCheck _check;

        [SetUp]
        public void CreatePolicy() {
            _policy = ScriptableObject.CreateInstance<GuardPolicy>();
            _check = new GuardPolicyCheck();
        }

        [TearDown]
        public void DestroyPolicy() {
            Object.DestroyImmediate(_policy);
        }

        private ValidationReport Validate() {
            var report = new ValidationReport("policy");
            _policy.Validate(report);
            return report;
        }

        private ValidationReport Checked() {
            var report = new ValidationReport("policy");
            _check.Check(_policy, report);
            return report;
        }

        [Test]
        public void ThePolicyIsAnAuthoredDefinition() {
            Assert.IsTrue(typeof(IAuthoredDefinition).IsAssignableFrom(typeof(GuardPolicy)));
        }

        [Test]
        public void EveryFindingCodeIsACode() {
            Assert.AreEqual("core.guard-repairs-under-one", GuardPolicy.RepairsUnderOneCode);
            Assert.AreEqual("core.guard-window-invalid", GuardPolicy.WindowInvalidCode);
            Assert.AreEqual("core.guard-hold-invalid", GuardPolicy.HoldInvalidCode);
            Assert.AreEqual("core.guard-policy", GuardPolicyCheck.Code);

            foreach (var code in new[] {
                         GuardPolicy.RepairsUnderOneCode, GuardPolicy.WindowInvalidCode, GuardPolicy.HoldInvalidCode,
                         GuardPolicyCheck.Code
                     }) {
                Assert.IsTrue(ValidationFinding.IsCode(code), code);
            }
        }

        [Test]
        public void ACleanPolicyReportsNothing() {
            Assert.IsTrue(Validate().IsEmpty, Validate().ToText());
            Assert.IsTrue(Checked().IsEmpty, Checked().ToText());
        }

        [Test]
        public void RepairsUnderOneAreRefused() {
            _policy.repairs = 0;
            var report = Validate();
            Assert.AreEqual(1, report.Findings.Count, report.ToText());
            Assert.AreEqual(GuardPolicy.RepairsUnderOneCode, report.Findings[0].Code);
            Assert.AreEqual(FindingSeverity.Error, report.Findings[0].Severity);
            Assert.AreEqual(nameof(GuardPolicy.repairs), report.Findings[0].Location);
        }

        [Test]
        public void ANegativeOrNonFiniteWindowIsRefused() {
            foreach (var seconds in new[] { -0.5, double.NaN, double.PositiveInfinity }) {
                _policy.windowSeconds = seconds;
                var report = Validate();
                Assert.AreEqual(1, report.Findings.Count, report.ToText());
                Assert.AreEqual(GuardPolicy.WindowInvalidCode, report.Findings[0].Code);
                Assert.AreEqual(FindingSeverity.Error, report.Findings[0].Severity);
                Assert.AreEqual(nameof(GuardPolicy.windowSeconds), report.Findings[0].Location);
            }
        }

        [Test]
        public void ANegativeOrNonFiniteHoldIsRefused() {
            foreach (var seconds in new[] { -1, double.NaN, double.NegativeInfinity }) {
                _policy.holdSeconds = seconds;
                var report = Validate();
                Assert.AreEqual(1, report.Findings.Count, report.ToText());
                Assert.AreEqual(GuardPolicy.HoldInvalidCode, report.Findings[0].Code);
                Assert.AreEqual(FindingSeverity.Error, report.Findings[0].Severity);
                Assert.AreEqual(nameof(GuardPolicy.holdSeconds), report.Findings[0].Location);
            }
        }

        [Test]
        public void AWindowShorterThanOneTickAtTwentyASecondIsAWarning() {
            _policy.windowSeconds = 0.04;
            var report = Checked();
            Assert.AreEqual(1, report.Findings.Count, report.ToText());
            Assert.AreEqual(GuardPolicyCheck.Code, report.Findings[0].Code);
            Assert.AreEqual(FindingSeverity.Warning, report.Findings[0].Severity);
            Assert.AreEqual(nameof(GuardPolicy.windowSeconds), report.Findings[0].Location);

            _policy.windowSeconds = 0.05;
            Assert.IsTrue(Checked().IsEmpty, Checked().ToText());
        }

        [Test]
        public void TheCheckLeavesThePolicysOwnFindingsToItsValidate() {
            _policy.windowSeconds = -1;
            Assert.IsTrue(Checked().IsEmpty, Checked().ToText());
        }

        [Test]
        public void ThePreviewPrintsTheThreeNumbers() {
            _policy.repairs = 4;
            _policy.windowSeconds = 1.5;
            _policy.holdSeconds = 6;
            var lines = _check.PreviewLines(_policy);
            Assert.AreEqual(1, lines.Count);
            Assert.AreEqual("4 repairs in a window of 1.5 s, held 6 s", lines[0]);
        }

        [Test]
        public void TheRegistryFindsTheCheckOverThePolicy() {
            var check = DefinitionCheckRegistry.Default.Find(GuardPolicyCheck.Code);
            Assert.IsNotNull(check);
            Assert.AreEqual(typeof(GuardPolicy), check.DefinitionType);
            Assert.AreEqual(typeof(GuardPolicyCheck), check.CheckType);
            Assert.AreEqual("Core", check.Package);
            Assert.AreEqual(GuardPolicyCheck.Code,
                typeof(GuardPolicyCheck).GetCustomAttribute<RegisteredDefinitionCheckAttribute>().Name);
        }

        [Test]
        public void TheRegisteredCheckReportsAShortWindow() {
            _policy.windowSeconds = 0.01;
            var report = new ValidationReport("policy");
            DefinitionCheckRegistry.Default.Find(GuardPolicyCheck.Code).Run(_policy, report);
            Assert.AreEqual(1, report.Findings.Count, report.ToText());
            Assert.AreEqual(GuardPolicyCheck.Code, report.Findings[0].Code);
        }
    }
}
