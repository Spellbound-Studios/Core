// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Intents {
    public sealed class IntentSequence {
        public IntentSequence(IntentActor actor) : this(actor, 1) { }

        public IntentSequence(IntentActor actor, uint nextSequence) {
            if (actor == IntentActor.None) {
                throw new ArgumentException("An intent sequence needs an actor; IntentActor.None is no one.",
                    nameof(actor));
            }

            if (nextSequence == 0) {
                throw new ArgumentOutOfRangeException(nameof(nextSequence), nextSequence,
                    "Sequence zero is never issued; the first sequence is at least 1.");
            }

            Actor = actor;
            NextSequence = nextSequence;
        }

        public IntentActor Actor { get; }

        public uint NextSequence { get; private set; }

        public IntentKey Next() {
            if (NextSequence == uint.MaxValue) {
                throw new InvalidOperationException("The intent sequence of actor " + Actor + " reached "
                                                    + uint.MaxValue + " and does not wrap.");
            }

            var key = new IntentKey(Actor, NextSequence);
            NextSequence++;
            return key;
        }
    }
}
