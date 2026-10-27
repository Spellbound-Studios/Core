// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Spellbound.Core.Ledger {
    public static class LedgerStaleness {
        public const string DocumentationAddress = "https://docs.unity3d.com/";
        public const string DocumentationSearchPage = "/Documentation/Manual/30_search.html?q=";
        public const string DefaultSearch = "What's new";

        public static string EditorMajorMinor(string unityVersion) {
            if (!TryMajorMinor(unityVersion, out var major, out var minor)) {
                return null;
            }

            return major.ToString(CultureInfo.InvariantCulture) + "." + minor.ToString(CultureInfo.InvariantCulture);
        }

        public static IReadOnlyList<string> EditorVersionsBetween(string judged, string current) {
            var versions = new List<string>();

            if (!TryMajorMinor(current, out var currentMajor, out var currentMinor)) {
                return versions;
            }

            var currentText = currentMajor.ToString(CultureInfo.InvariantCulture) + "."
                              + currentMinor.ToString(CultureInfo.InvariantCulture);

            if (!TryMajorMinor(judged, out var judgedMajor, out var judgedMinor)) {
                versions.Add(currentText);
                return versions;
            }

            if (judgedMajor != currentMajor) {
                if (currentMajor > judgedMajor) {
                    versions.Add(currentText);
                }

                return versions;
            }

            for (var minor = judgedMinor + 1; minor <= currentMinor; minor++) {
                versions.Add(currentMajor.ToString(CultureInfo.InvariantCulture) + "."
                             + minor.ToString(CultureInfo.InvariantCulture));
            }

            return versions;
        }

        public static string DocumentationSearchAddress(string editorMajorMinor, IReadOnlyList<string> watchWords) {
            var query = new StringBuilder();

            if (watchWords != null) {
                foreach (var word in watchWords) {
                    if (string.IsNullOrWhiteSpace(word)) {
                        continue;
                    }

                    if (query.Length > 0) {
                        query.Append('+');
                    }

                    query.Append(Encode(word.Trim()));
                }
            }

            if (query.Length == 0) {
                query.Append(Encode(DefaultSearch));
            }

            return DocumentationAddress + editorMajorMinor + DocumentationSearchPage + query;
        }

        private static bool TryMajorMinor(string version, out int major, out int minor) {
            major = 0;
            minor = 0;

            if (string.IsNullOrWhiteSpace(version)) {
                return false;
            }

            var parts = version.Trim().Split('.');

            if (parts.Length < 2) {
                return false;
            }

            return int.TryParse(LeadingDigits(parts[0]), NumberStyles.None, CultureInfo.InvariantCulture, out major)
                   && int.TryParse(LeadingDigits(parts[1]), NumberStyles.None, CultureInfo.InvariantCulture, out minor);
        }

        private static string LeadingDigits(string text) {
            var length = 0;

            while (length < text.Length && text[length] >= '0' && text[length] <= '9') {
                length++;
            }

            return text.Substring(0, length);
        }

        private static string Encode(string text) {
            var builder = new StringBuilder();

            foreach (var value in Encoding.UTF8.GetBytes(text)) {
                var character = (char)value;

                if (IsUnreserved(character)) {
                    builder.Append(character);
                }
                else if (character == ' ') {
                    builder.Append('+');
                }
                else {
                    builder.Append('%').Append(value.ToString("X2", CultureInfo.InvariantCulture));
                }
            }

            return builder.ToString();
        }

        private static bool IsUnreserved(char character) {
            return character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z'
                   || character >= '0' && character <= '9' || character == '-' || character == '_' || character == '.'
                   || character == '~';
        }
    }
}
