// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Ledger {
    public static class LedgerVerdicts {
        public static bool TryParse(string text, out LedgerVerdict verdict) {
            verdict = LedgerVerdict.Watch;

            if (string.IsNullOrWhiteSpace(text)) {
                return false;
            }

            var compact = text.Trim().Replace(" ", string.Empty);

            if (string.Equals(compact, "Moved", StringComparison.OrdinalIgnoreCase)) {
                verdict = LedgerVerdict.Moved;
                return true;
            }

            if (string.Equals(compact, "BuildsOn", StringComparison.OrdinalIgnoreCase)) {
                verdict = LedgerVerdict.BuildsOn;
                return true;
            }

            if (string.Equals(compact, "Keeps", StringComparison.OrdinalIgnoreCase)) {
                verdict = LedgerVerdict.Keeps;
                return true;
            }

            if (string.Equals(compact, "Watch", StringComparison.OrdinalIgnoreCase)) {
                verdict = LedgerVerdict.Watch;
                return true;
            }

            return false;
        }

        public static string Text(LedgerVerdict verdict) {
            switch (verdict) {
                case LedgerVerdict.Moved:
                    return "Moved";
                case LedgerVerdict.BuildsOn:
                    return "Builds on";
                case LedgerVerdict.Keeps:
                    return "Keeps";
                case LedgerVerdict.Watch:
                    return "Watch";
                default:
                    throw new ArgumentOutOfRangeException(nameof(verdict), verdict, "Not a ledger verdict.");
            }
        }
    }
}
