// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.UnityLedger {
    public readonly struct LedgerJudgement {
        private static readonly char[] Whitespace = { ' ', '\t' };

        public readonly string Package;
        public readonly string Version;

        public bool IsEditor => Package == null;

        public LedgerJudgement(string package, string version) {
            if (string.IsNullOrWhiteSpace(version)) {
                throw new ArgumentException("A judgement needs a version.", nameof(version));
            }

            Package = string.IsNullOrWhiteSpace(package) ? null : package.Trim();
            Version = version.Trim();
        }

        public static bool TryParse(string entry, out LedgerJudgement judgement) {
            judgement = default;

            if (string.IsNullOrWhiteSpace(entry)) {
                return false;
            }

            var tokens = entry.Trim().Split(Whitespace, StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length == 1 && char.IsDigit(tokens[0][0])) {
                judgement = new LedgerJudgement(null, tokens[0]);
                return true;
            }

            if (tokens.Length == 2) {
                judgement = new LedgerJudgement(tokens[0], tokens[1]);
                return true;
            }

            return false;
        }

        public override string ToString() {
            return IsEditor ? Version ?? string.Empty : Package + " " + Version;
        }
    }
}
