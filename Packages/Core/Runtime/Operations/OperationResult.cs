// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Operations {
    public sealed class OperationResult {
        public OperationStatus Status { get; }
        public string Text { get; }
        public object Value { get; }

        public bool Succeeded => Status == OperationStatus.Succeeded || Status == OperationStatus.Previewed;

        public OperationSeverity Severity {
            get {
                switch (Status) {
                    case OperationStatus.Succeeded:
                    case OperationStatus.Previewed:
                        return OperationSeverity.Information;
                    case OperationStatus.Refused:
                        return OperationSeverity.Warning;
                    default:
                        return OperationSeverity.Error;
                }
            }
        }

        private OperationResult(OperationStatus status, string prose, object value) {
            Status = status;
            Value = value;
            Text = Compose(status, prose);
        }

        public static OperationResult Success(string prose, object value = null) {
            return new OperationResult(OperationStatus.Succeeded, prose, value);
        }

        public static OperationResult Invalid(string prose) {
            return new OperationResult(OperationStatus.Invalid, prose, null);
        }

        public static OperationResult Failed(string prose) {
            return new OperationResult(OperationStatus.Failed, prose, null);
        }

        public static OperationResult Refused(string prose) {
            return new OperationResult(OperationStatus.Refused, prose, null);
        }

        public static OperationResult Previewed(string prose, object value = null) {
            return new OperationResult(OperationStatus.Previewed, prose, value);
        }

        public static string StatusWord(OperationStatus status) {
            return status.ToString().ToLowerInvariant();
        }

        internal OperationResult AsPreviewed() {
            return Status == OperationStatus.Succeeded ? new OperationResult(OperationStatus.Previewed, Prose, Value) : this;
        }

        internal string Prose {
            get {
                var word = StatusWord(Status);

                if (Text.Length <= word.Length + 2) {
                    return string.Empty;
                }

                return Text.Substring(word.Length + 2);
            }
        }

        private static string Compose(OperationStatus status, string prose) {
            var word = StatusWord(status);

            if (string.IsNullOrEmpty(prose)) {
                return word;
            }

            return word + ": " + prose.TrimEnd('\r', '\n');
        }

        public override string ToString() {
            return Text;
        }
    }
}
