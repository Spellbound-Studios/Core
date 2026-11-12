// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Operations.Editor {
    public sealed class PackageVersionRange {
        public string Expression { get; }
        public string Lower { get; }
        public string Upper { get; }
        public bool LowerInclusive { get; }
        public bool UpperInclusive { get; }

        private PackageVersionRange(string expression, string lower, string upper, bool lowerInclusive, bool upperInclusive) {
            Expression = expression;
            Lower = lower;
            Upper = upper;
            LowerInclusive = lowerInclusive;
            UpperInclusive = upperInclusive;
        }

        public static PackageVersionRange Parse(string expression) {
            if (string.IsNullOrWhiteSpace(expression)) {
                throw new ArgumentException("A version range expression must not be empty.", nameof(expression));
            }

            var text = expression.Trim();
            var opensInclusive = text.StartsWith("[", StringComparison.Ordinal);
            var opensExclusive = text.StartsWith("(", StringComparison.Ordinal);

            if (!opensInclusive && !opensExclusive) {
                return new PackageVersionRange(text, text, null, true, false);
            }

            var closesInclusive = text.EndsWith("]", StringComparison.Ordinal);
            var closesExclusive = text.EndsWith(")", StringComparison.Ordinal);

            if (!closesInclusive && !closesExclusive) {
                throw new ArgumentException("The version range '" + expression + "' does not close with ] or ).", nameof(expression));
            }

            var inner = text.Substring(1, text.Length - 2);
            var comma = inner.IndexOf(',');

            if (comma < 0) {
                return new PackageVersionRange(text, inner.Trim(), inner.Trim(), true, true);
            }

            var lower = inner.Substring(0, comma).Trim();
            var upper = inner.Substring(comma + 1).Trim();
            return new PackageVersionRange(text, lower.Length == 0 ? null : lower, upper.Length == 0 ? null : upper, opensInclusive,
                closesInclusive);
        }

        public bool Contains(string version) {
            if (string.IsNullOrWhiteSpace(version)) {
                return false;
            }

            if (Lower != null) {
                var against = Compare(version, Lower);

                if (against < 0 || against == 0 && !LowerInclusive) {
                    return false;
                }
            }

            if (Upper != null) {
                var against = Compare(version, Upper);

                if (against > 0 || against == 0 && !UpperInclusive) {
                    return false;
                }
            }

            return true;
        }

        public static int Compare(string left, string right) {
            SplitPrerelease(left, out var leftCore, out var leftPrerelease);
            SplitPrerelease(right, out var rightCore, out var rightPrerelease);
            var core = CompareNumbers(leftCore.Split('.'), rightCore.Split('.'));

            if (core != 0) {
                return core;
            }

            if (leftPrerelease == null && rightPrerelease == null) {
                return 0;
            }

            if (leftPrerelease == null) {
                return 1;
            }

            if (rightPrerelease == null) {
                return -1;
            }

            return CompareIdentifiers(leftPrerelease.Split('.'), rightPrerelease.Split('.'));
        }

        private static void SplitPrerelease(string version, out string core, out string prerelease) {
            var text = version.Trim();
            var plus = text.IndexOf('+');

            if (plus >= 0) {
                text = text.Substring(0, plus);
            }

            var dash = text.IndexOf('-');
            core = dash < 0 ? text : text.Substring(0, dash);
            prerelease = dash < 0 ? null : text.Substring(dash + 1);
        }

        private static int CompareNumbers(string[] left, string[] right) {
            var count = Math.Max(left.Length, right.Length);

            for (var i = 0; i < count; i++) {
                var leftValue = i < left.Length && long.TryParse(left[i], out var parsedLeft) ? parsedLeft : 0;
                var rightValue = i < right.Length && long.TryParse(right[i], out var parsedRight) ? parsedRight : 0;

                if (leftValue != rightValue) {
                    return leftValue < rightValue ? -1 : 1;
                }
            }

            return 0;
        }

        private static int CompareIdentifiers(string[] left, string[] right) {
            var count = Math.Min(left.Length, right.Length);

            for (var i = 0; i < count; i++) {
                var leftNumeric = long.TryParse(left[i], out var leftValue);
                var rightNumeric = long.TryParse(right[i], out var rightValue);
                int result;

                if (leftNumeric && rightNumeric) {
                    result = leftValue.CompareTo(rightValue);
                }
                else if (leftNumeric || rightNumeric) {
                    result = leftNumeric ? -1 : 1;
                }
                else {
                    result = string.CompareOrdinal(left[i], right[i]);
                }

                if (result != 0) {
                    return result < 0 ? -1 : 1;
                }
            }

            return left.Length.CompareTo(right.Length);
        }

        public override string ToString() {
            return Expression;
        }
    }
}
