// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Spellbound.Core.Observations;
using Spellbound.Core.Operations;
using UnityEditor;
using UnityEngine;

namespace Spellbound.Core.Authoring.Editor {
    public sealed class DefinitionValidationRunner {
        public const string ValidationThrewCode = "core.validation-threw";
        public const string CheckThrewCode = "core.check-threw";
        public const string UnknownKeyCode = "core.unknown-key";
        public const string RenamedKeyCode = "core.renamed-key";
        public const string MissingTypeCode = "core.missing-type";
        public const string ScanThrewCode = "core.scan-threw";
        public const string ScanUnavailableCode = "core.scan-unavailable";
        public const string EverythingSubject = "the project";

        internal const string ValidateMillisecondsSuffix = ".validate_milliseconds";
        internal const string ValidateMillisecondsDescription =
            "The milliseconds the last run of a definition check under this prefix took; the budget is the check"
            + " budget of the run while editing.";

        private readonly DefinitionCheckRegistry _checks;
        private readonly AuthoredKeyScan _keyScan;
        private readonly ObservationContext _observations;
        private readonly string _scanLabel;

        public DefinitionValidationRunner() : this(null) { }

        public DefinitionValidationRunner(DefinitionCheckRegistry checks) : this(checks, null) { }

        public DefinitionValidationRunner(DefinitionCheckRegistry checks, ObservationContext observations) : this(
            checks, observations, null) { }

        public DefinitionValidationRunner(DefinitionCheckRegistry checks, ObservationContext observations,
            OperationSettings settings) {
            _checks = checks ?? DefinitionCheckRegistry.Default;
            _observations = observations ?? ObservationContext.Discard;
            _keyScan = new AuthoredKeyScan(settings != null ? settings : OperationSettings.Load());
            _scanLabel = PackageLabel.OwnValidation(typeof(AuthoredKeyScan));
        }

        public ValidationRun Validate(string subjectPath) {
            var normalized = ProjectPaths.Normalize(subjectPath);

            if (normalized.Length == 0) {
                throw new ArgumentException("Name an asset, a folder under Assets or a package folder under Packages.",
                    nameof(subjectPath));
            }

            if (!ProjectPaths.IsReadable(normalized)) {
                throw new ArgumentException("'" + subjectPath + "' is outside Assets and Packages.", nameof(subjectPath));
            }

            var folder = DefinitionAssets.ResolveFolder(normalized);

            if (folder != null) {
                return ValidateFolder(folder);
            }

            var asset = AssetDatabase.LoadMainAssetAtPath(normalized) as ScriptableObject;

            if (asset == null) {
                throw new ArgumentException("'" + subjectPath + "' is neither an asset nor a folder of this project.",
                    nameof(subjectPath));
            }

            if (!DefinitionAssets.IsDefinition(asset)) {
                throw new ArgumentException(
                    "'" + subjectPath + "' is a " + asset.GetType().Name + ", which does not implement IAuthoredDefinition.",
                    nameof(subjectPath));
            }

            return ValidateAsset(asset);
        }

        public ValidationRun ValidateFolder(string folder) {
            return ValidatePaths(folder, DefinitionAssets.FindInFolder(folder));
        }

        public ValidationRun ValidateAll() {
            return ValidatePaths(EverythingSubject, DefinitionAssets.FindAll());
        }

        public ValidationRun ValidateDrafts(OperationSettings settings) {
            var folder = DefinitionDrafts.Folder(settings);
            return AssetDatabase.IsValidFolder(folder)
                ? ValidateFolder(folder)
                : ValidatePaths(folder, Array.Empty<string>());
        }

        public ValidationRun ValidatePaths(string subject, IReadOnlyList<string> assetPaths) {
            var runs = new List<ValidationRun>();

            foreach (var path in assetPaths) {
                var asset = AssetDatabase.LoadMainAssetAtPath(path) as ScriptableObject;

                if (DefinitionAssets.IsDefinition(asset)) {
                    runs.Add(ValidateAsset(asset));
                }
            }

            return ValidationRun.Merge(subject, runs);
        }

        public ValidationRun ValidateAsset(ScriptableObject asset) {
            return ValidateAsset(asset, ValidationScope.Everything);
        }

