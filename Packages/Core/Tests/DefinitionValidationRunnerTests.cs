// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using NUnit.Framework;
using Spellbound.Core.Authoring;
using Spellbound.Core.Authoring.Editor;
using Spellbound.Core.Tests.AuthoringFixture;
using UnityEditor;
using UnityEngine;

namespace Spellbound.Core.Tests {
    public class DefinitionValidationRunnerTests {
        public const string PackagedFixturePath = "Packages/com.spellboundstudios.core/Tests/AuthoringFixture/Assets/PackagedFixture.asset";

        private AuthoringTestFolder _folder;
        private DefinitionValidationRunner _runner;

        [SetUp]
        public void SetUp() {
            _folder = new AuthoringTestFolder();
            _runner = new DefinitionValidationRunner();
            DefinitionReportCache.Clear();
        }

        [TearDown]
        public void TearDown() {
            _folder.Dispose();
        }

        [Test]
        public void OneAssetRunsItsOwnValidationThenTheRegisteredChecksInNameOrder() {
            var asset = _folder.Create("Loud", string.Empty, 250);
            var run = _runner.ValidateAsset(asset);
            Assert.AreEqual(AssetDatabase.GetAssetPath(asset), run.Subject);
            Assert.AreEqual(new[] { "FixtureDefinition (Core)" }, run.OwnValidations);
            Assert.AreEqual(new[] { "fixturecheck.threshold-high (Core)" }, run.RegisteredChecks);
            Assert.AreEqual("checks: FixtureDefinition (Core), fixturecheck.threshold-high (Core)", run.ChecksLine());
            Assert.AreEqual(2, run.Report.Count(FindingSeverity.Error));
            Assert.AreEqual(FixtureDefinition.TitleEmptyCode, run.Report.Findings[0].Code);
            Assert.AreEqual("fixturecheck.threshold-high", run.Report.Findings[1].Code);
            Assert.AreEqual(run.Subject, run.Report.Findings[1].Subject);
            Assert.AreEqual(2, run.Timings.Count);
            Assert.IsTrue(run.HasErrors);
        }

        [Test]
        public void ARegisteredCheckStampsItsLabelOnItsFindingsAndTheOwnValidationStampsNothing() {
            var asset = _folder.Create("Stamped", string.Empty, 250);
            var run = _runner.ValidateAsset(asset);
            Assert.AreEqual(FixtureDefinition.TitleEmptyCode, run.Report.Findings[0].Code);
            Assert.IsNull(run.Report.Findings[0].Check);
            Assert.AreEqual("fixturecheck.threshold-high", run.Report.Findings[1].Code);
            Assert.AreEqual("fixturecheck.threshold-high (Core)", run.Report.Findings[1].Check);
        }

        [Test]
        public void TheTextIsTheSummaryTheChecksLineThenTheFindings() {
            var asset = _folder.Create("Fine", "fine", 5);
            var text = _runner.ValidateAsset(asset).ToText();
            Assert.AreEqual("no findings in 1 asset\nchecks: FixtureDefinition (Core), fixturecheck.threshold-high (Core)", text);
            var withWarning = _folder.Create("Low", "low", -1);
            var lines = _runner.ValidateAsset(withWarning).ToText().Split('\n');
            Assert.AreEqual("1 warning in 1 asset", lines[0]);
            StringAssert.StartsWith("checks: ", lines[1]);
            StringAssert.StartsWith(FixtureDefinition.ThresholdNegativeCode + " warning " + AssetDatabase.GetAssetPath(withWarning) + " at threshold: ", lines[2]);
        }

        [Test]
        public void AValidatorThatThrowsBecomesAnErrorFindingAndTheChecksStillRun() {
            var asset = _folder.Create("Broken", "broken", 500);
            asset.throwOnValidate = true;
            var run = _runner.ValidateAsset(asset);
            Assert.AreEqual(DefinitionValidationRunner.ValidationThrewCode, run.Report.Findings[0].Code);
            StringAssert.Contains("InvalidOperationException", run.Report.Findings[0].Message);
            Assert.AreEqual("fixturecheck.threshold-high", run.Report.Findings[1].Code);
        }

        [Test]
        public void AFolderMergesItsAssetsAndListsEachCheckOnce() {
            _folder.Create("A", "a", 1);
            _folder.Create("B", string.Empty, 1);
            var sub = _folder.Subfolder("Deeper");
            _folder.Create("C", "c", 1000, sub);
            var run = _runner.Validate(_folder.Path);
            Assert.AreEqual(3, run.Assets.Count);
            Assert.AreEqual("checks: FixtureDefinition (Core), fixturecheck.threshold-high (Core)", run.ChecksLine());
            Assert.AreEqual("2 errors in 3 assets", run.Summary());
            Assert.AreEqual(6, run.Timings.Count);
        }

