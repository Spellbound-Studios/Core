// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;
using Spellbound.Core.Operations;

namespace Spellbound.Core.Authoring.Editor {
    public sealed class ValidationRun {
        public const string ChecksPrefix = "checks: ";

        internal const string DemotedText = "moved to the saved path for this session";
        internal const string RanPrefix = "ran ";
        internal const string StaleText = "stale";
        internal const string TimeFormat = "HH:mm:ss";

        private const string NotesSeparator = "; ";

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

        internal static ValidationRun MergePartial(ValidationRun full, ValidationRun partial, DateTime changedAt) {
            if (partial == null) {
                throw new ArgumentNullException(nameof(partial));
            }

            if (full == null) {
                return partial;
            }

            var ran = new HashSet<string>(partial.Checks, StringComparer.Ordinal);
            var ownValidations = new List<string>(full.OwnValidations);
            var registeredChecks = new List<string>(full.RegisteredChecks);
            AddDistinct(ownValidations, partial.OwnValidations);
            AddDistinct(registeredChecks, partial.RegisteredChecks);
            var report = new ValidationReport(partial.Report.Subject);
            var timings = new List<ValidationTiming>();

            foreach (var finding in partial.Report.Findings) {
                if (finding.Check == null) {
                    report.Add(finding);
                }
            }

            var listed = new HashSet<string>(ownValidations, StringComparer.Ordinal);
            listed.UnionWith(registeredChecks);
            Carry(ownValidations, ran, full, partial, changedAt, report, timings);

            foreach (var timing in partial.Timings) {
                if (!listed.Contains(timing.Check)) {
                    timings.Add(timing);
                }
            }

            Carry(registeredChecks, ran, full, partial, changedAt, report, timings);
            return new ValidationRun(partial.Subject, partial.Assets, ownValidations, registeredChecks, report, timings,
                partial.Finished);
        }

        private static void Carry(IReadOnlyList<string> checks, HashSet<string> ran, ValidationRun full,
            ValidationRun partial, DateTime changedAt, ValidationReport report, List<ValidationTiming> timings) {
            foreach (var check in checks) {
                var source = ran.Contains(check) ? partial : full;

                foreach (var finding in source.Report.Findings) {
                    if (string.Equals(finding.Check, check, StringComparison.Ordinal)) {
                        report.Add(finding);
                    }
                }

                var timing = source.TimingFor(check);

                if (timing == null) {
                    continue;
                }

                timings.Add(source == full ? timing.CarriedForward(timing.RanAt < changedAt) : timing);
            }
        }

        public ValidationTiming TimingFor(string check) {
            foreach (var timing in Timings) {
                if (string.Equals(timing.Check, check, StringComparison.Ordinal)) {
                    return timing;
                }
            }

            return null;
        }

        public string CheckText(string check) {
            var timing = TimingFor(check);

            if (timing == null || (!timing.Carried && !timing.Demoted)) {
                return check;
            }

            var notes = new List<string>();

            if (timing.Demoted) {
                notes.Add(DemotedText + " (" + (int)Math.Ceiling(timing.DemotedMilliseconds) + " ms over "
                          + timing.BudgetMilliseconds + " ms)");
            }

            if (timing.Carried) {
                var stale = timing.Stale ? " " + StaleText : string.Empty;
                notes.Add(RanPrefix + timing.RanAt.ToString(TimeFormat) + stale);
            }

            return check + " " + string.Join(" ", notes);
        }

        internal string CheckNotes() {
            var notes = new List<string>();

            foreach (var check in Checks) {
                var text = CheckText(check);

                if (!string.Equals(text, check, StringComparison.Ordinal)) {
                    notes.Add(text);
                }
            }

            return string.Join(NotesSeparator, notes);
        }

        public ValidationRun Annotated(BannerSchedule demotions = null) {
            var timings = WithDemotions(demotions);
            var noted = ReferenceEquals(timings, Timings)
                ? this
                : new ValidationRun(Subject, Assets, OwnValidations, RegisteredChecks, Report, timings, Finished);
            var report = new ValidationReport(Report.Subject);
            var annotated = false;

            foreach (var finding in Report.Findings) {
                var text = finding.Check == null ? null : noted.CheckText(finding.Check);

                if (text == null || string.Equals(text, finding.Check, StringComparison.Ordinal)) {
                    report.Add(finding);
                    continue;
                }

                report.Add(finding.WithCheck(text));
                annotated = true;
            }

            return annotated
                ? new ValidationRun(Subject, Assets, OwnValidations, RegisteredChecks, report, timings, Finished)
                : noted;
        }

        private IReadOnlyList<ValidationTiming> WithDemotions(BannerSchedule demotions) {
            if (demotions == null) {
                return Timings;
            }

            var timings = new List<ValidationTiming>(Timings.Count);
            var changed = false;

            foreach (var timing in Timings) {
                if (!timing.Demoted && demotions.TryGetDemotion(timing.Check, out var milliseconds)) {
                    timings.Add(timing.DemotedOver(milliseconds, demotions.CheckBudgetMilliseconds));
                    changed = true;
                }
                else {
                    timings.Add(timing);
                }
            }

            return changed ? timings : Timings;
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
