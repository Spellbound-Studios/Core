// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections.Generic;

namespace Spellbound.Core.Observations {
    internal sealed class ObservationSlot {
        private ObservationDeclaration _declaration;
        private Dictionary<uint, long> _reasons;

        internal ObservationSlot(ObservationKey key, ObservationDeclaration declaration) {
            Key = key;
            _declaration = declaration;
        }

        internal ObservationKey Key { get; }
        internal bool HasReading { get; set; }
        internal double Value { get; set; }
        internal double Total { get; set; }
        internal double Peak { get; set; }
        internal long Count { get; set; }
        internal bool HasBudgetOverride { get; set; }
        internal double BudgetOverride { get; set; }
        internal bool HasQualityLevelOverride { get; set; }
        internal int QualityLevelOverride { get; set; }
        internal uint Version { get; set; }

        internal ObservationDeclaration Declaration {
            get => _declaration ??= ObservationRegistry.Find(Key);
            set => _declaration ??= value;
        }

        internal double Budget {
            get {
                if (HasBudgetOverride) {
                    return BudgetOverride;
                }

                var declaration = Declaration;
                return declaration != null ? declaration.DefaultBudget : double.NaN;
            }
        }

        internal int QualityLevel {
            get {
                if (HasQualityLevelOverride) {
                    return QualityLevelOverride;
                }

                return Declaration?.DefaultQualityLevel ?? 0;
            }
        }

        internal void AddReason(ObservationReason reason, long amount) {
            _reasons ??= new Dictionary<uint, long>();
            _reasons.TryGetValue(reason.Id, out var total);
            _reasons[reason.Id] = total + amount;
        }

        internal void ReadReasons(List<ReasonCount> reasons) {
            if (_reasons == null) {
                return;
            }

            foreach (var pair in _reasons) {
                var reason = new ObservationReason(pair.Key, ObservationReasons.Name(pair.Key));
                reasons.Add(new ReasonCount(reason, pair.Value));
            }
        }

        internal ObservationReading Reading() {
            var declaration = Declaration;
            var kind = declaration?.Kind ?? ObservationKind.Counter;
            var unit = declaration?.Unit ?? ObservationUnit.Count;
            return new ObservationReading(Key, kind, unit, Value, Total, Peak, Count, Budget, QualityLevel, Version);
        }
    }
}
