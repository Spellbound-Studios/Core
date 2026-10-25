// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using Spellbound.Core.Hashing;

namespace Spellbound.Core.Operations {
    public static class GeneratedBlock {
        private const string OpeningPrefix = "<!-- spellbound:";
        private const string ClosingPrefix = "<!-- /spellbound:";
        private const string Suffix = " -->";

        public static string Hash(string body) {
            return StableHash.Fnv1A32(Normalize(body)).ToString("x8");
        }

        public static string Render(string name, string body) {
            Validate(name);
            var normalized = Normalize(body);
            return OpeningPrefix + name + " " + Hash(normalized) + Suffix + "\n" + normalized + "\n" + ClosingPrefix + name + Suffix;
        }

        public static bool TryFind(string text, string name, out int start, out int length) {
            start = -1;
            length = 0;

            if (string.IsNullOrEmpty(text)) {
                return false;
            }

            Validate(name);
            var opening = OpeningPrefix + name + " ";
            var closing = ClosingPrefix + name + Suffix;
            var openingIndex = text.IndexOf(opening, StringComparison.Ordinal);

            if (openingIndex < 0) {
                return false;
            }

            var closingIndex = text.IndexOf(closing, openingIndex, StringComparison.Ordinal);

            if (closingIndex < 0) {
                return false;
            }

            start = openingIndex;
            length = closingIndex + closing.Length - openingIndex;
            return true;
        }

        public static string Body(string text, string name) {
            if (!TryFind(text, name, out var start, out var length)) {
                return null;
            }

            var block = text.Substring(start, length);
            var firstBreak = block.IndexOf('\n');
            var lastBreak = block.LastIndexOf('\n');

            if (firstBreak < 0 || lastBreak <= firstBreak) {
                return string.Empty;
            }

            return Normalize(block.Substring(firstBreak + 1, lastBreak - firstBreak - 1));
        }

        public static string StoredHash(string text, string name) {
            if (!TryFind(text, name, out var start, out _)) {
                return null;
            }

            var hashStart = start + OpeningPrefix.Length + name.Length + 1;
            var hashEnd = text.IndexOf(Suffix, hashStart, StringComparison.Ordinal);
            return hashEnd < 0 ? null : text.Substring(hashStart, hashEnd - hashStart).Trim();
        }

        public static bool IsCurrent(string text, string name) {
            var body = Body(text, name);
            return body != null && string.Equals(StoredHash(text, name), Hash(body), StringComparison.Ordinal);
        }

        public static string Replace(string text, string name, string body) {
            var existing = text ?? string.Empty;
            var lineBreak = LineBreakOf(existing);
            var rendered = Render(name, body).Replace("\n", lineBreak);

            if (TryFind(existing, name, out var start, out var length)) {
                return existing.Substring(0, start) + rendered + existing.Substring(start + length);
            }

            if (existing.Length == 0) {
                return rendered + lineBreak;
            }

            var separator = existing.EndsWith("\n") ? lineBreak : lineBreak + lineBreak;
            return existing + separator + rendered + lineBreak;
        }

        public static string InsertUnderHeading(string text, string heading, string name, string body) {
            var existing = text ?? string.Empty;

            if (TryFind(existing, name, out _, out _)) {
                return Replace(existing, name, body);
            }

            var headingLine = "## " + heading;
            var headingIndex = IndexOfLine(existing, headingLine);

            if (headingIndex < 0) {
                return Replace(existing, name, body);
            }

            var lineBreak = LineBreakOf(existing);
            var rendered = Render(name, body).Replace("\n", lineBreak);
            var sectionEnd = IndexOfLine(existing, "## ", headingIndex + headingLine.Length);
            var insertAt = sectionEnd < 0 ? existing.Length : sectionEnd;
            var before = existing.Substring(0, insertAt).TrimEnd('\r', '\n');
            var after = existing.Substring(insertAt);
            return before + lineBreak + lineBreak + rendered + lineBreak + (after.Length > 0 ? lineBreak + after : string.Empty);
        }

        private static string LineBreakOf(string text) {
            return text.IndexOf("\r\n", StringComparison.Ordinal) >= 0 ? "\r\n" : "\n";
        }

        private static int IndexOfLine(string text, string lineStart, int from = 0) {
            var index = from;

            while (index <= text.Length) {
                var lineEnd = text.IndexOf('\n', index);
                var line = lineEnd < 0 ? text.Substring(index) : text.Substring(index, lineEnd - index);

                if (line.TrimEnd('\r').StartsWith(lineStart, StringComparison.Ordinal)) {
                    return index;
                }

                if (lineEnd < 0) {
                    break;
                }

                index = lineEnd + 1;
            }

            return -1;
        }

        private static string Normalize(string body) {
            return (body ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Trim('\n');
        }

        private static void Validate(string name) {
            if (string.IsNullOrWhiteSpace(name) || name.IndexOf(' ') >= 0) {
                throw new ArgumentException("A generated block name is one word.", nameof(name));
            }
        }
    }
}
