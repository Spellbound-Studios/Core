// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using Spellbound.Core.Simulation;

namespace Spellbound.Core.Observations {
    public sealed class Occurrence {
        public ObservationDeclaration Declaration { get; }

        public ObservationKey Key => Declaration.Key;

        internal Occurrence(ObservationDeclaration declaration) {
            Declaration = declaration ?? throw new ArgumentNullException(nameof(declaration));
        }

        public void Record(ObservationContext context, Tick tick, ulong subject) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            context.RecordOccurrence(Declaration);
        }

        public override string ToString() => Declaration.ToString();
    }
}
