// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;

namespace Spellbound.Core.UnityLedger {
    public static class LedgerTable {
        public const char ListSeparator = ';';

        public static readonly IReadOnlyList<string> Columns = new[] {
            "Id", "Ours", "Ours (type)", "Unity item", "Verdict", "Judged at", "Reason", "Evidence", "Watch packages",
            "Watch words"
        };

        public static IReadOnlyList<LedgerRow> Parse(string markdown, string source,
            ICollection<string> problems = null) {
            var rows = new List<LedgerRow>();

            if (string.IsNullOrEmpty(markdown)) {
                return rows;
            }

            var lines = markdown.Replace("\r\n", "\n").Split('\n');
            var index = 0;

            while (index < lines.Length) {
                if (!IsHeader(lines[index])) {
                    index++;
                    continue;
                }

                index++;

                if (index < lines.Length && IsSeparator(lines[index])) {
                    index++;
                }

                while (index < lines.Length && IsTableRow(lines[index])) {
                    var row = ReadRow(lines[index], source, index + 1, problems);

                    if (row != null) {
                        rows.Add(row);
                    }

                    index++;
                }
            }

            return rows;
        }

        public static string Render(IReadOnlyList<LedgerRow> rows) {
            var builder = new StringBuilder();
            AppendCells(builder, Columns);
            builder.Append('\n').Append('|');

            for (var i = 0; i < Columns.Count; i++) {
                builder.Append("---|");
            }

            if (rows == null) {
                return builder.ToString();
            }

            foreach (var row in rows) {
                builder.Append('\n');
                AppendCells(builder, new[] {
                    row.Id, row.Ours, row.OursType ?? string.Empty, row.UnityItem, LedgerVerdicts.Text(row.Verdict),
                    JoinList(row.JudgedAt), row.Reason, row.Evidence, JoinList(row.WatchPackages),
                    JoinList(row.WatchWords)
                });
            }

            return builder.ToString();
        }

        internal static bool IsTableRow(string line) {
            if (line == null) {
                return false;
            }

            var trimmed = line.Trim();
            return trimmed.Length >= 2 && trimmed[0] == '|' && trimmed[trimmed.Length - 1] == '|';
        }

        internal static bool IsHeader(string line) {
            if (!IsTableRow(line)) {
                return false;
            }

            var cells = Cells(line);

            if (cells.Count != Columns.Count) {
                return false;
            }

            for (var i = 0; i < cells.Count; i++) {
                if (!string.Equals(cells[i], Columns[i], StringComparison.Ordinal)) {
                    return false;
                }
            }

            return true;
        }

        internal static bool IsSeparator(string line) {
            if (!IsTableRow(line)) {
                return false;
            }

            foreach (var cell in Cells(line)) {
                if (cell.Length == 0 || cell.IndexOf('-') < 0) {
                    return false;
                }

                foreach (var character in cell) {
                    if (character != '-' && character != ':') {
                        return false;
                    }
                }
            }

            return true;
        }

        private static List<string> Cells(string line) {
            var trimmed = line.Trim();
            var inner = trimmed.Substring(1, trimmed.Length - 2);
            var cells = new List<string>();

            foreach (var cell in inner.Split('|')) {
                cells.Add(cell.Trim());
            }

            return cells;
        }

        private static LedgerRow ReadRow(string line, string source, int lineNumber, ICollection<string> problems) {
            var cells = Cells(line);

            if (cells.Count != Columns.Count) {
                Report(problems, source, lineNumber,
                    "the row has " + cells.Count + " cells; a ledger row has " + Columns.Count);
                return null;
            }

            if (cells[0].Length == 0) {
                Report(problems, source, lineNumber, "the row has an empty Id");
                return null;
            }

            if (!LedgerVerdicts.TryParse(cells[4], out var verdict)) {
                Report(problems, source, lineNumber,
                    "unknown verdict '" + cells[4] + "'; a verdict is Moved, Builds on, Keeps or Watch");
                return null;
            }

            var judgedAt = new List<LedgerJudgement>();

            foreach (var entry in SplitList(cells[5])) {
                if (!LedgerJudgement.TryParse(entry, out var judgement)) {
                    Report(problems, source, lineNumber,
                        "Judged at entry '" + entry + "' is neither an editor version nor a package and its version");
                    return null;
                }

                judgedAt.Add(judgement);
            }

            var oursType = cells[2].Trim('`').Trim();
            return new LedgerRow(cells[0], cells[1], oursType.Length == 0 ? null : oursType, cells[3], verdict,
                judgedAt, cells[6], cells[7], SplitList(cells[8]), SplitList(cells[9]), source, lineNumber);
        }

        private static List<string> SplitList(string cell) {
            var entries = new List<string>();

            foreach (var entry in cell.Split(ListSeparator)) {
                var trimmed = entry.Trim();

                if (trimmed.Length > 0) {
                    entries.Add(trimmed);
                }
            }

            return entries;
        }

        private static void Report(ICollection<string> problems, string source, int lineNumber, string what) {
            problems?.Add(source + ":" + lineNumber + ": " + what);
        }

        private static void AppendCells(StringBuilder builder, IReadOnlyList<string> cells) {
            builder.Append('|');

            foreach (var cell in cells) {
                builder.Append(' ').Append(cell).Append(" |");
            }
        }

        private static string JoinList<T>(IReadOnlyList<T> entries) {
            var builder = new StringBuilder();

            for (var i = 0; i < entries.Count; i++) {
                if (i > 0) {
                    builder.Append(ListSeparator).Append(' ');
                }

                builder.Append(entries[i]);
            }

            return builder.ToString();
        }
    }
}
