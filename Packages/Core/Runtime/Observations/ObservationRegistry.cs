// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using Spellbound.Core.Logging;
using Spellbound.Core.Registries;
using UnityEngine;

namespace Spellbound.Core.Observations {
    internal static class ObservationRegistry {
        private static readonly HashRegistry<ObservationDeclaration> Declarations =
            new HashRegistry<ObservationDeclaration>();

        internal static Counter Counter(string key, string description) {
            var declaration = Declare(key, ObservationKind.Counter, description, ObservationUnit.Count, double.NaN, 0);
            return new Counter(declaration);
        }

        internal static Occurrence Occurrence(string key, string description) {
            var declaration = Declare(key, ObservationKind.Occurrence, description, ObservationUnit.Count, double.NaN,
                0);
            return new Occurrence(declaration);
        }

        internal static Gauge Gauge(string key, string description, ObservationUnit unit) {
            var declaration = Declare(key, ObservationKind.Gauge, description, unit, double.NaN, 0);
            return new Gauge(declaration);
        }

        internal static Cost Cost(string key, string description, ObservationUnit unit, double defaultBudget,
            int defaultQualityLevel) {
            var declaration = Declare(key, ObservationKind.Cost, description, unit, defaultBudget, defaultQualityLevel);
            return new Cost(declaration);
        }

        internal static ObservationDeclaration Find(ObservationKey key) {
            return Declarations.TryGet(key.Hash, out var declaration) ? declaration : null;
        }

        internal static void Ensure(ObservationDeclaration declaration) {
            if (Declarations.TryGet(declaration.Key.Hash, out var existing)) {
                if (!existing.Matches(declaration)) {
                    Log.Error("The observation key '" + declaration.Key + "' is declared twice in different ways: "
                              + existing
                              + " \"" + existing.Description + "\" and " + declaration + " \"" + declaration.Description
                              + "\"; the first is kept.");
                }

                return;
            }

            Declarations.Add(declaration);
        }

        internal static IReadOnlyList<ObservationDeclaration> Declared() {
            ObservationDeclarationScan.Run();
            var declared = new List<ObservationDeclaration>(Declarations.All);
            declared.Sort((left, right) => string.CompareOrdinal(left.Key.Name, right.Key.Name));
            return declared;
        }

        private static ObservationDeclaration Declare(string key, ObservationKind kind, string description,
            ObservationUnit unit,
            double defaultBudget, int defaultQualityLevel) {
            var parsed = ObservationKey.Parse(key);
            var declaration = new ObservationDeclaration(parsed, kind, description, unit, defaultBudget,
                defaultQualityLevel);

            if (Declarations.TryGet(parsed.Hash, out var existing)) {
                if (existing.Matches(declaration)) {
                    return existing;
                }

                throw new ArgumentException("The observation key '" + key + "' is already declared as " + existing
                                            + " with the description \"" + existing.Description
                                            + "\"; a key is declared once, by one package.", nameof(key));
            }

            Declarations.Add(declaration);
            return declaration;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlaySession() {
            Declarations.Clear();
            ObservationDeclarationScan.Reset();
        }
    }
}
