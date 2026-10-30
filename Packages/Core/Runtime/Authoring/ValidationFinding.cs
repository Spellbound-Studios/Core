// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;

namespace Spellbound.Core.Authoring {
    public sealed class ValidationFinding {
        private static readonly IReadOnlyDictionary<string, string> NoHints = new Dictionary<string, string>();

        public string Code { get; }
        public FindingSeverity Severity { get; }
        public string Subject { get; }
        public string Location { get; }
        public string Message { get; }
        public string Hint { get; }
        public string FixCode { get; }
        public IReadOnlyDictionary<string, string> NamedHints { get; }
        public string Check { get; }

        public ValidationFinding(string code, FindingSeverity severity, string subject, string message,
            string location = null, string hint = null, string fixCode = null,
            IReadOnlyDictionary<string, string> namedHints = null, string check = null) {
            if (!IsCode(code)) {
                throw new ArgumentException("A finding code is <package>.<kebab-name> in lower case; '" + code + "' is not.",
                    nameof(code));
            }

            if (string.IsNullOrWhiteSpace(message)) {
                throw new ArgumentException("A finding needs a message.", nameof(message));
            }

            Code = code;
            Severity = severity;
            Subject = subject ?? string.Empty;
            Message = message.Trim();
            Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim();
            Hint = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
            FixCode = string.IsNullOrWhiteSpace(fixCode) ? null : fixCode.Trim();
            NamedHints = namedHints ?? NoHints;
            Check = string.IsNullOrWhiteSpace(check) ? null : check.Trim();
        }

        public ValidationFinding WithSubject(string subject) {
            return new ValidationFinding(Code, Severity, subject, Message, Location, Hint, FixCode, NamedHints, Check);
        }

        public ValidationFinding WithCheck(string check) {
            return new ValidationFinding(Code, Severity, Subject, Message, Location, Hint, FixCode, NamedHints, check);
        }

        public static bool IsCode(string code) {
            if (string.IsNullOrEmpty(code) || code.IndexOf('.') <= 0 || code.EndsWith(".") || code.Contains("..")) {
                return false;
            }

            foreach (var character in code) {
                var legal = (character >= 'a' && character <= 'z') || (character >= '0' && character <= '9')
                            || character == '.' || character == '-';

                if (!legal) {
                    return false;
                }
            }

            return true;
        }

        public static string SeverityWord(FindingSeverity severity) {
            return severity.ToString().ToLowerInvariant();
        }

        public string ToText() {
            var builder = new StringBuilder();
            builder.Append(Code).Append(' ').Append(SeverityWord(Severity));

            if (Subject.Length > 0) {
                builder.Append(' ').Append(Subject);
            }

            if (Location != null) {
                builder.Append(" at ").Append(Location);
            }

            builder.Append(": ").Append(Message);

            if (Hint != null) {
                builder.Append(" (").Append(Hint).Append(')');
            }

            if (FixCode != null) {
                builder.Append(" fix: ").Append(FixCode);
            }

            return builder.ToString();
        }

        public override string ToString() {
            return ToText();
        }
    }
}
