// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using Spellbound.Core.Simulation;

namespace Spellbound.Core.Intents {
    internal sealed class IntentWindow {
        internal IntentWindow(int size) {
            Sequences = new uint[size];
            Receipts = new IntentReceipt[size];
        }

        internal uint[] Sequences { get; }

        internal IntentReceipt[] Receipts { get; }

        internal IntentActor Actor { get; private set; }

        internal uint Highest { get; set; }

        internal Tick DropAt { get; set; }

        internal int DepartedIndex { get; set; } = -1;

        internal bool IsDeparted => DepartedIndex >= 0;

        internal void Reset(IntentActor actor) {
            Array.Clear(Sequences, 0, Sequences.Length);
            Array.Clear(Receipts, 0, Receipts.Length);
            Actor = actor;
            Highest = 0;
            DropAt = Tick.Zero;
            DepartedIndex = -1;
        }
    }
}
