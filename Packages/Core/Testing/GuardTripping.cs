// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Globalization;
using Spellbound.Core.Observations;
using Spellbound.Core.Simulation;

namespace Spellbound.Core.Testing {
    public static class GuardTripping {
        private const int MaximumChecks = 65536;

        public static int Trip(Guard guard, ObservationContext context, ulong subject, Tick from) {
            if (guard == null) {
                throw new ArgumentNullException(nameof(guard));
            }

            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            var tick = from;

            for (var checks = 1; checks <= MaximumChecks; checks++) {
                if (guard.Check(context, false, subject, tick) == GuardVerdict.Hold) {
                    return checks;
                }

                tick = tick.Plus(new TickSpan(1));
            }

            throw new InvalidOperationException("The guard '" + guard.Key + "' did not hold after "
                                                + MaximumChecks.ToString(CultureInfo.InvariantCulture)
                                                + " failing checks in the context '" + context
                                                + "'; its policy never holds.");
        }

        public static int TripAll(string package, ObservationContext context) {
            if (string.IsNullOrEmpty(package)) {
                throw new ArgumentException("Name the package whose guards are tripped.", nameof(package));
            }

            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            var tripped = 0;

            foreach (var declaration in CoreKit.Observations.Declared()) {
                if (!declaration.IsGuard
                    || !string.Equals(declaration.Key.Package, package, StringComparison.Ordinal)) {
                    continue;
                }

                var guard = CoreKit.Observations.FindGuard(declaration.Key);

                if (guard == null) {
                    continue;
                }

                Trip(guard, context, 0, Tick.Zero);
                tripped++;
            }

            return tripped;
        }
    }
}
