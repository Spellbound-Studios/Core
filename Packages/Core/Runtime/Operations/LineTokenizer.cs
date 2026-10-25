// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections.Generic;
using System.Text;

namespace Spellbound.Core.Operations {
    internal static class LineTokenizer {
        public static bool TryTokenize(string line, out List<LineToken> tokens, out string error) {
            tokens = new List<LineToken>();
            error = null;

            if (string.IsNullOrWhiteSpace(line)) {
                return true;
            }

            var current = new StringBuilder();
            var inQuotes = false;
            var hasToken = false;
            var sawQuote = false;

            for (var i = 0; i < line.Length; i++) {
                var character = line[i];

                if (inQuotes) {
                    if (character == '\\' && i + 1 < line.Length && (line[i + 1] == '"' || line[i + 1] == '\\')) {
                        current.Append(line[i + 1]);
                        i++;
                        continue;
                    }

                    if (character == '"') {
                        inQuotes = false;
                        continue;
                    }

                    current.Append(character);
                    continue;
                }

                if (character == '"') {
                    inQuotes = true;
                    hasToken = true;
                    sawQuote = true;
                    continue;
                }

                if (char.IsWhiteSpace(character)) {
                    if (hasToken) {
                        tokens.Add(new LineToken(current.ToString(), sawQuote));
                        current.Clear();
                        hasToken = false;
                        sawQuote = false;
                    }

                    continue;
                }

                current.Append(character);
                hasToken = true;
            }

            if (inQuotes) {
                error = "the line has an unclosed quote";
                return false;
            }

            if (hasToken) {
                tokens.Add(new LineToken(current.ToString(), sawQuote));
            }

            return true;
        }

        public static bool TryFillPlaceholders(List<LineToken> tokens, IReadOnlyDictionary<string, string> placeholders,
            out string error) {
            error = null;

            for (var i = 0; i < tokens.Count; i++) {
                var text = tokens[i].Text;
                var filled = false;
                var open = text.IndexOf('{');

                while (open >= 0) {
                    var close = text.IndexOf('}', open + 1);

                    if (close < 0) {
                        break;
                    }

                    var name = text.Substring(open + 1, close - open - 1);
                    string value = null;

                    if (placeholders == null || !placeholders.TryGetValue(name, out value)) {
                        var supplied = placeholders == null || placeholders.Count == 0
                            ? "none"
                            : string.Join(", ", placeholders.Keys);
                        error = "placeholder {" + name + "} has no value (supplied: " + supplied + ")";
                        return false;
                    }

                    text = text.Substring(0, open) + value + text.Substring(close + 1);
                    filled = true;
                    open = text.IndexOf('{', open + value.Length);
                }

                if (filled) {
                    tokens[i] = new LineToken(text, true);
                }
            }

            return true;
        }
    }
}
