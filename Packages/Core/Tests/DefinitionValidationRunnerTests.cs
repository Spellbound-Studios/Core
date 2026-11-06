// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Threading;
using NUnit.Framework;
using Spellbound.Core.Authoring;
using Spellbound.Core.Authoring.Editor;
using Spellbound.Core.Observations;
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
            Assert.AreEqual(3, run.Timings.Count);
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
            Assert.AreEqual(9, run.Timings.Count);
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

        [Test]
        public void TheCheapPathRunsTheChecksThatRunWhileEditingAndNotTheSavedPathOnes() {
            var savedPath = new SavedPathCheck();
            var runner = RunnerWith(savedPath);
            var asset = _folder.Create("Cheap", "cheap", 5);
            runner.ValidateAsset(asset);
            Assert.AreEqual(1, savedPath.Runs);
            asset.threshold = 250;
            var schedule = new BannerSchedule(0, 1000);
            var run = runner.ValidateAsset(asset, ValidationScope.Editing(schedule, DateTime.Now));
            Assert.AreEqual(1, savedPath.Runs);
            Assert.IsTrue(ValidationScope.Editing(schedule, DateTime.Now).Includes(FixtureCheck()));
            Assert.IsFalse(ValidationScope.Editing(schedule, DateTime.Now).Includes(SavedPathDescription(savedPath)));
            Assert.IsTrue(ValidationScope.Everything.Includes(SavedPathDescription(savedPath)));
            Assert.AreEqual(1, Count(run, ThresholdCheckCode));
            Assert.AreEqual(1, Count(run, SavedPathCode));
        }

        [Test]
        public void TheMergedRunKeepsTheSavedPathFindingsWithTheirTimeMarkedStaleAfterAChange() {
            var savedPath = new SavedPathCheck();
            var runner = RunnerWith(savedPath);
            var asset = _folder.Create("Merged", "merged", 5);
            var full = runner.ValidateAsset(asset);
            var savedLabel = SavedPathDescription(savedPath).Label;
            var savedRanAt = full.TimingFor(savedLabel).RanAt;
            asset.threshold = 250;
            var run = runner.ValidateAsset(asset, ValidationScope.Editing(new BannerSchedule(0, 1000),
                full.Finished.AddMilliseconds(1)));
            Assert.IsTrue(DefinitionReportCache.TryGet(AssetDatabase.GetAssetPath(asset), out var cached));
            Assert.AreSame(run, cached);
            Assert.AreEqual(1, Count(run, SavedPathCode));
            Assert.AreEqual(1, Count(run, ThresholdCheckCode));
            Assert.AreEqual(new[] { FixtureCheck().Label, savedLabel }, run.RegisteredChecks);
            var saved = run.TimingFor(savedLabel);
            Assert.IsTrue(saved.Stale);
            Assert.AreEqual(savedRanAt, saved.RanAt);
            Assert.IsFalse(run.TimingFor(FixtureCheck().Label).Stale);
            var savedText = savedLabel + " ran " + savedRanAt.ToString("HH:mm:ss") + " stale";
            Assert.AreEqual(savedText, run.CheckText(savedLabel));
            Assert.AreEqual(BareChecksLine(run), run.ChecksLine());
            Assert.AreEqual(FixtureCheck().Label, run.CheckText(FixtureCheck().Label));
            var shown = run.Annotated();
            Assert.AreEqual(savedText, FindingOf(shown, SavedPathCode).Check);
            Assert.AreEqual(FixtureCheck().Label, FindingOf(shown, ThresholdCheckCode).Check);
            Assert.AreEqual(savedLabel, FindingOf(run, SavedPathCode).Check);
        }

        [Test]
        public void ACheckThatRanAfterTheLastChangeIsCarriedWithoutAStaleMark() {
            var savedPath = new SavedPathCheck();
            var runner = RunnerWith(savedPath);
            var asset = _folder.Create("Current", "current", 5);
            var full = runner.ValidateAsset(asset);
            var run = runner.ValidateAsset(asset, ValidationScope.Editing(new BannerSchedule(0, 1000),
                full.TimingFor(SavedPathDescription(savedPath).Label).RanAt.AddMilliseconds(-1)));
            var carried = run.TimingFor(SavedPathDescription(savedPath).Label);
            Assert.IsFalse(carried.Stale);
            Assert.AreEqual(carried.Check + " ran " + carried.RanAt.ToString("HH:mm:ss"),
                run.CheckText(carried.Check));
            Assert.AreEqual(full.ChecksLine(), run.ChecksLine());
        }

        [Test]
        public void TheFullPathClearsEveryStaleMark() {
            var savedPath = new SavedPathCheck();
            var runner = RunnerWith(savedPath);
            var asset = _folder.Create("Cleared", "cleared", 5);
            var full = runner.ValidateAsset(asset);
            runner.ValidateAsset(asset, ValidationScope.Editing(new BannerSchedule(0, 1000),
                full.Finished.AddMilliseconds(1)));
            var again = runner.ValidateAsset(asset);
            Assert.AreEqual(2, savedPath.Runs);

            foreach (var timing in again.Timings) {
                Assert.IsFalse(timing.Stale, timing.Check);
                Assert.IsFalse(timing.Demoted, timing.Check);
            }

            Assert.IsTrue(DefinitionReportCache.TryGet(AssetDatabase.GetAssetPath(asset), out var cached));
            Assert.AreSame(again, cached);
            Assert.AreEqual(full.ChecksLine(), again.ChecksLine());
        }

        [Test]
        public void ACheckOverTheBudgetIsDemotedForTheSessionAndTheFullPathStillRunsIt() {
            var slow = new SlowEditingCheck();
            var description = new DefinitionCheckDescription(SlowCode, true, slow, typeof(FixtureDefinition));
            var runner = new DefinitionValidationRunner(new DefinitionCheckRegistry(new[] { description }));
            var asset = _folder.Create("Slow", "slow", 5);
            var schedule = new BannerSchedule(0, 5);
            var run = runner.ValidateAsset(asset, ValidationScope.Editing(schedule, DateTime.Now));
            Assert.AreEqual(1, slow.Runs);
            Assert.IsTrue(schedule.IsDemoted(description.Label));
            var timing = run.TimingFor(description.Label);
            Assert.IsTrue(timing.Demoted);
            Assert.AreEqual(5, timing.BudgetMilliseconds);
            var expected = description.Label + " moved to the saved path for this session ("
                           + (int)Math.Ceiling(timing.Milliseconds) + " ms over 5 ms)";
            Assert.AreEqual(expected, run.CheckText(description.Label));
            Assert.AreEqual(BareChecksLine(run), run.ChecksLine());
            runner.ValidateAsset(asset, ValidationScope.Editing(schedule, DateTime.Now));
            Assert.AreEqual(1, slow.Runs);
            Assert.IsFalse(ValidationScope.Editing(schedule, DateTime.Now).Includes(description));
            var full = runner.ValidateAsset(asset);
            Assert.AreEqual(2, slow.Runs);
            Assert.IsFalse(full.TimingFor(description.Label).Demoted);
            Assert.IsTrue(schedule.IsDemoted(description.Label));
        }

        [Test]
        public void EachCheckRunIsObservedUnderItsCodePrefix() {
            var savedPath = new SavedPathCheck();
            var registry = new DefinitionCheckRegistry(new[] { FixtureCheck(), SavedPathDescription(savedPath) });

            using (var observations = CoreKit.Observations.CreateContext("definition-validation-runner-tests")) {
                var runner = new DefinitionValidationRunner(registry, observations);
                var asset = _folder.Create("Observed", "observed", 5);
                runner.ValidateAsset(asset);
                Assert.IsTrue(observations.TryRead(ObservationKey.Parse("fixturecheck.validate_milliseconds"), out _));
                Assert.IsTrue(observations.TryRead(ObservationKey.Parse("test.validate_milliseconds"), out _));
                runner.ValidateAsset(asset, ValidationScope.Editing(new BannerSchedule(0, 75), DateTime.Now));
                Assert.AreEqual(75, observations.Budget(ObservationKey.Parse("fixturecheck.validate_milliseconds")));
            }

            Assert.DoesNotThrow(() => RunnerWith(savedPath).ValidateAsset(_folder.Create("Discarded", "discarded", 5)));
        }

        [Test]
        public void TheCheapPathWithNoEarlierRunStoresWhatItRan() {
            var savedPath = new SavedPathCheck();
            var asset = _folder.Create("First", "first", 5);
            var run = RunnerWith(savedPath).ValidateAsset(asset,
                ValidationScope.Editing(new BannerSchedule(0, 1000), DateTime.Now));
            Assert.AreEqual(0, savedPath.Runs);
            Assert.AreEqual(new[] { FixtureCheck().Label }, run.RegisteredChecks);
            Assert.IsTrue(DefinitionReportCache.TryGet(AssetDatabase.GetAssetPath(asset), out var cached));
            Assert.AreSame(run, cached);
        }

        [Test]
        public void TheDemotionNoteStaysInTheCheckRowWhileTheCheckIsDemoted() {
            var slow = new SlowEditingCheck();
            var description = new DefinitionCheckDescription(SlowCode, true, slow, typeof(FixtureDefinition));
            var runner = new DefinitionValidationRunner(new DefinitionCheckRegistry(new[] { description }));
            var asset = _folder.Create("StillDemoted", "still demoted", 5);
            var schedule = new BannerSchedule(0, 5);
            var demoting = runner.ValidateAsset(asset, ValidationScope.Editing(schedule, DateTime.Now));
            var expected = description.Label + " moved to the saved path for this session ("
                           + (int)Math.Ceiling(demoting.TimingFor(description.Label).Milliseconds) + " ms over 5 ms)";
            var full = runner.ValidateAsset(asset);
            Assert.AreEqual(description.Label, full.CheckText(description.Label));
            Assert.AreEqual(expected, full.Annotated(schedule).CheckText(description.Label));
            Assert.AreEqual(BareChecksLine(full), full.Annotated(schedule).ChecksLine());
            Assert.AreEqual(full.ChecksLine(), full.Annotated().ChecksLine());
            var carried = runner.ValidateAsset(asset,
                ValidationScope.Editing(schedule, full.Finished.AddMilliseconds(1)));
            StringAssert.StartsWith(expected, carried.Annotated(schedule).CheckText(description.Label));
            Assert.AreEqual(2, slow.Runs);
        }

        [Test]
        public void CoreValidateRecordsEachCheckRunInTheOperationsObservationContext() {
            var settings = ScriptableObject.CreateInstance<Spellbound.Core.Operations.OperationSettings>();
            var host = new Spellbound.Core.Operations.OperationHost(new Spellbound.Core.Operations.OperationCatalog(
                () => new[] {
                    typeof(Spellbound.Core.Operations.OperationHost).Assembly,
                    typeof(DefinitionValidationRunner).Assembly
                }), () => settings);
            var asset = _folder.Create("Operated", "operated", 5);
            var key = ObservationKey.Parse("fixturecheck.validate_milliseconds");

            try {
                using (var observations = CoreKit.Observations.CreateContext("core-validate-observation-tests")) {
                    host.Run("core validate --path " + AssetDatabase.GetAssetPath(asset),
                        Spellbound.Core.Operations.OperationCaller.Development(), observations);
                    Assert.IsTrue(observations.TryRead(key, out _));
                }
            }
            finally {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void BannerRunsShareOneSettingsInstance() {
            var asset = _folder.Create("Shared", "shared", 5);
            DefinitionInspectorBanner.RunChecks(asset);
            var before = Resources.FindObjectsOfTypeAll<Spellbound.Core.Operations.OperationSettings>().Length;

            for (var run = 0; run < 3; run++) {
                Assert.IsNotNull(DefinitionInspectorBanner.RunChecks(asset));
            }

            var after = Resources.FindObjectsOfTypeAll<Spellbound.Core.Operations.OperationSettings>().Length;
            Assert.AreEqual(before, after);
        }

        [Test]
        public void TheScanIsTimedInTheRunAndLeftOutOfTheChecksLine() {
            var run = _runner.ValidateAsset(_folder.Create("ScanTimed", "scan timed", 5));
            Assert.IsNotNull(run.TimingFor("AuthoredKeyScan (Core)"));
            CollectionAssert.DoesNotContain(run.Checks, "AuthoredKeyScan (Core)");
        }

        private const string ThresholdCheckCode = "fixturecheck.threshold-high";
        private const string SavedPathCode = "test.saved-path-only";
        private const string SlowCode = "test.slow-while-editing";

        private static DefinitionCheckDescription FixtureCheck() {
            return DefinitionCheckRegistry.Default.Find(ThresholdCheckCode);
        }

        private static DefinitionCheckDescription SavedPathDescription(SavedPathCheck check) {
            return new DefinitionCheckDescription(SavedPathCode, false, check, typeof(FixtureDefinition));
        }

        private static DefinitionValidationRunner RunnerWith(SavedPathCheck savedPath) {
            return new DefinitionValidationRunner(
                new DefinitionCheckRegistry(new[] { FixtureCheck(), SavedPathDescription(savedPath) }));
        }

        private static string BareChecksLine(ValidationRun run) {
            return ValidationRun.ChecksPrefix + string.Join(", ", run.Checks);
        }

        private static int Count(ValidationRun run, string code) {
            var count = 0;

            foreach (var finding in run.Report.Findings) {
                if (finding.Code == code) {
                    count++;
                }
            }

            return count;
        }

        private static ValidationFinding FindingOf(ValidationRun run, string code) {
            foreach (var finding in run.Report.Findings) {
                if (finding.Code == code) {
                    return finding;
                }
            }

            return null;
        }

        private sealed class SavedPathCheck : IDefinitionCheck<FixtureDefinition> {
            public int Runs { get; private set; }

            public void Check(FixtureDefinition definition, ValidationReport report) {
                Runs++;
                report.Warning(SavedPathCode, "found on the saved path");
            }
        }

        private sealed class SlowEditingCheck : IDefinitionCheck<FixtureDefinition> {
            public int Runs { get; private set; }

            public void Check(FixtureDefinition definition, ValidationReport report) {
                Runs++;
                Thread.Sleep(30);
            }
        }
    }
}