        [Test]
        public void AnEmptyFolderIsAnEmptyRun() {
            var run = _runner.Validate(_folder.Path);
            Assert.AreEqual(0, run.Assets.Count);
            Assert.IsFalse(run.HasErrors);
            Assert.AreEqual("no authored definitions under " + _folder.Path, run.Summary());
            Assert.AreEqual("checks: none", run.ChecksLine());
        }

        [Test]
        public void EverythingIncludesTheTemporaryAssetAndThePackagedOne() {
            var asset = _folder.Create("Everywhere", "everywhere", 1);
            var run = _runner.ValidateAll();
            Assert.AreEqual(DefinitionValidationRunner.EverythingSubject, run.Subject);
            CollectionAssert.Contains(run.Assets, AssetDatabase.GetAssetPath(asset));
            CollectionAssert.Contains(run.Assets, PackagedFixturePath);
        }

        [Test]
        public void APackageFolderIsASubjectByIdOrByShortName() {
            var byIdentifier = _runner.Validate("Packages/com.spellboundstudios.core");
            CollectionAssert.Contains(byIdentifier.Assets, PackagedFixturePath);
            var byShortName = _runner.Validate("Packages/Core");
            CollectionAssert.Contains(byShortName.Assets, PackagedFixturePath);
            Assert.AreEqual("Packages/com.spellboundstudios.core/Tests", DefinitionAssets.ResolveFolder("Packages/Core/Tests"));
            Assert.IsNull(DefinitionAssets.ResolveFolder("Packages/Nowhere"));
        }

        [Test]
        public void ThePackagedFixtureValidatesClean() {
            var run = _runner.Validate(PackagedFixturePath);
            Assert.AreEqual(1, run.Assets.Count);
            Assert.IsFalse(run.HasErrors);
        }

        [Test]
        public void APathOutsideAssetsAndPackagesOrNamingNothingIsRefused() {
            Assert.Throws<ArgumentException>(() => _runner.Validate("../outside"));
            Assert.Throws<ArgumentException>(() => _runner.Validate("C:/anywhere"));
            Assert.Throws<ArgumentException>(() => _runner.Validate("Assets/Nowhere/Nothing.asset"));
            Assert.Throws<ArgumentException>(() => _runner.Validate("Packages/Nowhere"));
            Assert.Throws<ArgumentException>(() => _runner.Validate(" "));
        }

        [Test]
        public void AnAssetThatIsNotADefinitionIsRefused() {
            var settings = ScriptableObject.CreateInstance<Spellbound.Core.Operations.OperationSettings>();
            var path = _folder.Path + "/Settings.asset";
            AssetDatabase.CreateAsset(settings, path);
            Assert.Throws<ArgumentException>(() => _runner.Validate(path));
            Assert.Throws<ArgumentException>(() => _runner.ValidateAsset(settings));
        }

        [Test]
        public void EachAssetRunIsCachedForTheBanner() {
            var asset = _folder.Create("Cached", "cached", 1);
            var path = AssetDatabase.GetAssetPath(asset);
            string changed = null;
            Action<string> listener = p => changed = p;
            DefinitionReportCache.Changed += listener;

            try {
                Assert.IsFalse(DefinitionReportCache.TryGet(path, out _));
                var run = _runner.Validate(_folder.Path);
                Assert.IsTrue(DefinitionReportCache.TryGet(path, out var cached));
                Assert.AreEqual(path, changed);
                Assert.AreEqual(1, cached.Assets.Count);
                Assert.AreEqual(run.Checks.Count, cached.Checks.Count);
                DefinitionReportCache.Forget(path);
                Assert.IsFalse(DefinitionReportCache.TryGet(path, out _));
            }
            finally {
                DefinitionReportCache.Changed -= listener;
            }
        }

        [Test]
        public void PreviewLinesComeFromTheRegisteredChecks() {
            var asset = _folder.Create("Previewed", "previewed", 42);
            Assert.AreEqual(new[] { "threshold 42 of 100" }, _runner.PreviewLines(asset));
        }

        [Test]
        public void TheSaveWatcherPicksDefinitionPathsOnly() {
            var asset = _folder.Create("Saved", "saved", 1);
            var settings = ScriptableObject.CreateInstance<Spellbound.Core.Operations.OperationSettings>();
            var other = _folder.Path + "/Other.asset";
            AssetDatabase.CreateAsset(settings, other);
            var picked = DefinitionSaveWatcher.DefinitionPathsAmong(new[] { AssetDatabase.GetAssetPath(asset), other, "Assets/Nowhere.asset", null });
            Assert.AreEqual(new[] { AssetDatabase.GetAssetPath(asset) }, picked);
        }

        [Test]
        public void TheDefinitionTypesAreConcreteScriptableObjectsOnTheContract() {
            CollectionAssert.Contains(DefinitionAssets.DefinitionTypes(), typeof(FixtureDefinition));
            Assert.IsTrue(DefinitionAssets.IsDefinitionPath(PackagedFixturePath));
            Assert.IsFalse(DefinitionAssets.IsDefinitionPath("Assets/Nowhere.asset"));
        }
    }
}
