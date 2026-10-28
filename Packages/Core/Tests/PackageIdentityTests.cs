// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Spellbound.Core.Authoring;
using Spellbound.Core.Packages.Editor;
using UnityEditor;
using UnityEngine;

namespace Spellbound.Core.Tests {
    public class PackageIdentityTests {
        private const string CoreById = "Packages/com.spellboundstudios.core";
        private const string CoreByFolder = "Packages/Core";

        private readonly List<PackageIdentity> _created = new List<PackageIdentity>();

        [TearDown]
        public void TearDown() {
            foreach (var identity in _created) {
                UnityEngine.Object.DestroyImmediate(identity);
            }

            _created.Clear();
        }

        [Test]
        public void ACleanIdentityHasNoFinding() {
            Assert.IsTrue(Validated(Clean()).IsEmpty);
        }

        [Test]
        public void ACleanIdentityMayLeaveItsListsEmptyAndBeRetired() {
            var identity = Clean();
            identity.mustNotOwn = Array.Empty<string>();
            identity.simulation = null;
            identity.machineMatchedJobs = Array.Empty<string>();
            identity.status = PackageIdentity.RetiredStatus;
            identity.skeletonVersion = 0;
            Assert.IsTrue(Validated(identity).IsEmpty);
        }

        [Test]
        public void AFreshIdentityNamesEveryMissingField() {
            var identity = ScriptableObject.CreateInstance<PackageIdentity>();
            _created.Add(identity);
            CollectionAssert.AreEqual(
                new[] {
                    PackageIdentity.JobEmptyCode, PackageIdentity.KindEmptyCode, PackageIdentity.LayerEmptyCode,
                    PackageIdentity.FrontDoorEmptyCode
                },
                Validated(identity).Findings.Select(finding => finding.Code).ToArray());
        }

        [Test]
        public void AnEmptyJobIsAnError() {
            var identity = Clean();
            identity.job = "  ";
            AssertOnly(identity, PackageIdentity.JobEmptyCode, FindingSeverity.Error, nameof(PackageIdentity.job));
        }

        [TestCase("Gives a game\none plain thing.")]
        [TestCase("Gives a game\r\none plain thing.")]
        public void AJobWithALineBreakIsAnError(string job) {
            var identity = Clean();
            identity.job = job;
            AssertOnly(identity, PackageIdentity.JobLineBreakCode, FindingSeverity.Error,
                nameof(PackageIdentity.job));
        }

        [Test]
        public void AnEmptyKindIsAnError() {
            var identity = Clean();
            identity.kind = string.Empty;
            AssertOnly(identity, PackageIdentity.KindEmptyCode, FindingSeverity.Error, nameof(PackageIdentity.kind));
        }

        [Test]
        public void AnEmptyLayerIsAnError() {
            var identity = Clean();
            identity.layer = null;
            AssertOnly(identity, PackageIdentity.LayerEmptyCode, FindingSeverity.Error,
                nameof(PackageIdentity.layer));
        }

        [TestCase("Library")]
        [TestCase("two words")]
        [TestCase("tool-")]
        public void AKindThatIsNotAnIdentifierIsAnError(string kind) {
            var identity = Clean();
            identity.kind = kind;
            AssertOnly(identity, PackageIdentity.IdentifierFormCode, FindingSeverity.Error,
                nameof(PackageIdentity.kind));
        }

        [Test]
        public void ALayerThatIsNotAnIdentifierIsAnError() {
            var identity = Clean();
            identity.layer = "mechanism_layer";
            AssertOnly(identity, PackageIdentity.IdentifierFormCode, FindingSeverity.Error,
                nameof(PackageIdentity.layer));
        }

        [TestCase("paused")]
        [TestCase("Active")]
        [TestCase("")]
        public void AStatusOtherThanActiveOrRetiredIsAnError(string status) {
            var identity = Clean();
            identity.status = status;
            AssertOnly(identity, PackageIdentity.StatusUnknownCode, FindingSeverity.Error,
                nameof(PackageIdentity.status));
        }

        [Test]
        public void AnEmptyFrontDoorIsAnError() {
            var identity = Clean();
            identity.frontDoor = " ";
            AssertOnly(identity, PackageIdentity.FrontDoorEmptyCode, FindingSeverity.Error,
                nameof(PackageIdentity.frontDoor));
        }

        [Test]
        public void AnEmptyEntryIsAnErrorInEveryList() {
            var identity = Clean();
            identity.mustNotOwn = new[] { "anything drawn", " " };
            AssertOnly(identity, PackageIdentity.EntryEmptyCode, FindingSeverity.Error, "mustNotOwn[1]");
            identity = Clean();
            identity.simulation = new[] { string.Empty };
            AssertOnly(identity, PackageIdentity.EntryEmptyCode, FindingSeverity.Error, "simulation[0]");
            identity = Clean();
            identity.machineMatchedJobs = new[] { "pack bytes", null };
            AssertOnly(identity, PackageIdentity.EntryEmptyCode, FindingSeverity.Error, "machineMatchedJobs[1]");
        }

