// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using Spellbound.Core.Simulation;

namespace Spellbound.Core.Observations {
    public sealed class Guard {
        private static readonly Dictionary<uint, Guard> Declared = new Dictionary<uint, Guard>();
        private static readonly ObservationReason RepairReason = ObservationReasons.Register("repair");
        private static readonly ObservationReason HoldReason = ObservationReasons.Register("hold");

        public ObservationDeclaration Declaration { get; }

        public ObservationKey Key => Declaration.Key;

        public GuardPolicyValues Defaults { get; }

        internal Guard(ObservationDeclaration declaration, GuardPolicyValues defaults) {
            Declaration = declaration ?? throw new ArgumentNullException(nameof(declaration));
            Defaults = defaults;

            lock (Declared) {
                if (!Declared.ContainsKey(declaration.Key.Hash)) {
                    Declared.Add(declaration.Key.Hash, this);
                }
            }
        }

        internal static Guard Find(ObservationKey key) {
            lock (Declared) {
                return Declared.TryGetValue(key.Hash, out var guard) ? guard : null;
            }
        }

        public GuardVerdict Check(ObservationContext context, bool holds, ulong subject, Tick tick) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.IsDiscard) {
                return GuardVerdict.Holds;
            }

            var hash = Declaration.Key.Hash;
            var ticks = context.GuardTicks(this);
            var exists = context.TryGetGuardState(hash, subject, out var state);

            if (holds) {
                if (exists && tick >= state.HoldUntil && tick >= state.WindowStart.Plus(ticks.Window)) {
                    context.RemoveGuardState(hash, subject);
                }

                return GuardVerdict.Holds;
            }

            if (!exists) {
                state = new GuardState {
                    GuardHash = hash,
                    Subject = subject,
                    WindowStart = tick,
                    RepairCount = 0,
                    HoldUntil = tick
                };
            }
            else if (tick < state.HoldUntil) {
                context.AddCount(Declaration, HoldReason, 1);
                context.RecordOccurrence(Declaration);
                return GuardVerdict.Hold;
            }

            if (tick >= state.WindowStart.Plus(ticks.Window) || state.RepairCount >= ticks.Repairs) {
                state.WindowStart = tick;
                state.RepairCount = 0;
            }

            state.RepairCount++;
            context.AddCount(Declaration, RepairReason, 1);
            context.RecordOccurrence(Declaration);

            if (state.RepairCount >= ticks.Repairs) {
                state.HoldUntil = tick.Plus(ticks.Hold);
                context.SetGuardState(state);
                return GuardVerdict.Hold;
            }

            context.SetGuardState(state);
            return GuardVerdict.Repair;
        }

        public override string ToString() => Declaration.ToString();
    }
}