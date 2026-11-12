// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Observations {
    public sealed class Cost {
        public ObservationDeclaration Declaration { get; }

        public ObservationKey Key => Declaration.Key;

        internal Cost(ObservationDeclaration declaration) {
            Declaration = declaration ?? throw new ArgumentNullException(nameof(declaration));
        }

        public void Record(ObservationContext context, double amount) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            context.RecordCost(Declaration, amount);
        }

        public CostScope Measure(ObservationContext context) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            return new CostScope(this, context);
        }

        public override string ToString() => Declaration.ToString();
    }
}
