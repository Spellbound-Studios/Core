// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using Spellbound.Core.Registries;

namespace Spellbound.Core.Observations {
    public sealed class ObservationDeclaration : IRegistryEntry {
        public ObservationKey Key { get; }
        public ObservationKind Kind { get; }
        public string Description { get; }
        public ObservationUnit Unit { get; }
        public double DefaultBudget { get; }
        public int DefaultQualityLevel { get; }

        public bool HasDefaultBudget => !double.IsNaN(DefaultBudget);

        public uint Hash => Key.Hash;

        internal ObservationDeclaration(ObservationKey key, ObservationKind kind, string description,
            ObservationUnit unit,
            double defaultBudget, int defaultQualityLevel) {
            if (key.IsNone) {
                throw new ArgumentException("An observation declaration needs a key.", nameof(key));
            }

            if (string.IsNullOrWhiteSpace(description)) {
                throw new ArgumentException(
                    "The observation '" + key + "' needs a description: it is what core observations prints"
                    + " and what a person reads.",
                    nameof(description));
            }

            Key = key;
            Kind = kind;
            Description = description.Trim();
            Unit = unit;
            DefaultBudget = defaultBudget;
            DefaultQualityLevel = defaultQualityLevel;
        }

        internal bool Matches(ObservationDeclaration other) {
            return other != null
                   && Key == other.Key
                   && Kind == other.Kind
                   && string.Equals(Description, other.Description, StringComparison.Ordinal)
                   && Unit == other.Unit
                   && DefaultBudget.Equals(other.DefaultBudget)
                   && DefaultQualityLevel == other.DefaultQualityLevel;
        }

        public override string ToString() => Key + " (" + Kind.ToString().ToLowerInvariant() + ", " + Unit + ")";
    }
}
