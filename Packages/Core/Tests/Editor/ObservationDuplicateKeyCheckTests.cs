// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Spellbound.Core.Authoring;
using Spellbound.Core.Authoring.Editor;
using Spellbound.Core.Observations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Spellbound.Core.Tests {
    public class ObservationDuplicateKeyCheckTests {
        private const string FirstCollidingKey = "alpha.wtbqkysv";
        private const string SecondCollidingKey = "beta.artgjrkn";

        private ObservationSettings _settings;
        private GuardPolicy _policy;
        private List<ObservationDeclaration> _declared;
        private ObservationDuplicateKeyCheck _check;

        [SetUp]
        public void CreateSettings() {
            _settings = ScriptableObject.CreateInstance<ObservationSettings>();
            _policy = ScriptableObject.CreateInstance<GuardPolicy>();
            _declared = new List<ObservationDeclaration> {
                Declaration("test.cost.frame", ObservationKind.Cost, false),
                Declaration("test.occurrence.landed", ObservationKind.Occurrence, false),
                Declaration("test.guard.up", ObservationKind.Counter, true)
            };
            _check = new ObservationDuplicateKeyCheck(() => _declared);
        }

        [TearDown]
        public void DestroySettings() {
            Object.DestroyImmediate(_settings);
            Object.DestroyImmediate(_policy);
        }

        private static ObservationDeclaration Declaration(string key, ObservationKind kind, bool isGuard) {
            return new ObservationDeclaration(ObservationKey.Parse(key), kind, "A declaration for the check.",
                ObservationUnit.Count, double.NaN, 0, isGuard);
        }

        private ValidationReport Checked() {
            var report = new ValidationReport("settings");
            _check.Check(_settings, report);
            return report;
        }

        private void NameEveryDeclaredKeyOnce() {
            _settings.budgets.Add(new ObservationBudgetOverride { key = "test.cost.frame", budget = 2 });
            _settings.rateCaps.Add(new ObservationRateCap { key = "test.occurrence.landed", recordsPerSecond = 5 });
            _settings.guardPolicies.Add(new ObservationGuardPolicyOverride { key = "test.guard.up", policy = _policy });
        }

        [Test]
        public void EveryFindingCodeIsACode() {
            Assert.AreEqual("core.observation-duplicate-key", ObservationDuplicateKeyCheck.DuplicateKeyCode);
            Assert.AreEqual("core.observation-key-undeclared", ObservationDuplicateKeyCheck.KeyUndeclaredCode);
            Assert.AreEqual("core.observation-declared-twice", ObservationDuplicateKeyCheck.DeclaredTwiceCode);

            foreach (var code in new[] {
                         ObservationDuplicateKeyCheck.DuplicateKeyCode, ObservationDuplicateKeyCheck.KeyUndeclaredCode,
                         ObservationDuplicateKeyCheck.DeclaredTwiceCode
                     }) {
                Assert.IsTrue(ValidationFinding.IsCode(code), code);
            }
        }

        [Test]
        public void ACleanAssetReportsNothing() {
            NameEveryDeclaredKeyOnce();
            Assert.IsTrue(Checked().IsEmpty, Checked().ToText());
        }

        [Test]
        public void ACostKeyWithABudgetAndAQualityLevelReportsNothing() {
            NameEveryDeclaredKeyOnce();
            _settings.qualityLevels.Add(new ObservationQualityLevelOverride { key = "test.cost.frame", level = 2 });
            Assert.IsTrue(Checked().IsEmpty, Checked().ToText());
        }

        [Test]
        public void AKeyNamedTwiceAcrossTheListsIsAnError() {
            NameEveryDeclaredKeyOnce();
            _settings.qualityLevels.Add(new ObservationQualityLevelOverride { key = "test.guard.up", level = 1 });
            var report = Checked();
            Assert.AreEqual(1, report.Findings.Count, report.ToText());
            Assert.AreEqual(ObservationDuplicateKeyCheck.DuplicateKeyCode, report.Findings[0].Code);
            Assert.AreEqual(FindingSeverity.Error, report.Findings[0].Severity);
            Assert.AreEqual(nameof(ObservationSettings.guardPolicies), report.Findings[0].Location);
            StringAssert.Contains("'test.guard.up'", report.Findings[0].Message);
        }

        [Test]
        public void AKeyNamedTwiceInOneListIsAnError() {
            NameEveryDeclaredKeyOnce();
            _settings.budgets.Add(new ObservationBudgetOverride { key = "test.cost.frame", budget = 3 });
            var report = Checked();
            Assert.AreEqual(1, report.Findings.Count, report.ToText());
            Assert.AreEqual(ObservationDuplicateKeyCheck.DuplicateKeyCode, report.Findings[0].Code);
            Assert.AreEqual(nameof(ObservationSettings.budgets), report.Findings[0].Location);
        }

        [Test]
        public void AKeyNoPackageDeclaresIsAWarning() {
            _settings.rateCaps.Add(new ObservationRateCap { key = "test.occurrence.missing", recordsPerSecond = 1 });
            var report = Checked();
            Assert.AreEqual(1, report.Findings.Count, report.ToText());
            Assert.AreEqual(ObservationDuplicateKeyCheck.KeyUndeclaredCode, report.Findings[0].Code);
            Assert.AreEqual(FindingSeverity.Warning, report.Findings[0].Severity);
            Assert.AreEqual(nameof(ObservationSettings.rateCaps), report.Findings[0].Location);
            StringAssert.Contains("'test.occurrence.missing'", report.Findings[0].Message);
        }

        [Test]
        public void AKeyThatDoesNotParseIsLeftToTheAssetsValidate() {
            _settings.budgets.Add(new ObservationBudgetOverride { key = "Not A Key" });
            _settings.qualityLevels.Add(new ObservationQualityLevelOverride { key = "Not A Key" });
            Assert.IsTrue(Checked().IsEmpty, Checked().ToText());
        }

        [Test]
        public void TwoPackagesDeclaringOneKeyHashIsAnErrorOnTheAsset() {
            Assert.AreEqual(ObservationKey.Parse(FirstCollidingKey).Hash,
                ObservationKey.Parse(SecondCollidingKey).Hash);
            _declared.Add(Declaration(FirstCollidingKey, ObservationKind.Occurrence, false));
            _declared.Add(Declaration(SecondCollidingKey, ObservationKind.Counter, false));
            NameEveryDeclaredKeyOnce();
            var report = Checked();
            Assert.AreEqual(1, report.Findings.Count, report.ToText());
            Assert.AreEqual(ObservationDuplicateKeyCheck.DeclaredTwiceCode, report.Findings[0].Code);
            Assert.AreEqual(FindingSeverity.Error, report.Findings[0].Severity);
            StringAssert.Contains("'alpha'", report.Findings[0].Message);
            StringAssert.Contains("'beta'", report.Findings[0].Message);
        }

        [Test]
        public void ThePreviewListsTheKeysWithTheirDeclaredKind() {
            NameEveryDeclaredKeyOnce();
            _settings.qualityLevels.Add(new ObservationQualityLevelOverride { key = "test.cost.frame", level = 2 });
            _settings.rateCaps.Add(new ObservationRateCap { key = "test.occurrence.missing", recordsPerSecond = 1 });
            var lines = _check.PreviewLines(_settings);
            CollectionAssert.AreEqual(new[] {
                "test.cost.frame: cost",
                "test.occurrence.landed: occurrence",
                "test.occurrence.missing: undeclared",
                "test.guard.up: guard"
            }, lines);
        }

        [Test]
        public void TheRegistryFindsTheCheckOverTheSettings() {
            var check = DefinitionCheckRegistry.Default.Find(ObservationDuplicateKeyCheck.DuplicateKeyCode);
            Assert.IsNotNull(check);
            Assert.AreEqual(typeof(ObservationSettings), check.DefinitionType);
            Assert.AreEqual(typeof(ObservationDuplicateKeyCheck), check.CheckType);
            Assert.AreEqual(ObservationDuplicateKeyCheck.DuplicateKeyCode,
                typeof(ObservationDuplicateKeyCheck).GetCustomAttribute<RegisteredDefinitionCheckAttribute>().Name);
        }

        [Test]
        public void TheChecksForTheSettingsIncludeTheDuplicateKeyCheck() {
            var names = DefinitionCheckRegistry.Default.For(typeof(ObservationSettings)).Select(check => check.Name);
            CollectionAssert.Contains(names.ToList(), ObservationDuplicateKeyCheck.DuplicateKeyCode);
        }

        [Test]
        public void TheRegisteredCheckReadsTheDeclaredKeys() {
            _settings.budgets.Add(new ObservationBudgetOverride { key = "core.clock.held", budget = 1 });
            _settings.rateCaps.Add(new ObservationRateCap { key = "test.never_declared.key", recordsPerSecond = 1 });
            var report = new ValidationReport("settings");
            DefinitionCheckRegistry.Default.Find(ObservationDuplicateKeyCheck.DuplicateKeyCode).Run(_settings, report);
            Assert.AreEqual(1, report.Findings.Count, report.ToText());
            Assert.AreEqual(ObservationDuplicateKeyCheck.KeyUndeclaredCode, report.Findings[0].Code);
        }
    }
}
