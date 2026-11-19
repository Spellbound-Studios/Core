// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Observations {
    public sealed class Gauge {
        public ObservationDeclaration Declaration { get; }

        public ObservationKey Key => Declaration.Key;

        internal Gauge(ObservationDeclaration declaration) {
            Declaration = declaration ?? throw new ArgumentNullException(nameof(declaration));
        }

        public void Set(ObservationContext context, double value) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            context.SetGauge(Declaration, value);
        }

        public override string ToString() => Declaration.ToString();
    }
}
