// Copyright (c) Spellbound Studios. All rights reserved.

namespace Spellbound.Core.Operations {
    public sealed class OperationStatistic {
        public string Line { get; }
        public int Calls { get; internal set; }
        public int Failures { get; internal set; }
        public double TotalMilliseconds { get; internal set; }
        public double MaximumMilliseconds { get; internal set; }

        internal OperationStatistic(string line) {
            Line = line;
        }
    }
}
