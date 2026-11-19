// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;

namespace Spellbound.Core.UnityLedger {
    public sealed class LedgerRow {
        public string Id { get; }
        public string Ours { get; }
        public string OursType { get; }
        public string UnityItem { get; }
        public LedgerVerdict Verdict { get; }
        public IReadOnlyList<LedgerJudgement> JudgedAt { get; }
        public string Reason { get; }
        public string Evidence { get; }
        public IReadOnlyList<string> WatchPackages { get; }
        public IReadOnlyList<string> WatchWords { get; }
        public string Source { get; }
        public int Line { get; }

        public LedgerRow(string id, string ours, string oursType, string unityItem, LedgerVerdict verdict,
            IReadOnlyList<LedgerJudgement> judgedAt, string reason, string evidence,
            IReadOnlyList<string> watchPackages, IReadOnlyList<string> watchWords, string source, int line) {
            if (string.IsNullOrWhiteSpace(id)) {
                throw new ArgumentException("A ledger row needs an Id.", nameof(id));
            }

            Id = id;
            Ours = ours ?? string.Empty;
            OursType = string.IsNullOrWhiteSpace(oursType) ? null : oursType;
            UnityItem = unityItem ?? string.Empty;
            Verdict = verdict;
            JudgedAt = judgedAt ?? Array.Empty<LedgerJudgement>();
            Reason = reason ?? string.Empty;
            Evidence = evidence ?? string.Empty;
            WatchPackages = watchPackages ?? Array.Empty<string>();
            WatchWords = watchWords ?? Array.Empty<string>();
            Source = source;
            Line = line;
        }

        public LedgerJudgement? EditorJudgement {
            get {
                foreach (var judgement in JudgedAt) {
                    if (judgement.IsEditor) {
                        return judgement;
                    }
                }

                return null;
            }
        }

        public bool TryJudgementFor(string package, out LedgerJudgement judgement) {
            foreach (var candidate in JudgedAt) {
                if (!candidate.IsEditor
                    && string.Equals(candidate.Package, package, StringComparison.OrdinalIgnoreCase)) {
                    judgement = candidate;
                    return true;
                }
            }

            judgement = default;
            return false;
        }

        public override string ToString() {
            return Id + " " + Ours + ": " + LedgerVerdicts.Text(Verdict);
        }
    }
}
