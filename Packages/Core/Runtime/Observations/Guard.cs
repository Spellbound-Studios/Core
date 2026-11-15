// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using Spellbound.Core.Simulation;

namespace Spellbound.Core.Observations {
    public sealed class Guard {
        public ObservationDeclaration Declaration { get; }

        public ObservationKey Key => Declaration.Key;

        public GuardPolicyValues Defaults { get; }

        internal Guard(ObservationDeclaration declaration, GuardPolicyValues defaults) {
            Declaration = declaration ?? throw new ArgumentNullException(nameof(declaration));
            Defaults = defaults;
        }

        public GuardVerdict Check(ObservationContext context, bool holds, ulong subject, Tick tick) {
            throw new NotImplementedException();
        }

        public override string ToString() => Declaration.ToString();
    }
}
