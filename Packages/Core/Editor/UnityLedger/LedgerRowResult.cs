// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;

namespace Spellbound.Core.UnityLedger {
    public sealed class LedgerRowResult {
        public LedgerRow Row { get; }
        public bool IsStale { get; }
        public bool IsWrong { get; }
        public IReadOnlyList<string> Lines { get; }

        public LedgerRowResult(LedgerRow row, bool isStale, bool isWrong, IReadOnlyList<string> lines) {
            Row = row ?? throw new ArgumentNullException(nameof(row));
            IsStale = isStale;
            IsWrong = isWrong;
            Lines = lines ?? Array.Empty<string>();
        }
    }
}
