// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Spellbound.Core.Authoring;

namespace Spellbound.Core.Testing {
    public static class DeterministicFloatModeScan {
        private const int AttributeWindow = 400;
        private const string DeterministicMarker = "FloatMode.Deterministic";

        private static readonly Regex AttributeBlocks = new Regex(
            @"(?:\[[^\[\]]*\]\s*)+(?:(?:public|internal|private|protected|readonly|unsafe|partial|ref)\s+)*$",
            RegexOptions.CultureInvariant);

        public static ValidationReport Check(IReadOnlyList<string> jobNames, string sourceRoot) {
            if (jobNames == null) {
                throw new ArgumentNullException(nameof(jobNames));
            }

            if (sourceRoot == null) {
                throw new ArgumentNullException(nameof(sourceRoot));
            }

            var report = new ValidationReport(sourceRoot);
            var sources = ReadSources(sourceRoot);

            foreach (var jobName in jobNames) {
                if (string.IsNullOrWhiteSpace(jobName)) {
                    continue;
                }

                CheckJob(report, jobName.Trim(), sources, sourceRoot);
            }

            return report;
        }

        private static void CheckJob(ValidationReport report, string jobName,
            List<KeyValuePair<string, string>> sources, string sourceRoot) {
            var declaration = new Regex(@"\bstruct\s+" + Regex.Escape(jobName) + @"\b", RegexOptions.CultureInvariant);
            var declared = false;

            foreach (var source in sources) {
                foreach (Match match in declaration.Matches(source.Value)) {
                    declared = true;

                    if (CarriesDeterministicFloatMode(source.Value, match.Index)) {
                        continue;
                    }

                    report.Error("core.float-mode-missing",
                        "Job '" + jobName + "' is declared without " + DeterministicMarker + ".", source.Key,
                        "Declare it with [BurstCompile(FloatMode = FloatMode.Deterministic)].");
                }
            }

            if (!declared) {
                report.Error("core.float-mode-job-missing",
                    "No source under '" + sourceRoot + "' declares struct '" + jobName + "'.", sourceRoot,
                    "Check the job name against its declaration.");
            }
        }

        private static bool CarriesDeterministicFloatMode(string text, int declarationIndex) {
            var start = Math.Max(0, declarationIndex - AttributeWindow);
            var window = text.Substring(start, declarationIndex - start);
            var attributes = AttributeBlocks.Match(window);
            return attributes.Success && attributes.Value.Contains(DeterministicMarker);
        }

        private static List<KeyValuePair<string, string>> ReadSources(string sourceRoot) {
            var sources = new List<KeyValuePair<string, string>>();

            if (!Directory.Exists(sourceRoot)) {
                return sources;
            }

            var pending = new Stack<string>();
            pending.Push(sourceRoot);

            while (pending.Count > 0) {
                var folder = pending.Pop();
                var files = Directory.GetFiles(folder, "*.cs");
                Array.Sort(files, StringComparer.Ordinal);

                foreach (var file in files) {
                    sources.Add(new KeyValuePair<string, string>(file, File.ReadAllText(file)));
                }

                foreach (var child in Directory.GetDirectories(folder)) {
                    if (!Path.GetFileName(child).EndsWith("~", StringComparison.Ordinal)) {
                        pending.Push(child);
                    }
                }
            }

            return sources;
        }
    }
}
