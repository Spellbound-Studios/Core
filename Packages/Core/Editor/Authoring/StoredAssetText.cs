// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Spellbound.Core.Authoring.Editor {
    internal sealed class StoredAssetText {
        private const string DocumentStart = "---";
        private const string KeySeparator = ": ";
        private const char AnchorMarker = '&';

        private readonly string[] _lines;
        private readonly List<StoredKey> _keys = new List<StoredKey>();
        private int _index;

        private StoredAssetText(string[] lines) {
            _lines = (string[])lines.Clone();
        }

        public static IReadOnlyList<StoredKey> ObjectAt(string[] lines, long anchor) {
            return new StoredAssetText(lines).ReadObjectAt(anchor);
        }

        private IReadOnlyList<StoredKey> ReadObjectAt(long anchor) {
            while (_index < _lines.Length) {
                var line = _lines[_index];
                _index++;

                if (!line.StartsWith(DocumentStart) || !TryAnchor(line, out var found) || found != anchor) {
                    continue;
                }

                var classLine = NextContent();

                if (classLine < 0 || IndentOf(_lines[classLine]) != 0) {
                    return null;
                }

                _index = classLine + 1;
                var body = NextContent();

                if (body < 0 || _lines[body].StartsWith(DocumentStart) || IndentOf(_lines[body]) == 0) {
                    return _keys;
                }

                ReadMapping(IndentOf(_lines[body]), string.Empty);
                return _keys;
            }

            return null;
        }

        private static bool TryAnchor(string line, out long anchor) {
            anchor = 0;
            var marker = line.IndexOf(AnchorMarker);

            if (marker < 0) {
                return false;
            }

            var end = marker + 1;

            while (end < line.Length && (char.IsDigit(line[end]) || (end == marker + 1 && line[end] == '-'))) {
                end++;
            }

            return long.TryParse(line.Substring(marker + 1, end - marker - 1), NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out anchor);
        }

        private void ReadMapping(int indent, string prefix) {
            while (_index < _lines.Length) {
                var line = _lines[_index];

                if (IsBlank(line)) {
                    _index++;
                    continue;
                }

                var lineIndent = IndentOf(line);

                if (lineIndent < indent || line.StartsWith(DocumentStart) || IsSequenceItem(line, lineIndent)) {
                    return;
                }

                if (lineIndent > indent || !TrySplitKey(line, lineIndent, out var key, out var rest)) {
                    _index++;
                    continue;
                }

                var path = prefix + key;
                var lineNumber = _index + 1;
                _index++;

                if (rest.Length > 0) {
                    _keys.Add(new StoredKey(path, rest + Continuation(indent), lineNumber));
                    continue;
                }

                ReadNested(indent, path, lineNumber);
            }
        }

        private void ReadNested(int indent, string path, int lineNumber) {
            var next = NextContent();
            var position = _keys.Count;
            var start = _index;

            if (next >= 0 && IsSequenceItem(_lines[next], IndentOf(_lines[next]))
                          && IndentOf(_lines[next]) >= indent) {
                ReadSequence(IndentOf(_lines[next]), path);
            }
            else if (next >= 0 && IndentOf(_lines[next]) > indent && !_lines[next].StartsWith(DocumentStart)) {
                ReadMapping(IndentOf(_lines[next]), path + ".");
            }

            _keys.Insert(position, new StoredKey(path, Joined(start, _index), lineNumber));
        }

        private void ReadSequence(int indent, string path) {
            var element = 0;

            while (_index < _lines.Length) {
                var line = _lines[_index];

                if (IsBlank(line)) {
                    _index++;
                    continue;
                }

                var lineIndent = IndentOf(line);

                if (lineIndent != indent || !IsSequenceItem(line, lineIndent)) {
                    return;
                }

                var contentColumn = lineIndent + 1;

                while (contentColumn < line.Length && line[contentColumn] == ' ') {
                    contentColumn++;
                }

                var itemPath = path + "[" + element + "]";
                element++;

                if (contentColumn >= line.Length) {
                    _index++;
                    var next = NextContent();

                    if (next >= 0 && IndentOf(_lines[next]) > indent
                                  && !IsSequenceItem(_lines[next], IndentOf(_lines[next]))) {
                        ReadMapping(IndentOf(_lines[next]), itemPath + ".");
                    }

                    continue;
                }

                if (TrySplitKey(line, contentColumn, out _, out _)) {
                    _lines[_index] = new string(' ', contentColumn) + line.Substring(contentColumn);
                    ReadMapping(contentColumn, itemPath + ".");
                    continue;
                }

                _index++;
                Continuation(indent);
            }
        }

        private string Continuation(int indent) {
            var builder = new StringBuilder();

            while (true) {
                var next = NextContent();

                if (next < 0 || IndentOf(_lines[next]) <= indent || _lines[next].StartsWith(DocumentStart)) {
                    return builder.ToString();
                }

                builder.Append(' ').Append(_lines[next].Trim());
                _index = next + 1;
            }
        }

        private string Joined(int start, int end) {
            var builder = new StringBuilder();

            for (var line = start; line < end; line++) {
                if (IsBlank(_lines[line])) {
                    continue;
                }

                if (builder.Length > 0) {
                    builder.Append(' ');
                }

                builder.Append(_lines[line].Trim());
            }

            return builder.ToString();
        }

        private int NextContent() {
            for (var line = _index; line < _lines.Length; line++) {
                if (!IsBlank(_lines[line])) {
                    return line;
                }
            }

            return -1;
        }

        private static bool TrySplitKey(string line, int column, out string key, out string rest) {
            key = null;
            rest = null;
            var content = line.TrimEnd();

            if (column >= content.Length || StartsFlowOrQuote(content[column])) {
                return false;
            }

            var end = content.IndexOf(KeySeparator, column, StringComparison.Ordinal);

            if (end < 0) {
                if (content[content.Length - 1] != ':') {
                    return false;
                }

                end = content.Length - 1;
            }

            if (end == column) {
                return false;
            }

            key = content.Substring(column, end - column);
            rest = content.Substring(end + 1).Trim();
            return true;
        }

        private static bool StartsFlowOrQuote(char character) {
            return character == '{' || character == '[' || character == '"' || character == '\''
                   || character == '#';
        }

        private static bool IsSequenceItem(string line, int indent) {
            return indent < line.Length && line[indent] == '-'
                                        && (indent + 1 == line.Length || line[indent + 1] == ' ')
                                        && !line.StartsWith(DocumentStart);
        }

        private static bool IsBlank(string line) {
            return string.IsNullOrWhiteSpace(line);
        }

        private static int IndentOf(string line) {
            var indent = 0;

            while (indent < line.Length && line[indent] == ' ') {
                indent++;
            }

            return indent;
        }
    }
}
