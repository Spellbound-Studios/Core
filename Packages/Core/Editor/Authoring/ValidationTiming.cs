// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Authoring.Editor {
    public sealed class ValidationTiming {
        public string Check { get; }
        public string Asset { get; }
        public double Milliseconds { get; }
        public DateTime RanAt { get; }
        public bool Carried { get; }
        public bool Stale { get; }
        public bool Demoted { get; }
        public double DemotedMilliseconds { get; }
        public int BudgetMilliseconds { get; }

        public ValidationTiming(string check, string asset, double milliseconds) : this(check, asset, milliseconds,
            default, false, false, false, 0, 0) { }

        public ValidationTiming(string check, string asset, double milliseconds, DateTime ranAt) : this(check, asset,
            milliseconds, ranAt, false, false, false, 0, 0) { }

        private ValidationTiming(string check, string asset, double milliseconds, DateTime ranAt, bool carried,
            bool stale, bool demoted, double demotedMilliseconds, int budgetMilliseconds) {
            Check = check;
            Asset = asset;
            Milliseconds = milliseconds;
            RanAt = ranAt;
            Carried = carried;
            Stale = stale;
            Demoted = demoted;
            DemotedMilliseconds = demotedMilliseconds;
            BudgetMilliseconds = budgetMilliseconds;
        }

        internal ValidationTiming CarriedForward(bool stale) {
            return new ValidationTiming(Check, Asset, Milliseconds, RanAt, true, Stale || stale, Demoted,
                DemotedMilliseconds, BudgetMilliseconds);
        }

        internal ValidationTiming DemotedOver(double demotedMilliseconds, int budgetMilliseconds) {
            return new ValidationTiming(Check, Asset, Milliseconds, RanAt, Carried, Stale, true, demotedMilliseconds,
                budgetMilliseconds);
        }
    }
}
