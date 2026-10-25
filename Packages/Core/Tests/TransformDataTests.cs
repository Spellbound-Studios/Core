// Copyright 2026 Spellbound Studio Inc.

using NUnit.Framework;
using UnityEngine;

namespace Spellbound.Core.Tests {
    public class TransformDataTests {
        [Test]
        public void ComposedRotationSurvivesCaptureAndRebuild() {
            var go = new GameObject();

            try {
                go.transform.rotation = Quaternion.Euler(35f, 70f, 20f);

                var data = new TransformData(go.transform);

                Assert.Less(Quaternion.Angle(go.transform.rotation, data.RotAsQuaternion()), 0.01f);
            }
            finally {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void YawOnlyRotationSurvivesCaptureAndRebuild() {
            var go = new GameObject();

            try {
                go.transform.rotation = Quaternion.Euler(0f, 125f, 0f);

                var data = new TransformData(go.transform);

                Assert.Less(Quaternion.Angle(go.transform.rotation, data.RotAsQuaternion()), 0.01f);
            }
            finally {
                Object.DestroyImmediate(go);
            }
        }
    }
}
