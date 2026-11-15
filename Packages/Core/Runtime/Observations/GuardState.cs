// Copyright (c) Spellbound Studios. All rights reserved.

using System.Runtime.InteropServices;
using Spellbound.Core.Simulation;

namespace Spellbound.Core.Observations {
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    internal struct GuardState {
        [FieldOffset(0)] public uint GuardHash;
        [FieldOffset(4)] public int RepairCount;
        [FieldOffset(8)] public ulong Subject;
        [FieldOffset(16)] public Tick WindowStart;
        [FieldOffset(24)] public Tick HoldUntil;
    }
}
