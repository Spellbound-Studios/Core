// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;

namespace Spellbound.Core.Authoring {
    public sealed class ValidationReport {
        private readonly List<ValidationFinding> _findings = new List<ValidationFinding>();

        public string Subject { get; }
        public IReadOnlyList<ValidationFinding> Findings => _findings;
        public bool HasErrors => Count(FindingSeverity.Error) > 0;
        public bool IsEmpty => _findings.Count == 0;

        public ValidationReport(string subject) {
            Subject = subject ?? string.Empty;
        }

        public ValidationFinding Error(string code, string message, string location = null, string hint = null) {
            return Add(new ValidationFinding(code, FindingSeverity.Error, Subject, message, location, hint));
        }

        public ValidationFinding Warning(string code, string message, string location = null, string hint = null) {
            return Add(new ValidationFinding(code, FindingSeverity.Warning, Subject, message, location, hint));
        }

        public ValidationFinding Information(string code, string message, string location = null, string hint = null) {
            return Add(new ValidationFinding(code, FindingSeverity.Information, Subject, message, location, hint));
        }

        public ValidationFinding Add(ValidationFinding finding) {
            if (finding == null) {
                throw new ArgumentNullException(nameof(finding));
            }

            _findings.Add(finding);
            return finding;
        }

        public void Merge(ValidationReport other) {
            if (other == null || ReferenceEquals(other, this)) {
                return;
            }

            _findings.AddRange(other._findings);
        }

        public void StampCheck(int fromIndex, string check) {
            if (fromIndex < 0) {
                throw new ArgumentOutOfRangeException(nameof(fromIndex), fromIndex, "A finding index is zero or more.");
            }

            for (var index = fromIndex; index < _findings.Count; index++) {
                if (_findings[index].Check == null) {
                    _findings[index] = _findings[index].WithCheck(check);
                }
            }
        }

        public int Count(FindingSeverity severity) {
            var count = 0;

            foreach (var finding in _findings) {
                if (finding.Severity == severity) {
                    count++;
                }
            }

            return count;
        }

        public IReadOnlyList<ValidationFinding> Ordered() {
            var ordered = new List<ValidationFinding>(_findings.Count);
            Append(ordered, FindingSeverity.Error);
            Append(ordered, FindingSeverity.Warning);
            Append(ordered, FindingSeverity.Information);
            return ordered;
        }

        public string Summary() {
            if (_findings.Count == 0) {
                return "no findings";
            }

            var parts = new List<string>();
            AppendCount(parts, FindingSeverity.Error, "error");
            AppendCount(parts, FindingSeverity.Warning, "warning");
            AppendCount(parts, FindingSeverity.Information, "information");
            return string.Join(", ", parts);
        }

        public string ToText() {
            var builder = new StringBuilder();

            foreach (var finding in Ordered()) {
                if (builder.Length > 0) {
                    builder.Append('\n');
                }

                builder.Append(finding.ToText());
            }

            return builder.ToString();
        }

        private void Append(List<ValidationFinding> target, FindingSeverity severity) {
            foreach (var finding in _findings) {
                if (finding.Severity == severity) {
                    target.Add(finding);
                }
            }
        }

        private void AppendCount(List<string> parts, FindingSeverity severity, string word) {
            var count = Count(severity);

            if (count == 0) {
                return;
            }

            var plural = count == 1 || severity == FindingSeverity.Information ? string.Empty : "s";
            parts.Add(count + " " + word + plural);
        }

        public override string ToString() {
            return ToText();
        }
    }
}
