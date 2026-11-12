// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using Spellbound.Core.Authoring;
using Spellbound.Core.Authoring.Editor;
using UnityEditor;
using UnityEngine;

namespace Spellbound.Core.Packages.Editor {
    [CreateAssetMenu(menuName = "Spellbound/Package Identity", fileName = "Identity")]
    public sealed class PackageIdentity : ScriptableObject, IAuthoredDefinition {
        public const string FileName = "Identity.asset";
        public const string ActiveStatus = "active";
        public const string RetiredStatus = "retired";
        public const string JobEmptyCode = "core.identity-job-empty";
        public const string JobLineBreakCode = "core.identity-job-line-break";
        public const string KindEmptyCode = "core.identity-kind-empty";
        public const string LayerEmptyCode = "core.identity-layer-empty";
        public const string FrontDoorEmptyCode = "core.identity-front-door-empty";
        public const string IdentifierFormCode = "core.identity-identifier-form";
        public const string StatusUnknownCode = "core.identity-status-unknown";
        public const string EntryEmptyCode = "core.identity-entry-empty";
        public const string EntryRepeatedCode = "core.identity-entry-repeated";
        public const string SkeletonNegativeCode = "core.identity-skeleton-negative";

        public static readonly IReadOnlyList<string> Kinds = new[] { "foundation", "library", "tool", "studio" };

        public static readonly IReadOnlyList<string> Layers = new[] {
            "foundation", "mechanism", "composer", "tool", "studio"
        };

        public static readonly IReadOnlyList<string> Statuses = new[] { ActiveStatus, RetiredStatus };

        public static readonly IReadOnlyList<string> JudgmentFields = new[] {
            nameof(kind), nameof(layer), nameof(status), nameof(job), nameof(frontDoor), nameof(mustNotOwn),
            nameof(hotPath), nameof(deterministicOutput), nameof(simulation), nameof(machineMatchedJobs)
        };

        public string kind = string.Empty;
        public string layer = string.Empty;
        public string status = ActiveStatus;
        public string job = string.Empty;
        public string frontDoor = string.Empty;
        public string[] mustNotOwn = Array.Empty<string>();
        public bool hotPath;
        public bool deterministicOutput;
        public string[] simulation = Array.Empty<string>();
        public string[] machineMatchedJobs = Array.Empty<string>();
        public int skeletonVersion;

        public static bool IsJudgmentField(string fieldName) {
            foreach (var judged in JudgmentFields) {
                if (string.Equals(judged, fieldName, StringComparison.Ordinal)) {
                    return true;
                }
            }

            return false;
        }

        public static bool IsIdentifier(string text) {
            if (string.IsNullOrEmpty(text) || text[0] < 'a' || text[0] > 'z' || text.EndsWith("-")) {
                return false;
            }

            foreach (var character in text) {
                var legal = (character >= 'a' && character <= 'z') || (character >= '0' && character <= '9')
                            || character == '-';

                if (!legal) {
                    return false;
                }
            }

            return !text.Contains("--");
        }

        public static string PathFor(string packageFolder) {
            var folder = ProjectPaths.Normalize(packageFolder);
            return folder.Length == 0 ? FileName : folder + "/" + FileName;
        }

        public static PackageIdentity Find(string packageFolder) {
            var normalized = ProjectPaths.Normalize(packageFolder);

            if (normalized.Length == 0) {
                return null;
            }

            var folder = DefinitionAssets.ResolveFolder(normalized) ?? normalized;
            return AssetDatabase.LoadAssetAtPath<PackageIdentity>(PathFor(folder));
        }

        public void Validate(ValidationReport report) {
            if (report == null) {
                throw new ArgumentNullException(nameof(report));
            }

            if (string.IsNullOrWhiteSpace(job)) {
                report.Error(JobEmptyCode, "The identity names no job.", nameof(job),
                    "Write the package's one sentence: what it gives a game, in plain words.");
            }
            else if (job.IndexOf('\n') >= 0 || job.IndexOf('\r') >= 0) {
                report.Error(JobLineBreakCode, "The job holds a line break.", nameof(job),
                    "Write the sentence on one line; the inspector wraps it.");
            }

            RequireIdentifier(report, kind, nameof(kind), KindEmptyCode,
                "Name the kind: " + string.Join(", ", Kinds) + ".");
            RequireIdentifier(report, layer, nameof(layer), LayerEmptyCode,
                "Name the layer: " + string.Join(", ", Layers) + ".");

            if (!IsKnownStatus(status)) {
                report.Error(StatusUnknownCode, "The status '" + status + "' is neither active nor retired.",
                    nameof(status),
                    "A package is active, or retired with a catalogue note naming what replaced it.");
            }

            if (string.IsNullOrWhiteSpace(frontDoor)) {
                report.Error(FrontDoorEmptyCode, "The identity names no front door.", nameof(frontDoor),
                    "Name the public type a newcomer reaches first, the package's Kit.");
            }

            CheckEntries(report, mustNotOwn, nameof(mustNotOwn));
            CheckEntries(report, simulation, nameof(simulation));
            CheckEntries(report, machineMatchedJobs, nameof(machineMatchedJobs));

            if (skeletonVersion < 0) {
                report.Error(SkeletonNegativeCode, "The skeleton version " + skeletonVersion + " is below zero.",
                    nameof(skeletonVersion), "Zero means the package was not made from the skeleton.");
            }
        }

        private static bool IsKnownStatus(string value) {
            foreach (var known in Statuses) {
                if (string.Equals(known, value, StringComparison.Ordinal)) {
                    return true;
                }
            }

            return false;
        }

        private static void RequireIdentifier(ValidationReport report, string value, string field, string emptyCode,
            string hint) {
            if (string.IsNullOrWhiteSpace(value)) {
                report.Error(emptyCode, "The identity names no " + field + ".", field, hint);
                return;
            }

            if (!IsIdentifier(value)) {
                report.Error(IdentifierFormCode,
                    "The " + field + " '" + value + "' is not an id: lower-case letters, digits and single hyphens.",
                    field, hint);
            }
        }

        private static void CheckEntries(ValidationReport report, string[] entries, string field) {
            if (entries == null) {
                return;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);

            for (var index = 0; index < entries.Length; index++) {
                var entry = entries[index];

                if (string.IsNullOrWhiteSpace(entry)) {
                    report.Error(EntryEmptyCode, "Entry " + index + " of " + field + " is empty.",
                        field + "[" + index + "]",
                        "Remove the empty entry.");
                    continue;
                }

                if (!seen.Add(entry.Trim())) {
                    report.Warning(EntryRepeatedCode, "'" + entry.Trim() + "' appears twice in " + field + ".",
                        field + "[" + index + "]", "Keep one.");
                }
            }
        }
    }
}
