// Copyright (c) Spellbound Studios. All rights reserved.

using UnityEngine;

namespace Spellbound.Core.Ground {
    public interface IOccupancySource {
        Occupancy Classify(in Bounds bounds);
    }
}
