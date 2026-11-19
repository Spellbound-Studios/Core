// Copyright (c) Spellbound Studios. All rights reserved.

using System.Runtime.InteropServices;

namespace Spellbound.Core.Observations {
    [StructLayout(LayoutKind.Sequential, Size = 32)]
    internal readonly struct NativeObservationEntry {
        public readonly uint Key;
        public readonly byte Kind;
        private readonly ulong _payload;
        public readonly ulong TickValue;

        internal NativeObservationEntry(uint key, ObservationKind kind, ulong payload, ulong tickValue) {
            Key = key;
            Kind = (byte)kind;
            _payload = payload;
            TickValue = tickValue;
        }

        public long Amount => (long)_payload;

        public ulong Subject => _payload;
    }
}
