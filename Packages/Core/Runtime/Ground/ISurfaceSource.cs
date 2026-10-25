// Copyright (c) Spellbound Studios. All rights reserved.

using Unity.Collections;
using Unity.Jobs;

namespace Spellbound.Core.Ground {
    public interface ISurfaceSource {
        JobHandle Schedule(in SurfaceRequest request, NativeArray<SurfaceSample> samples, JobHandle dependency);
    }
}
