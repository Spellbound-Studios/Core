// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Globalization;
using Spellbound.Core.Authoring;
using UnityEngine;

namespace Spellbound.Core.Intents {
    [CreateAssetMenu(menuName = "Spellbound/Intent Ledger Settings", fileName = "IntentLedgerSettings")]
    public sealed class IntentLedgerSettings : ScriptableObject, IAuthoredDefinition {
        public static readonly int DefaultWindowSize = 32;
        public static readonly double DefaultDepartureGraceSeconds = 300;

        public const string WindowSizeUnderOneCode = "core.intent-window-size-under-one";
        public const string GraceNegativeCode = "core.intent-grace-negative";
        public const string GraceNotFiniteCode = "core.intent-grace-not-finite";

        public int windowSize = DefaultWindowSize;
        public double departureGraceSeconds = DefaultDepartureGraceSeconds;

        public void Validate(ValidationReport report) {
            if (report == null) {
                throw new ArgumentNullException(nameof(report));
            }

            if (windowSize < 1) {
                report.Error(WindowSizeUnderOneCode,
                    "The window size is " + windowSize.ToString(CultureInfo.InvariantCulture) + ", under 1.",
                    nameof(windowSize), "Keep at least one intent per actor.");
            }

            var seconds = departureGraceSeconds.ToString(CultureInfo.InvariantCulture);

            if (double.IsNaN(departureGraceSeconds) || double.IsInfinity(departureGraceSeconds)) {
                report.Error(GraceNotFiniteCode, "The departure grace is " + seconds + ", not a finite number.",
                    nameof(departureGraceSeconds), "Set a finite number of seconds, zero or more.");
            }
            else if (departureGraceSeconds < 0) {
                report.Error(GraceNegativeCode, "The departure grace is " + seconds + " seconds, below zero.",
                    nameof(departureGraceSeconds), "Set zero or more seconds.");
            }
        }
    }
}
