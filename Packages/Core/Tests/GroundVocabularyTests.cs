// Copyright (c) Spellbound Studios. All rights reserved.

using NUnit.Framework;
using Spellbound.Core.Ground;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace Spellbound.Core.Tests {
    public class GroundVocabularyTests {
        private sealed class OriginStraddlingOccupancySource : IOccupancySource {
            public Occupancy Classify(in Bounds bounds) {
                return bounds.Contains(Vector3.zero) ? Occupancy.Mixed : Occupancy.Empty;
            }
        }

        private sealed class SpacingFillingSurfaceSource : ISurfaceSource {
            public JobHandle Schedule(in SurfaceRequest request, NativeArray<SurfaceSample> samples,
                JobHandle dependency) {
                for (var index = 0; index < samples.Length; index++) {
                    samples[index] = new SurfaceSample { Height = request.Spacing };
                }

                return dependency;
            }
        }

        [Test]
        public void SurfaceRequestIsBlittable() {
            Assert.IsTrue(UnsafeUtility.IsBlittable<SurfaceRequest>());
        }

        [Test]
        public void SurfaceSampleIsBlittable() {
            Assert.IsTrue(UnsafeUtility.IsBlittable<SurfaceSample>());
        }

        [Test]
        public void SampleCountIsWidthTimesHeight() {
            var request = new SurfaceRequest { Count = new Vector2Int(4, 6) };
            Assert.AreEqual(24, request.SampleCount);
        }

        [Test]
        public void OccupancySourceAnswersThroughTheInterface() {
            IOccupancySource source = new OriginStraddlingOccupancySource();
            var straddling = new Bounds(Vector3.zero, Vector3.one * 2f);
            var away = new Bounds(new Vector3(10f, 10f, 10f), Vector3.one);
            Assert.AreEqual(Occupancy.Mixed, source.Classify(straddling));
            Assert.AreEqual(Occupancy.Empty, source.Classify(away));
        }

        [Test]
        public void SurfaceSourceFillsEverySample() {
            ISurfaceSource source = new SpacingFillingSurfaceSource();
            var request = new SurfaceRequest { Spacing = 2.5f, Count = new Vector2Int(3, 2) };
            var samples = new NativeArray<SurfaceSample>(request.SampleCount, Allocator.Temp);
            try {
                var handle = source.Schedule(request, samples, default);
                handle.Complete();
                for (var index = 0; index < samples.Length; index++) {
                    Assert.AreEqual(2.5f, samples[index].Height);
                }
            } finally {
                samples.Dispose();
            }
        }
    }
}
