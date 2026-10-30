// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using Spellbound.Core.Observations;
using Spellbound.Core.Operations;
using Spellbound.Core.Packing;
using Spellbound.Core.Simulation;
using UnityEngine;

namespace Spellbound.Core {
    public static class CoreKit {
        private static OperationHost _operations;

        public static OperationHost Operations => _operations ??= new OperationHost();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlaySession() {
            _operations = null;
        }

        public static class Packing {
            public static string Describe(Type type) => PackedLayoutDescription.Describe(type);
        }

        public static class Observations {
            public static Counter Counter(string key, string description) =>
                ObservationRegistry.Counter(key, description);

            public static Occurrence Occurrence(string key, string description) =>
                ObservationRegistry.Occurrence(key, description);

            public static Gauge Gauge(string key, string description) =>
                ObservationRegistry.Gauge(key, description, ObservationUnit.Count);

            public static Gauge Gauge(string key, string description, ObservationUnit unit) =>
                ObservationRegistry.Gauge(key, description, unit);

            public static Cost Cost(string key, string description) =>
                ObservationRegistry.Cost(key, description, ObservationUnit.Milliseconds, double.NaN, 0);

            public static Cost Cost(string key, string description, ObservationUnit unit) =>
                ObservationRegistry.Cost(key, description, unit, double.NaN, 0);

            public static Cost Cost(string key, string description, ObservationUnit unit, double defaultBudget,
                int defaultQualityLevel) =>
                ObservationRegistry.Cost(key, description, unit, defaultBudget, defaultQualityLevel);

            public static ObservationReason Reason(string name) => ObservationReasons.Register(name);

            public static string ReasonName(uint id) => ObservationReasons.Name(id);

            public static IReadOnlyList<ObservationDeclaration> Declared() => ObservationRegistry.Declared();

            public static ObservationContext CreateContext(string name) => CreateContext(name, null);

            public static ObservationContext CreateContext(string name, ObservationSettings settings) {
                if (string.IsNullOrWhiteSpace(name)) {
                    throw new ArgumentException("An observation context needs a name: the world it belongs to.",
                        nameof(name));
                }

                var trimmed = name.Trim();

                if (string.Equals(trimmed, ObservationContext.DiscardName, StringComparison.Ordinal)) {
                    throw new ArgumentException("The name '" + trimmed + "' is the discard context's; name the world.",
                        nameof(name));
                }

                if (ObservationContexts.Find(trimmed) != null) {
                    throw new ArgumentException("A live observation context is already named '" + trimmed
                                                + "'; dispose it first or name the new world differently.",
                                                nameof(name));
                }

                var context = new ObservationContext(trimmed, false);

                if (settings != null) {
                    settings.ApplyTo(context);
                }

                ObservationContexts.Add(context);
                return context;
            }

            public static int Compare(IReadOnlyList<ObservationReading> before, IReadOnlyList<ObservationReading> after,
                List<ObservationRatio> ratios) {
                if (before == null) {
                    throw new ArgumentNullException(nameof(before));
                }

                if (after == null) {
                    throw new ArgumentNullException(nameof(after));
                }

                if (ratios == null) {
                    throw new ArgumentNullException(nameof(ratios));
                }

                ratios.Clear();

                for (var i = 0; i < after.Count; i++) {
                    for (var j = 0; j < before.Count; j++) {
                        if (before[j].Key != after[i].Key) {
                            continue;
                        }

                        ratios.Add(new ObservationRatio(after[i].Key, before[j].Value, after[i].Value));
                        break;
                    }
                }

                return ratios.Count;
            }
        }

        public static class Time {
            public static TickClock StartClock(ITickSource source, ObservationContext observations) {
                var clock = new TickClock(source, observations);
                clock.Driver = TickClockDriver.Attach(clock);
                return clock;
            }

            public static TickClock StartClock(TickRate rate, ObservationContext observations) =>
                StartClock(new GameTimeTickSource(rate), observations);

            public static void StopClock(TickClock clock) {
                if (clock?.Driver == null) {
                    return;
                }

                clock.Driver.Stop();
                clock.Driver = null;
            }
        }
    }
}
