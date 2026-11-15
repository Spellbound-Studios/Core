// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Observations {
    [Serializable]
    public struct ObservationGuardPolicyOverride {
        public string key;
        public GuardPolicy policy;
    }
}