        [Test]
        public void ARepeatedEntryIsAWarning() {
            var identity = Clean();
            identity.mustNotOwn = new[] { "connections", " connections " };
            AssertOnly(identity, PackageIdentity.EntryRepeatedCode, FindingSeverity.Warning, "mustNotOwn[1]");
        }

        [Test]
        public void ANegativeSkeletonVersionIsAnError() {
            var identity = Clean();
            identity.skeletonVersion = -1;
            AssertOnly(identity, PackageIdentity.SkeletonNegativeCode, FindingSeverity.Error,
                nameof(PackageIdentity.skeletonVersion));
        }

        [Test]
        public void ValidateNeedsAReport() {
            var identity = Clean();
            Assert.Throws<ArgumentNullException>(() => identity.Validate(null));
        }

        [TestCase("foundation", true)]
        [TestCase("a", true)]
        [TestCase("tool2", true)]
        [TestCase("two-words", true)]
        [TestCase(null, false)]
        [TestCase("", false)]
        [TestCase("Foundation", false)]
        [TestCase("2d", false)]
        [TestCase("-tool", false)]
        [TestCase("tool-", false)]
        [TestCase("two--words", false)]
        [TestCase("two_words", false)]
        [TestCase("two words", false)]
        public void AnIdentifierIsLowerCaseLettersDigitsAndSingleHyphens(string text, bool expected) {
            Assert.AreEqual(expected, PackageIdentity.IsIdentifier(text));
        }

        [Test]
        public void EveryJudgmentFieldIsRecognised() {
            CollectionAssert.AreEqual(
                new[] {
                    nameof(PackageIdentity.kind), nameof(PackageIdentity.layer), nameof(PackageIdentity.status),
                    nameof(PackageIdentity.job), nameof(PackageIdentity.frontDoor), nameof(PackageIdentity.mustNotOwn),
                    nameof(PackageIdentity.hotPath), nameof(PackageIdentity.deterministicOutput),
                    nameof(PackageIdentity.simulation), nameof(PackageIdentity.machineMatchedJobs)
                },
                PackageIdentity.JudgmentFields);

            foreach (var field in PackageIdentity.JudgmentFields) {
                Assert.IsTrue(PackageIdentity.IsJudgmentField(field), field);
            }
        }

        [TestCase("skeletonVersion")]
        [TestCase("Kind")]
        [TestCase("")]
        [TestCase(null)]
        public void OtherNamesAreNotJudgmentFields(string fieldName) {
            Assert.IsFalse(PackageIdentity.IsJudgmentField(fieldName));
        }

        [Test]
        public void FindReadsCoresOwnIdentityByItsIdAndByItsFolder() {
            var byId = PackageIdentity.Find(CoreById);
            Assert.IsNotNull(byId);
            Assert.AreEqual(CoreById + "/" + PackageIdentity.FileName, AssetDatabase.GetAssetPath(byId));
            Assert.AreSame(byId, PackageIdentity.Find(CoreByFolder));
            Assert.AreSame(byId, PackageIdentity.Find(CoreByFolder + "/"));
        }

        [Test]
        public void CoresOwnIdentityValidatesClean() {
            var report = Validated(PackageIdentity.Find(CoreById));
            Assert.IsTrue(report.IsEmpty, report.ToText());
        }

        [Test]
        public void FindReturnsNullWithoutAFolderOrAnIdentity() {
            Assert.IsNull(PackageIdentity.Find(null));
            Assert.IsNull(PackageIdentity.Find(" "));
            Assert.IsNull(PackageIdentity.Find("Assets/NoSuchFolder"));
        }

        [Test]
        public void PathForAppendsTheFileName() {
            Assert.AreEqual("Packages/com.fixture/Identity.asset", PackageIdentity.PathFor("Packages/com.fixture/"));
            Assert.AreEqual(PackageIdentity.FileName, PackageIdentity.PathFor(string.Empty));
        }

        private PackageIdentity Clean() {
            var identity = ScriptableObject.CreateInstance<PackageIdentity>();
            _created.Add(identity);
            identity.kind = "library";
            identity.layer = "mechanism";
            identity.status = PackageIdentity.ActiveStatus;
            identity.job = "Gives a game one plain thing.";
            identity.frontDoor = "FixtureKit";
            identity.mustNotOwn = new[] { "anything drawn", "connections" };
            identity.simulation = new[] { "fixed steps" };
            identity.machineMatchedJobs = new[] { "pack bytes" };
            identity.skeletonVersion = 1;
            return identity;
        }

        private static ValidationReport Validated(PackageIdentity identity) {
            Assert.IsNotNull(identity);
            var report = new ValidationReport("Packages/com.fixture/Identity.asset");
            identity.Validate(report);
            return report;
        }

        private static void AssertOnly(PackageIdentity identity, string code, FindingSeverity severity,
            string location) {
            var findings = Validated(identity).Findings;
            Assert.AreEqual(1, findings.Count, string.Join("\n", findings.Select(finding => finding.ToText())));
            Assert.AreEqual(code, findings[0].Code);
            Assert.AreEqual(severity, findings[0].Severity);
            Assert.AreEqual(location, findings[0].Location);
        }
    }
}
