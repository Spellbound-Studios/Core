// Copyright (c) Spellbound Studios. All rights reserved.

using System.Runtime.InteropServices;

namespace Spellbound.Core.Observations {
    [StructLayout(LayoutKind.Sequential)]
    internal struct RecorderCell {
        public uint Key;
        public uint TickLow;
        public ulong Subject;

        public RecorderCell(uint key, uint tickLow, ulong subject) {
            Key = key;
            TickLow = tickLow;
            Subject = subject;
        }
    }
}
