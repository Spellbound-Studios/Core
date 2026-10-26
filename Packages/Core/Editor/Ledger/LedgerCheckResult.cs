// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;

namespace Spellbound.Core.Ledger.Editor {
    public sealed class LedgerCheckResult {
        public IReadOnlyList<LedgerRowResult> Rows { get; }
        public IReadOnlyList<string> Problems { get; }
        public int StaleCount { get; }
        public int WrongCount { get; }
        public int SourceCount { get; }

        public bool Passed => StaleCount == 0 && WrongCount == 0 && Problems.Count == 0;

        public LedgerCheckResult(IReadOnlyList<LedgerRowResult> rows, IReadOnlyList<string> problems, int sourceCount) {
            Rows = rows ?? Array.Empty<LedgerRowResult>();
            Problems = problems ?? Array.Empty<string>();
            SourceCount = sourceCount;

            foreach (var row in Rows) {
                if (row.IsStale) {
                    StaleCount++;
                }

                if (row.IsWrong) {
                    WrongCount++;
                }
            }
        }

        public string Summary() {
            return StaleCount + " rows stale, " + WrongCount + " rows wrong over " + Rows.Count + " rows from "
                   + SourceCount + " sources";
        }

        public string ToText() {
            var builder = new StringBuilder(Summary());

            foreach (var result in Rows) {
                if (!result.IsStale && !result.IsWrong) {
                    continue;
                }

                builder.Append('\n').Append(result.Row.Id).Append(' ').Append(result.Row.Ours).Append(" [")
                    .Append(LedgerVerdicts.Text(result.Row.Verdict)).Append("] ").Append(State(result));

                foreach (var line in result.Lines) {
                    builder.Append("\n  ").Append(line);
                }
            }

            if (Problems.Count > 0) {
                builder.Append("\nproblems:");

                foreach (var problem in Problems) {
                    builder.Append("\n  ").Append(problem);
                }
            }

            return builder.ToString();
        }

        public string MoveFormLines() {
            var builder = new StringBuilder();

            foreach (var result in Rows) {
                if (!result.IsStale && !result.IsWrong) {
                    continue;
                }

                if (builder.Length > 0) {
                    builder.Append('\n');
                }

                builder.Append("- ").Append(result.Row.Id).Append(' ').Append(result.Row.Ours).Append(": ")
                    .Append(result.Lines.Count > 0 ? result.Lines[0].Trim() : State(result));
            }

            return builder.ToString();
        }

        private static string State(LedgerRowResult result) {
            if (result.IsStale && result.IsWrong) {
                return "stale, wrong";
            }

            return result.IsStale ? "stale" : "wrong";
        }
    }
}
