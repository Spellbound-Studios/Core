// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Observations {
    [Serializable]
    public struct ObservationRateCap {
        public string key;
        public double recordsPerSecond;
    }
}
