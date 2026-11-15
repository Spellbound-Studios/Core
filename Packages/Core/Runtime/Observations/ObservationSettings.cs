// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using Spellbound.Core.Logging;
using UnityEngine;

namespace Spellbound.Core.Observations {
    [CreateAssetMenu(menuName = "Spellbound/Observation Settings", fileName = "ObservationSettings")]
    public sealed class ObservationSettings : ScriptableObject {
        public static readonly int DefaultRecorderCells = 32768;

        public List<ObservationBudgetOverride> budgets = new List<ObservationBudgetOverride>();
        public List<ObservationQualityLevelOverride> qualityLevels = new List<ObservationQualityLevelOverride>();
        public int recorderCells = DefaultRecorderCells;
        public List<ObservationRateCap> rateCaps = new List<ObservationRateCap>();
        public List<ObservationGuardPolicyOverride> guardPolicies = new List<ObservationGuardPolicyOverride>();

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

            if (guardPolicies != null) {
                foreach (var entry in guardPolicies) {
                    if (!ObservationKey.TryParse(entry.key, out var key, out var error)) {
                        Log.Error(name + ": a guard policy override is skipped. " + error);
                        continue;
                    }

                    if (entry.policy == null) {
                        Log.Error(name + ": the guard policy override of '" + key
                                  + "' names no policy; it is skipped.");
                        continue;
                    }

                    GuardPolicyValues values;

                    try {
                        values = entry.policy.Values;
                    }
                    catch (ArgumentException exception) {
                        Log.Error(name + ": the guard policy override of '" + key + "' is skipped. "
                                  + exception.Message);
                        continue;
                    }

                    context.SetGuardPolicy(key, values);
                }
            }
        }
    }
}
