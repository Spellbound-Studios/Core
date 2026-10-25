// Copyright (c) Spellbound Studios. All rights reserved.

using UnityEngine;

namespace Spellbound.Core.ReferenceFrames {
    internal sealed class WorldReferenceFrame : IReferenceFrame {
        public static readonly WorldReferenceFrame Instance = new WorldReferenceFrame();

        private WorldReferenceFrame() { }

        public ReferenceFrameId Id => ReferenceFrameId.World;

        public Pose WorldPose => new Pose(Vector3.zero, Quaternion.identity);

        public Vector3 AngularVelocity => Vector3.zero;

        public Vector3 VelocityAt(Vector3 worldPoint) => Vector3.zero;
    }
}
