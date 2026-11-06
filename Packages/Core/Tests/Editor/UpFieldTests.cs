// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Diagnostics;
using NUnit.Framework;
using Spellbound.Core.Logging;
using Spellbound.Core.ReferenceFrames;
#if SPELLBOUND_CORE_TESTS_BURST
using Unity.Burst;
#endif
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace Spellbound.Core.Tests {
    public class UpFieldTests {
        private const int LatticeSide = 17;
        private const int LatticeCellCount = LatticeSide * LatticeSide * LatticeSide;
        private const float LatticeSpacing = 0.75f;
        private const float LatticeOrigin = -6f;
        private const float Tolerance = 1e-5f;

#if SPELLBOUND_CORE_TESTS_BURST
        [BurstCompile(CompileSynchronously = true)]
        private struct UpFieldEvaluationJob : IJob {
            [NativeDisableContainerSafetyRestriction]
            public UpField Field;

            public NativeArray<UpDirection> Results;

            public void Execute() {
                for (var z = 0; z < LatticeSide; z++) {
                    for (var y = 0; y < LatticeSide; y++) {
                        for (var x = 0; x < LatticeSide; x++) {
                            Field.Evaluate(LatticeOrigin + x * LatticeSpacing, LatticeOrigin + y * LatticeSpacing,
                                LatticeOrigin + z * LatticeSpacing, out var upX, out var upY, out var upZ);
                            Results[x + LatticeSide * (y + LatticeSide * z)] = new UpDirection(upX, upY, upZ);
                        }
                    }
                }
            }
        }

#endif

        private sealed class PlanetUpProvider : IUpProvider {
            private readonly Vector3 _centre;

            public PlanetUpProvider(Vector3 centre) {
                _centre = centre;
            }

            public Vector3 UpAt(Vector3 worldPoint) {
                Field(default).Evaluate(worldPoint.x, worldPoint.y, worldPoint.z, out var upX, out var upY,
                    out var upZ);
                return new Vector3(upX, upY, upZ);
            }

            public UpField Field(in Bounds region) {
                return UpField.RadialFrom(_centre.x, _centre.y, _centre.z);
            }
        }

        private static void AssertUp(in UpField field, Vector3 point, Vector3 expected) {
            field.Evaluate(point.x, point.y, point.z, out var upX, out var upY, out var upZ);
            Assert.AreEqual(expected.x, upX, Tolerance);
            Assert.AreEqual(expected.y, upY, Tolerance);
            Assert.AreEqual(expected.z, upZ, Tolerance);
        }

        private static NativeArray<UpDirection> CornerDirections(Allocator allocator) {
            var directions = new NativeArray<UpDirection>(8, allocator);
            for (var index = 0; index < directions.Length; index++) {
                var x = (index & 1) == 0 ? -1f : 1f;
                var y = (index & 2) == 0 ? -1f : 1f;
                var z = (index & 4) == 0 ? -1f : 1f;
                directions[index] = UpDirection.From(new Vector3(x, y, z));
            }

            return directions;
        }

        [Test]
        public void ConstantEvaluatesToItsNormalisedVectorAnywhere() {
            var field = UpField.Constant(0f, 0f, 2f);
            Assert.IsTrue(field.IsConstant);
            AssertUp(field, Vector3.zero, Vector3.forward);
            AssertUp(field, new Vector3(100f, -40f, 7f), Vector3.forward);
        }

        [Test]
        public void ConstantFromZeroThrows() {
            Assert.Throws<ArgumentException>(() => UpField.Constant(0f, 0f, 0f));
        }

        [Test]
        public void WorldUpIsConstantUp() {
            Assert.IsTrue(UpField.WorldUp.IsConstant);
            AssertUp(UpField.WorldUp, new Vector3(3f, 4f, 5f), Vector3.up);
        }

        [Test]
        public void ADefaultUpFieldIsWorldUp() {
            var field = default(UpField);
            Assert.AreEqual(UpFieldForm.WorldUp, field.Form);
            Assert.IsFalse(field.IsConstant);
            AssertUp(field, new Vector3(3f, 4f, 5f), Vector3.up);
            Assert.AreEqual(UpFieldForm.Constant, UpField.WorldUp.Form);
            Assert.AreEqual(1f, UpField.WorldUp.Y);
        }

        [Test]
        public void RadialPointsAwayFromTheCentre() {
            var field = UpField.RadialFrom(0f, 0f, 0f);
            Assert.IsFalse(field.IsConstant);
            AssertUp(field, new Vector3(0f, 5f, 0f), new Vector3(0f, 1f, 0f));
            AssertUp(field, new Vector3(3f, 0f, 4f), new Vector3(0.6f, 0f, 0.8f));
        }

        [Test]
        public void RadialAtTheCentreIsWorldUp() {
            var field = UpField.RadialFrom(2f, 3f, 4f);
            AssertUp(field, new Vector3(2f, 3f, 4f), Vector3.up);
        }

        [Test]
        public void SampledReadsTheCellInsideAndOutsideBeyond() {
            var directions = CornerDirections(Allocator.Temp);
            try {
                var grid = new UpGrid(directions, 0f, 0f, 0f, 1f, 2, 2, 2);
                var outside = new UpDirection(1f, 0f, 0f);
                var field = UpField.Sampled(grid, outside);
                var cell = directions[grid.IndexOf(1, 0, 1)];
                AssertUp(field, new Vector3(1.5f, 0.5f, 1.5f), cell.ToVector3());
                AssertUp(field, new Vector3(-0.1f, 0.5f, 0.5f), outside.ToVector3());
                AssertUp(field, new Vector3(0.5f, 2f, 0.5f), outside.ToVector3());
            }
            finally {
                directions.Dispose();
            }
        }

        [Test]
        public void GridWithTheWrongDirectionCountThrows() {
            var directions = new NativeArray<UpDirection>(7, Allocator.Temp);
            try {
                Assert.Throws<ArgumentException>(() => new UpGrid(directions, 0f, 0f, 0f, 1f, 2, 2, 2));
            }
            finally {
                directions.Dispose();
            }
        }

        [Test]
        public void ConstantUpProviderGivesWorldUp() {
            var provider = new ConstantUpProvider();
            Assert.AreEqual(Vector3.up, provider.UpAt(new Vector3(10f, -3f, 8f)));
            Assert.IsTrue(provider.Field(new Bounds(Vector3.zero, Vector3.one)).IsConstant);
        }

        [Test]
        public void ConstantUpProviderNormalisesItsUp() {
            var provider = new ConstantUpProvider(new Vector3(3f, 0f, 0f));
            Assert.AreEqual(Vector3.right, provider.UpAt(Vector3.zero));
        }

        [Test]
        public void IUpProvider_CustomImplementation() {
            IUpProvider provider = new PlanetUpProvider(new Vector3(0f, -10f, 0f));
            var up = provider.UpAt(new Vector3(10f, -10f, 0f));
            Assert.AreEqual(1f, up.x, Tolerance);
            Assert.AreEqual(0f, up.y, Tolerance);
            Assert.AreEqual(UpFieldForm.Radial, provider.Field(default).Form);
        }

#if SPELLBOUND_CORE_TESTS_BURST
        [Test]
        public void BurstJobEvaluatesEveryForm() {
            var directions = CornerDirections(Allocator.TempJob);
            var results = new NativeArray<UpDirection>(LatticeCellCount, Allocator.TempJob);
            try {
                var grid = new UpGrid(directions, -4f, -4f, -4f, 4f, 2, 2, 2);
                var defaultTicks = MeasureForm(default, results);
                var constantTicks = MeasureForm(UpField.Constant(1f, 2f, 2f), results);
                var radialTicks = MeasureForm(UpField.RadialFrom(0f, 0f, 0f), results);
                var sampledTicks = MeasureForm(UpField.Sampled(grid, UpDirection.WorldUp), results);
                Log.Emit(LogLevel.Info, "UpFieldTests: " + LatticeCellCount + " cells, world up "
                                        + Milliseconds(defaultTicks) + " ms, constant "
                          + Milliseconds(constantTicks) + " ms, radial " + Milliseconds(radialTicks)
                          + " ms, sampled " + Milliseconds(sampledTicks) + " ms");
            }
            finally {
                directions.Dispose();
                results.Dispose();
            }
        }

        private static long MeasureForm(UpField field, NativeArray<UpDirection> results) {
            var job = new UpFieldEvaluationJob { Field = field, Results = results };
            job.Run();
            var stopwatch = Stopwatch.StartNew();
            job.Run();
            stopwatch.Stop();

            for (var z = 0; z < LatticeSide; z++) {
                for (var y = 0; y < LatticeSide; y++) {
                    for (var x = 0; x < LatticeSide; x++) {
                        field.Evaluate(LatticeOrigin + x * LatticeSpacing, LatticeOrigin + y * LatticeSpacing,
                            LatticeOrigin + z * LatticeSpacing, out var upX, out var upY, out var upZ);
                        var result = results[x + LatticeSide * (y + LatticeSide * z)];
                        Assert.AreEqual(upX, result.X, Tolerance);
                        Assert.AreEqual(upY, result.Y, Tolerance);
                        Assert.AreEqual(upZ, result.Z, Tolerance);
                    }
                }
            }

            return stopwatch.ElapsedTicks;
        }

        private static string Milliseconds(long ticks) {
            return (ticks * 1000.0 / Stopwatch.Frequency).ToString("F4");
        }
#endif
    }
}
