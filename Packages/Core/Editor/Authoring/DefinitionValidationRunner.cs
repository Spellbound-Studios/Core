// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Spellbound.Core.Operations;
using UnityEditor;
using UnityEngine;

namespace Spellbound.Core.Authoring.Editor {
    public sealed class DefinitionValidationRunner {
        public const string ValidationThrewCode = "core.validation-threw";
        public const string CheckThrewCode = "core.check-threw";
        public const string EverythingSubject = "the project";

        private readonly DefinitionCheckRegistry _checks;

        public DefinitionValidationRunner() : this(null) { }

        public DefinitionValidationRunner(DefinitionCheckRegistry checks) {
            _checks = checks ?? DefinitionCheckRegistry.Default;
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
            var watch = Stopwatch.StartNew();

            try {
                definition.Validate(report);
            }
            catch (Exception exception) {
                report.Error(ValidationThrewCode,
                    asset.GetType().Name + ".Validate threw " + exception.GetType().Name + ": " + exception.Message);
            }

            timings.Add(new ValidationTiming(ownValidation, subject, watch.Elapsed.TotalMilliseconds));

            foreach (var check in _checks.For(asset.GetType())) {
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
                timings.Add(new ValidationTiming(check.Label, subject, watch.Elapsed.TotalMilliseconds));
            }

            var run = new ValidationRun(subject, new[] { subject }, new[] { ownValidation }, checks, report, timings,
                DateTime.Now);

            if (!string.IsNullOrEmpty(path)) {
                DefinitionReportCache.Store(path, run);
            }

            return run;
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
