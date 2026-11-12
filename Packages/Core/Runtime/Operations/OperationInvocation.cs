// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;

namespace Spellbound.Core.Operations {
    public sealed class OperationInvocation {
        public uint Identifier { get; }
        public IReadOnlyList<object> Values { get; }
        public string Line { get; }
        public bool Confirmed { get; }
        public bool DryRun { get; }

        public OperationInvocation(uint identifier, object[] values, string line = null, bool confirmed = false,
            bool dryRun = false) {
            Identifier = identifier;
            Values = Array.AsReadOnly(values ?? Array.Empty<object>());
            Line = line;
            Confirmed = confirmed;
            DryRun = dryRun;
        }
    }
}
