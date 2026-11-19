// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Globalization;
using Spellbound.Core.Authoring;
using UnityEngine;

namespace Spellbound.Core.Observations {
    [CreateAssetMenu(menuName = "Spellbound/Guard Policy", fileName = "GuardPolicy")]
    public sealed class GuardPolicy : ScriptableObject, IAuthoredDefinition {
        public const string RepairsUnderOneCode = "core.guard-repairs-under-one";
        public const string WindowInvalidCode = "core.guard-window-invalid";
        public const string HoldInvalidCode = "core.guard-hold-invalid";

        public int repairs = GuardPolicyValues.Default.Repairs;
        public double windowSeconds = GuardPolicyValues.Default.WindowSeconds;
        public double holdSeconds = GuardPolicyValues.Default.HoldSeconds;

        public GuardPolicyValues Values => new GuardPolicyValues(repairs, windowSeconds, holdSeconds);

        public void Validate(ValidationReport report) {
            if (report == null) {
                throw new ArgumentNullException(nameof(report));
            }

            if (repairs < 1) {
                report.Error(RepairsUnderOneCode,
                    "The policy allows " + repairs.ToString(CultureInfo.InvariantCulture) + " repairs, under 1.",
                    nameof(repairs), "Allow at least one repair.");
            }

            if (!IsTime(windowSeconds)) {
                report.Error(WindowInvalidCode,
                    "The window is " + windowSeconds.ToString(CultureInfo.InvariantCulture)
                    + " seconds, not a finite number zero or more.",
                    nameof(windowSeconds), "Set a finite window of zero or more seconds.");
            }

            if (!IsTime(holdSeconds)) {
                report.Error(HoldInvalidCode,
                    "The hold is " + holdSeconds.ToString(CultureInfo.InvariantCulture)
                    + " seconds, not a finite number zero or more.",
                    nameof(holdSeconds), "Set a finite hold of zero or more seconds.");
            }
        }

        private static bool IsTime(double seconds) {
            return !double.IsNaN(seconds) && !double.IsInfinity(seconds) && seconds >= 0;
        }
    }
}
