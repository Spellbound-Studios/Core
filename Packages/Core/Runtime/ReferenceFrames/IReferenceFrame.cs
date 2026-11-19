// Copyright (c) Spellbound Studios. All rights reserved.

using UnityEngine;

namespace Spellbound.Core.ReferenceFrames {
    public interface IReferenceFrame {
        ReferenceFrameId Id { get; }
        Pose WorldPose { get; }
        Vector3 VelocityAt(Vector3 worldPoint);
        Vector3 AngularVelocity { get; }
    }
}
