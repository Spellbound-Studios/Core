// Copyright (c) Spellbound Studios. All rights reserved.

using UnityEngine;

namespace Spellbound.Core.ReferenceFrames {
    public interface IUpProvider {
        Vector3 UpAt(Vector3 worldPoint);

        UpField Field(in Bounds region);
    }
}
