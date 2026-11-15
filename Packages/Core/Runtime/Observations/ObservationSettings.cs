// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using Spellbound.Core.Authoring;
using Spellbound.Core.Logging;
using UnityEngine;

namespace Spellbound.Core.Observations {
    [CreateAssetMenu(menuName = "Spellbound/Observation Settings", fileName = "ObservationSettings")]
    public sealed class ObservationSettings : ScriptableObject, IAuthoredDefinition {
        public static readonly int DefaultRecorderCells = 32768;

        public const string KeyUnparsedCode = "core.observation-key-unparsed";
        public const string RecorderCellsUnderOneCode = "core.recorder-cells-under-one";
        public const string RateCapNegativeCode = "core.rate-cap-negative";

        public List<ObservationBudgetOverride> budgets = new List<ObservationBudgetOverride>();
        public List<ObservationQualityLevelOverride> qualityLevels = new List<ObservationQualityLevelOverride>();
        public int recorderCells = DefaultRecorderCells;
        public List<ObservationRateCap> rateCaps = new List<ObservationRateCap>();
        public List<ObservationGuardPolicyOverride> guardPolicies = new List<ObservationGuardPolicyOverride>();

        public void Validate(ValidationReport report) {
            if (report == null) {
                throw new ArgumentNullException(nameof(report));
            }

            if (budgets != null) {
                foreach (var entry in budgets) {
                    RequireKey(report, entry.key, nameof(budgets));
                }
            }

            if (qualityLevels != null) {
                foreach (var entry in qualityLevels) {
                    RequireKey(report, entry.key, nameof(qualityLevels));
                }
            }

            if (rateCaps != null) {
                foreach (var entry in rateCaps) {
                    RequireKey(report, entry.key, nameof(rateCaps));
                }
            }

            if (guardPolicies != null) {
                foreach (var entry in guardPolicies) {
                    RequireKey(report, entry.key, nameof(guardPolicies));
                }
            }

            if (recorderCells < 1) {
                report.Error(RecorderCellsUnderOneCode,
                    "The flight recorder has " + recorderCells.ToString(CultureInfo.InvariantCulture)
                    + " cells, under 1.",
                    nameof(recorderCells),
                    "Keep at least one cell; the default is "
                    + DefaultRecorderCells.ToString(CultureInfo.InvariantCulture) + ".");
            }

            if (rateCaps != null) {
                foreach (var entry in rateCaps) {
                    var rate = entry.recordsPerSecond;

                    if (double.IsNaN(rate) || double.IsInfinity(rate) || rate < 0) {
                        report.Error(RateCapNegativeCode,
                            "The rate cap of '" + entry.key + "' is " + rate.ToString(CultureInfo.InvariantCulture)
                            + " records a second, not a finite number zero or more.",
                            nameof(rateCaps), "Set a finite rate of zero or more records a second.");
                    }
                }
            }
        }

        private static void RequireKey(ValidationReport report, string text, string list) {
            if (ObservationKey.TryParse(text, out _, out var error)) {
                return;
            }

            report.Error(KeyUnparsedCode, error, list,
                "Rename '" + text + "' as package.subject.name in lower-case letters, digits and underscores.");
        }

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

            if (recorderCells < 1) {
                Log.Error(name + ": recorderCells is " + recorderCells
                          + "; a flight recorder needs at least one cell, so the default of "
                          + DefaultRecorderCells + " is kept.");
            }
            else {
                context.SetRecorderCells(recorderCells);
            }

            if (rateCaps != null) {
                foreach (var entry in rateCaps) {
                    if (!ObservationKey.TryParse(entry.key, out var key, out var error)) {
                        Log.Error(name + ": a rate cap is skipped. " + error);
                        continue;
                    }

                    if (double.IsNaN(entry.recordsPerSecond) || double.IsInfinity(entry.recordsPerSecond)
                        || entry.recordsPerSecond < 0) {
                        Log.Error(name + ": the rate cap of '" + key + "' is " + entry.recordsPerSecond
                                  + " records a second; a cap is finite and zero or more, so it is skipped.");
                        continue;
                    }

                    context.SetRateCap(key, entry.recordsPerSecond);
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