        public ValidationRun ValidateAsset(ScriptableObject asset, ValidationScope scope) {
            if (scope == null) {
                throw new ArgumentNullException(nameof(scope));
            }

            if (asset == null) {
                throw new ArgumentNullException(nameof(asset));
            }

            if (!(asset is IAuthoredDefinition definition)) {
                throw new ArgumentException(asset.GetType().FullName + " does not implement IAuthoredDefinition.",
                    nameof(asset));
            }

            var path = AssetDatabase.GetAssetPath(asset);
            var subject = string.IsNullOrEmpty(path) ? asset.name : path;
            var report = new ValidationReport(subject);
            var ownValidation = PackageLabel.OwnValidation(asset.GetType());
            var checks = new List<string>();
            var timings = new List<ValidationTiming>();
            var started = DateTime.Now;
            var watch = Stopwatch.StartNew();

            try {
                definition.Validate(report);
            }
            catch (Exception exception) {
                report.Error(ValidationThrewCode,
                    asset.GetType().Name + ".Validate threw " + exception.GetType().Name + ": " + exception.Message);
            }

            timings.Add(new ValidationTiming(ownValidation, subject, watch.Elapsed.TotalMilliseconds, started));

            if (!string.IsNullOrEmpty(path)) {
                timings.Add(Scanned(asset, report, subject));
            }

            foreach (var check in _checks.For(asset.GetType())) {
                if (!scope.Includes(check)) {
                    continue;
                }

                checks.Add(check.Label);
                var countBefore = report.Findings.Count;
                watch.Restart();

                try {
                    check.Run(asset, report);
                }
                catch (Exception exception) {
                    report.Error(CheckThrewCode,
                        check.Name + " threw " + exception.GetType().Name + ": " + exception.Message);
                }

                report.StampCheck(countBefore, check.Label);
                timings.Add(Measured(check, subject, watch.Elapsed.TotalMilliseconds, started, scope));
            }

            var run = new ValidationRun(subject, new[] { subject }, new[] { ownValidation }, checks, report, timings,
                DateTime.Now);

            if (!string.IsNullOrEmpty(path)) {
                if (scope.IsEditing && DefinitionReportCache.TryGet(path, out var earlier)) {
                    run = ValidationRun.MergePartial(earlier, run, scope.ChangedAt);
                }

                DefinitionReportCache.Store(path, run);
            }

            return run;
        }

        private ValidationTiming Scanned(ScriptableObject asset, ValidationReport report, string subject) {
            var started = DateTime.Now;
            var watch = Stopwatch.StartNew();

            try {
                _keyScan.Scan(asset, report);
            }
            catch (Exception exception) {
                var thrown = exception is TargetInvocationException && exception.InnerException != null
                    ? exception.InnerException
                    : exception;
                report.Error(ScanThrewCode, "the authored-key scan of " + asset.name + " threw "
                                            + thrown.GetType().Name + ": " + thrown.Message);
            }

            return new ValidationTiming(_scanLabel, subject, watch.Elapsed.TotalMilliseconds, started);
        }

        private ValidationTiming Measured(DefinitionCheckDescription check, string subject, double milliseconds,
            DateTime ranAt, ValidationScope scope) {
            Observe(check, milliseconds, scope);
            var timing = new ValidationTiming(check.Label, subject, milliseconds, ranAt);

            if (!scope.IsEditing || !scope.Schedule.IsOverBudget(milliseconds)) {
                return timing;
            }

            scope.Schedule.Demote(check.Label, milliseconds);
            return timing.DemotedOver(milliseconds, scope.Schedule.CheckBudgetMilliseconds);
        }

        private void Observe(DefinitionCheckDescription check, double milliseconds, ValidationScope scope) {
            if (_observations.IsDiscard) {
                return;
            }

            var prefix = check.Name.Substring(0, check.Name.IndexOf('.')).Replace('-', '_');

            if (!ObservationKey.TryParse(prefix + ValidateMillisecondsSuffix, out _, out _)) {
                return;
            }

            var gauge = CoreKit.Observations.Gauge(prefix + ValidateMillisecondsSuffix,
                ValidateMillisecondsDescription, ObservationUnit.Milliseconds);
            gauge.Set(_observations, milliseconds);

            if (scope.IsEditing) {
                _observations.SetBudget(gauge.Key, scope.Schedule.CheckBudgetMilliseconds);
            }
        }

        public IReadOnlyList<string> PreviewLines(ScriptableObject asset) {
            var lines = new List<string>();

            if (asset == null) {
                return lines;
            }

            foreach (var check in _checks.For(asset.GetType())) {
                try {
                    lines.AddRange(check.PreviewLines(asset));
                }
                catch (Exception exception) {
                    lines.Add(check.Name + " threw " + exception.GetType().Name + ": " + exception.Message);
                }
            }

            return lines;
        }
    }
}
