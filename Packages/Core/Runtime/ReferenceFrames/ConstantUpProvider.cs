// Copyright (c) Spellbound Studios. All rights reserved.

using UnityEngine;

namespace Spellbound.Core.ReferenceFrames {
    public sealed class ConstantUpProvider : IUpProvider {
        private readonly UpField _field;

        public ConstantUpProvider() {
            _field = UpField.WorldUp;
        }

        public ConstantUpProvider(Vector3 up) {
            _field = UpField.Constant(up.x, up.y, up.z);
        }

        public Vector3 UpAt(Vector3 worldPoint) {
            Field(default).Evaluate(worldPoint.x, worldPoint.y, worldPoint.z, out var upX, out var upY, out var upZ);
            return new Vector3(upX, upY, upZ);
        }

        public UpField Field(in Bounds region) {
            return _field;
        }
    }
}
