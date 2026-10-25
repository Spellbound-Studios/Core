// Copyright (c) Spellbound Studios. All rights reserved.

using Spellbound.Core.ReferenceFrames;
using UnityEngine;

namespace Spellbound.Core.Ground {
    public struct SurfaceRequest {
        public Vector2 Origin;
        public float Spacing;
        public Vector2Int Count;
        public ReferenceFrameId ReferenceFrame;

        public int SampleCount => Count.x * Count.y;
    }
}
