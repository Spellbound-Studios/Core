// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Observations {
    public sealed class Counter {
        public ObservationDeclaration Declaration { get; }

        public ObservationKey Key => Declaration.Key;

        internal Counter(ObservationDeclaration declaration) {
            Declaration = declaration ?? throw new ArgumentNullException(nameof(declaration));
        }

        public void Add(ObservationContext context) {
            Add(context, ObservationReason.None, 1);
        }

        public void Add(ObservationContext context, ObservationReason reason) {
            Add(context, reason, 1);
        }

        public void Add(ObservationContext context, long amount) {
            Add(context, ObservationReason.None, amount);
        }

        public void Add(ObservationContext context, ObservationReason reason, long amount) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            context.AddCount(Declaration, reason, amount);
        }

        public override string ToString() => Declaration.ToString();
    }
}
