// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;
using Spellbound.Core.Operations;

namespace Spellbound.Core.Authoring.Editor {
    public sealed class ValidationRun {
        public const string ChecksPrefix = "checks: ";

        public string Subject { get; }
        public DateTime Finished { get; }
        public IReadOnlyList<string> Assets { get; }
        public IReadOnlyList<string> OwnValidations { get; }
        public IReadOnlyList<string> RegisteredChecks { get; }
        public ValidationReport Report { get; }
        public IReadOnlyList<ValidationTiming> Timings { get; }
        public bool HasErrors => Report.HasErrors;

        public IReadOnlyList<string> Checks {
            get {
                var all = new List<string>(OwnValidations);
                all.AddRange(RegisteredChecks);
                return all;
            }
        }

        public ValidationRun(string subject, IReadOnlyList<string> assets, IReadOnlyList<string> ownValidations,
            IReadOnlyList<string> registeredChecks, ValidationReport report, IReadOnlyList<ValidationTiming> timings,
            DateTime finished) {
            Subject = subject ?? string.Empty;
            Assets = assets ?? Array.Empty<string>();
            OwnValidations = ownValidations ?? Array.Empty<string>();
            RegisteredChecks = registeredChecks ?? Array.Empty<string>();
            Report = report ?? new ValidationReport(subject);
            Timings = timings ?? Array.Empty<ValidationTiming>();
            Finished = finished;
        }

        public static ValidationRun Merge(string subject, IReadOnlyList<ValidationRun> runs) {
            var assets = new List<string>();
            var ownValidations = new List<string>();
            var registeredChecks = new List<string>();
            var report = new ValidationReport(subject);
            var timings = new List<ValidationTiming>();

            foreach (var run in runs) {
                assets.AddRange(run.Assets);
                report.Merge(run.Report);
                timings.AddRange(run.Timings);
                AddDistinct(ownValidations, run.OwnValidations);
                AddDistinct(registeredChecks, run.RegisteredChecks);
            }

            ownValidations.Sort(string.CompareOrdinal);
            registeredChecks.Sort(string.CompareOrdinal);
            return new ValidationRun(subject, assets, ownValidations, registeredChecks, report, timings, DateTime.Now);
        }

        private static void AddDistinct(List<string> target, IReadOnlyList<string> entries) {
            foreach (var entry in entries) {
                if (!target.Contains(entry)) {
                    target.Add(entry);
                }
            }
        }

        public string Summary() {
            if (Assets.Count == 0) {
                return "no authored definitions under " + Subject;
            }

            var assetsText = Assets.Count == 1 ? "1 asset" : Assets.Count + " assets";
            return Report.Summary() + " in " + assetsText;
        }

        public string ChecksLine() {
            var checks = Checks;
            return ChecksPrefix + (checks.Count == 0 ? "none" : string.Join(", ", checks));
        }

        public string ToText(bool withTimings = false) {
            var builder = new StringBuilder();
            builder.Append(Summary());
            builder.Append('\n').Append(ChecksLine());
            var findings = Report.ToText();

            if (findings.Length > 0) {
                builder.Append('\n').Append(findings);
            }

            if (withTimings && Timings.Count > 0) {
                var table = new OperationTable("check", "asset", "ms");

                foreach (var timing in Timings) {
                    table.AddRow(timing.Check, timing.Asset, timing.Milliseconds.ToString("F3"));
                }

                builder.Append('\n').Append(table.ToText());
            }

            return builder.ToString();
        }

        public override string ToString() {
            return ToText();
        }
    }
}
