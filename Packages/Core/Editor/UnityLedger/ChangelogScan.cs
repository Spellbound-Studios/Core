// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using Spellbound.Core.Operations.Editor;

namespace Spellbound.Core.UnityLedger {
    public static class ChangelogScan {
        public static IReadOnlyList<string> LinesBetween(string changelog, string fromVersion, string toVersion,
            IReadOnlyList<string> watchWords) {
            var found = new List<string>();

            if (string.IsNullOrEmpty(changelog) || string.IsNullOrWhiteSpace(toVersion)) {
                return found;
            }

            string section = null;
            var collecting = false;

            foreach (var line in changelog.Replace("\r\n", "\n").Split('\n')) {
                if (TrySectionVersion(line, out var heading)) {
                    section = heading;
                    collecting = IsBetween(section, fromVersion, toVersion);
                    continue;
                }

                if (!collecting) {
                    continue;
                }

                var trimmed = line.Trim();

                if (trimmed.Length > 0 && CarriesWatchWord(trimmed, watchWords)) {
                    found.Add("[" + section + "] " + trimmed);
                }
            }

            return found;
        }

        private static bool TrySectionVersion(string line, out string version) {
            version = null;
            var trimmed = line.TrimStart();

            if (!trimmed.StartsWith("## ", StringComparison.Ordinal)) {
                return false;
            }

            var open = trimmed.IndexOf('[');
            var close = open < 0 ? -1 : trimmed.IndexOf(']', open + 1);
            version = close < 0 ? trimmed.Substring(3).Trim() : trimmed.Substring(open + 1, close - open - 1).Trim();
            return true;
        }

        private static bool IsBetween(string version, string fromVersion, string toVersion) {
            if (string.IsNullOrEmpty(version) || !char.IsDigit(version[0])) {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(fromVersion) && PackageVersionRange.Compare(version, fromVersion) <= 0) {
                return false;
            }

            return PackageVersionRange.Compare(version, toVersion) <= 0;
        }

        private static bool CarriesWatchWord(string line, IReadOnlyList<string> watchWords) {
            if (watchWords == null || watchWords.Count == 0) {
                return true;
            }

            foreach (var word in watchWords) {
                if (!string.IsNullOrWhiteSpace(word)
                    && line.IndexOf(word.Trim(), StringComparison.OrdinalIgnoreCase) >= 0) {
                    return true;
                }
            }

            return false;
        }
    }
}
