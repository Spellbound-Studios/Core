// Copyright (c) Spellbound Studios. All rights reserved.

using UnityEngine;

namespace Spellbound.Core.ReferenceFrames {
    public readonly struct UpDirection {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        public UpDirection(float x, float y, float z) {
            X = x;
            Y = y;
            Z = z;
        }

        public static UpDirection WorldUp => new UpDirection(0f, 1f, 0f);

        public Vector3 ToVector3() {
            return new Vector3(X, Y, Z);
        }

        public static UpDirection From(Vector3 direction) {
            var normalised = direction.normalized;
            return new UpDirection(normalised.x, normalised.y, normalised.z);
        }
    }
}
