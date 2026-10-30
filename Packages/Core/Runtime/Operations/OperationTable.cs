// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Spellbound.Core.Operations {
    public sealed class OperationTable {
        private readonly List<IReadOnlyList<string>> _rows = new List<IReadOnlyList<string>>();

        public IReadOnlyList<string> Columns { get; }
        public IReadOnlyList<IReadOnlyList<string>> Rows => _rows;

        public OperationTable(params string[] columns) {
            if (columns == null || columns.Length == 0) {
                throw new ArgumentException("A table needs at least one column.", nameof(columns));
            }

            Columns = Array.AsReadOnly((string[])columns.Clone());
        }

        public OperationTable AddRow(params object[] cells) {
            if (cells == null || cells.Length != Columns.Count) {
                throw new ArgumentException("A row needs one cell per column.", nameof(cells));
            }

            var row = new string[cells.Length];

            for (var i = 0; i < cells.Length; i++) {
                row[i] = CellText(cells[i]);
            }

            _rows.Add(Array.AsReadOnly(row));
            return this;
        }

        public string ToText() {
            var widths = new int[Columns.Count];

            for (var i = 0; i < Columns.Count; i++) {
                widths[i] = Columns[i].Length;
            }

            foreach (var row in _rows) {
                for (var i = 0; i < row.Count; i++) {
                    widths[i] = Math.Max(widths[i], row[i].Length);
                }
            }

            var builder = new StringBuilder();
            AppendRow(builder, Columns, widths);

            foreach (var row in _rows) {
                builder.Append('\n');
                AppendRow(builder, row, widths);
            }

            return builder.ToString();
        }

        private static void AppendRow(StringBuilder builder, IReadOnlyList<string> cells, int[] widths) {
            for (var i = 0; i < cells.Count; i++) {
                if (i > 0) {
                    builder.Append("  ");
                }

                builder.Append(cells[i]);

                if (i < cells.Count - 1) {
                    builder.Append(' ', widths[i] - cells[i].Length);
                }
            }
        }

        private static string CellText(object cell) {
            switch (cell) {
                case null:
                    return string.Empty;
                case string text:
                    return text;
                case IFormattable formattable:
                    return formattable.ToString(null, CultureInfo.InvariantCulture);
                default:
                    return cell.ToString();
            }
        }

        public override string ToString() {
            return ToText();
        }
    }
}
