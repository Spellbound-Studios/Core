// Copyright (c) Spellbound Studios. All rights reserved.

namespace Spellbound.Core.Operations {
    public sealed class OperationHistoryEntry {
        public string Line { get; }
        public OperationStatus Status { get; }
        public string Caller { get; }
        public int Frame { get; }

        internal OperationHistoryEntry(string line, OperationStatus status, string caller, int frame) {
            Line = line;
            Status = status;
            Caller = caller;
            Frame = frame;
        }
    }
}
