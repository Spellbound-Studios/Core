// Copyright (c) Spellbound Studios. All rights reserved.

using NUnit.Framework;
using Spellbound.Core.Authoring;
using Spellbound.Core.Observations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Spellbound.Core.Tests {
    public class ObservationSettingsValidationTests {
        private ObservationSettings _settings;
        private GuardPolicy _policy;

        [SetUp]
        public void CreateSettings() {
            _settings = ScriptableObject.CreateInstance<ObservationSettings>();
            _policy = ScriptableObject.CreateInstance<GuardPolicy>();
        }

        [TearDown]
        public void DestroySettings() {
            Object.DestroyImmediate(_settings);
            Object.DestroyImmediate(_policy);
        }

        private ValidationReport Validate() {
            var report = new ValidationReport("settings");
            _settings.Validate(report);
            return report;
        }

        [Test]
        public void TheSettingsAssetIsAnAuthoredDefinition() {
            Assert.IsTrue(typeof(IAuthoredDefinition).IsAssignableFrom(typeof(ObservationSettings)));
        }

        [Test]
        public void EveryFindingCodeIsACode() {
            Assert.IsTrue(ValidationFinding.IsCode(ObservationSettings.KeyUnparsedCode));
            Assert.IsTrue(ValidationFinding.IsCode(ObservationSettings.RecorderCellsUnderOneCode));
            Assert.IsTrue(ValidationFinding.IsCode(ObservationSettings.RateCapNegativeCode));
            Assert.AreEqual("core.observation-key-unparsed", ObservationSettings.KeyUnparsedCode);
            Assert.AreEqual("core.recorder-cells-under-one", ObservationSettings.RecorderCellsUnderOneCode);
            Assert.AreEqual("core.rate-cap-negative", ObservationSettings.RateCapNegativeCode);
        }

        [Test]
        public void ACleanAssetReportsNothing() {
            _settings.budgets.Add(new ObservationBudgetOverride { key = "core.clock.held", budget = 2 });
            _settings.qualityLevels.Add(new ObservationQualityLevelOverride { key = "core.intent.stale", level = 1 });
            _settings.rateCaps.Add(new ObservationRateCap { key = "core.intent.repeats", recordsPerSecond = 0 });
            _settings.guardPolicies.Add(new ObservationGuardPolicyOverride {
                key = "core.reference_frame.up_varies", policy = _policy
            });

            var report = Validate();
            Assert.IsTrue(report.IsEmpty, report.ToText());
        }

        [Test]
        public void AKeyThatDoesNotParseIsReportedInEachList() {
            _settings.budgets.Add(new ObservationBudgetOverride { key = "Core.Clock" });
            _settings.qualityLevels.Add(new ObservationQualityLevelOverride { key = "nodots" });
            _settings.rateCaps.Add(new ObservationRateCap { key = "core..held", recordsPerSecond = 1 });
            _settings.guardPolicies.Add(new ObservationGuardPolicyOverride { key = string.Empty, policy = _policy });

            var report = Validate();
            Assert.AreEqual(4, report.Findings.Count, report.ToText());
            var lists = new[] {
                nameof(ObservationSettings.budgets), nameof(ObservationSettings.qualityLevels),
                nameof(ObservationSettings.rateCaps), nameof(ObservationSettings.guardPolicies)
            };
            var keys = new[] { "Core.Clock", "nodots", "core..held", string.Empty };

            for (var index = 0; index < report.Findings.Count; index++) {
                var finding = report.Findings[index];
                Assert.AreEqual(ObservationSettings.KeyUnparsedCode, finding.Code);
                Assert.AreEqual(FindingSeverity.Error, finding.Severity);
                Assert.AreEqual(lists[index], finding.Location);
                StringAssert.Contains("'" + keys[index] + "'", finding.Hint);
            }
        }

        [Test]
        public void RecorderCellsUnderOneAreRefused() {
            _settings.recorderCells = 0;
            var report = Validate();
            Assert.AreEqual(1, report.Findings.Count, report.ToText());
            Assert.AreEqual(ObservationSettings.RecorderCellsUnderOneCode, report.Findings[0].Code);
            Assert.AreEqual(FindingSeverity.Error, report.Findings[0].Severity);
            Assert.AreEqual(nameof(ObservationSettings.recorderCells), report.Findings[0].Location);
        }

        [Test]
        public void ANegativeOrNonFiniteRateCapIsRefused() {
            foreach (var rate in new[] { -1, double.NaN, double.PositiveInfinity, double.NegativeInfinity }) {
                _settings.rateCaps.Clear();
                _settings.rateCaps.Add(new ObservationRateCap { key = "core.intent.repeats", recordsPerSecond = rate });
                var report = Validate();
                Assert.AreEqual(1, report.Findings.Count, report.ToText());
                Assert.AreEqual(ObservationSettings.RateCapNegativeCode, report.Findings[0].Code);
                Assert.AreEqual(FindingSeverity.Error, report.Findings[0].Severity);
                Assert.AreEqual(nameof(ObservationSettings.rateCaps), report.Findings[0].Location);
            }
        }
    }
}
