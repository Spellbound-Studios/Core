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

            context.RecordOccurrence(Declaration, tick, subject, ReadOnlySpan<uint>.Empty);
        }

        public void Record(ObservationContext context, Tick tick, ulong subject, ulong value) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.IsDiscard) {
                return;
            }

            Span<uint> values = stackalloc uint[2];
            values[0] = (uint)value;
            values[1] = (uint)(value >> 32);
            context.RecordOccurrence(Declaration, tick, subject, values);
        }

        public void Record(ObservationContext context, Tick tick, ulong subject, ReadOnlySpan<uint> values) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            if (values.Length > RecordedOccurrence.MaximumValues) {
                throw new ArgumentException(
                    "An occurrence records at most " + RecordedOccurrence.MaximumValues + " values; "
                    + values.Length + " were given.", nameof(values));
            }

            context.RecordOccurrence(Declaration, tick, subject, values);
        }

        public override string ToString() => Declaration.ToString();
    }
}
