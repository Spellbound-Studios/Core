// Copyright (c) Spellbound Studios. All rights reserved.

namespace Spellbound.Core.Observations {
    public readonly struct ObservationReading {
        public readonly ObservationKey Key;
        public readonly ObservationKind Kind;
        public readonly ObservationUnit Unit;
        public readonly double Value;
        public readonly double Total;
        public readonly double Peak;
        public readonly long Count;
        public readonly double Budget;
        public readonly int QualityLevel;
        public readonly uint Version;

        public bool HasBudget => !double.IsNaN(Budget);

        public bool OverBudget => HasBudget && Value > Budget;

        internal ObservationReading(ObservationKey key, ObservationKind kind, ObservationUnit unit, double value,
            double total,
            double peak, long count, double budget, int qualityLevel, uint version) {
            Key = key;
            Kind = kind;
            Unit = unit;
            Value = value;
            Total = total;
            Peak = peak;
            Count = count;
            Budget = budget;
            QualityLevel = qualityLevel;
            Version = version;
        }

        public override string ToString() => Key + " = " + Value + " " + Unit;
    }
}
