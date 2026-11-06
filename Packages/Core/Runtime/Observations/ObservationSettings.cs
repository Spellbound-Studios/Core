// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections.Generic;
using Spellbound.Core.Logging;
using UnityEngine;

namespace Spellbound.Core.Observations {
    [CreateAssetMenu(menuName = "Spellbound/Observation Settings", fileName = "ObservationSettings")]
    public sealed class ObservationSettings : ScriptableObject {
        public List<ObservationBudgetOverride> budgets = new List<ObservationBudgetOverride>();
        public List<ObservationQualityLevelOverride> qualityLevels = new List<ObservationQualityLevelOverride>();

        internal void ApplyTo(ObservationContext context) {
            if (budgets != null) {
                foreach (var entry in budgets) {
                    if (ObservationKey.TryParse(entry.key, out var key, out var error)) {
                        context.SetBudget(key, entry.budget);
                    }
                    else {
                        Log.Error(name + ": a budget override is skipped. " + error);
                    }
                }
            }

            if (qualityLevels != null) {
                foreach (var entry in qualityLevels) {
                    if (ObservationKey.TryParse(entry.key, out var key, out var error)) {
                        context.SetQualityLevel(key, entry.level);
                    }
                    else {
                        Log.Error(name + ": a quality level override is skipped. " + error);
                    }
                }
            }
        }
    }
}
