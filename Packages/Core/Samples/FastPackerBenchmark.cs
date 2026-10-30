// Copyright 2026 Spellbound Studio Inc.

using System;
using System.Diagnostics;
using Spellbound.Core.Logging;
using UnityEngine;

namespace Spellbound.Core.Packing {
    public sealed class FastPackerBenchmark : MonoBehaviour {
        [SerializeField] private int iterations = 1_000_000;

        private void Start() {
            BenchmarkInts();
            BenchmarkFloats();
            BenchmarkVectors();
            BenchmarkQuaternions();
        }

        private void BenchmarkInts() {
            var buffer = new byte[sizeof(int) * iterations];
            var span = new Span<byte>(buffer);
            var readSpan = new ReadOnlySpan<byte>(buffer);
            var stopwatch = new Stopwatch();

            stopwatch.Restart();

            for (var index = 0; index < iterations; index++) {
                Packer.WriteInt(ref span, index);
            }

            stopwatch.Stop();
            Report("WriteInt", stopwatch);

            stopwatch.Restart();
            var sum = 0;

            for (var index = 0; index < iterations; index++) {
                sum += Packer.ReadInt(ref readSpan);
            }

            stopwatch.Stop();
            Report("ReadInt", stopwatch);
            Keep(sum);
        }

        private void BenchmarkFloats() {
            var buffer = new byte[sizeof(float) * iterations];
            var span = new Span<byte>(buffer);
            var readSpan = new ReadOnlySpan<byte>(buffer);
            var stopwatch = new Stopwatch();

            stopwatch.Restart();

            for (var index = 0; index < iterations; index++) {
                Packer.WriteFloat(ref span, index * 0.1f);
            }

            stopwatch.Stop();
            Report("WriteFloat", stopwatch);

            stopwatch.Restart();
            var sum = 0f;

            for (var index = 0; index < iterations; index++) {
                sum += Packer.ReadFloat(ref readSpan);
            }

            stopwatch.Stop();
            Report("ReadFloat", stopwatch);
            Keep(sum);
        }

        private void BenchmarkVectors() {
            var buffer = new byte[sizeof(float) * 3 * iterations];
            var span = new Span<byte>(buffer);
            var readSpan = new ReadOnlySpan<byte>(buffer);
            var stopwatch = new Stopwatch();
            var vector = new Vector3(1.1f, 2.2f, 3.3f);

            stopwatch.Restart();

            for (var index = 0; index < iterations; index++) {
                Packer.WriteVector3(ref span, vector);
            }

            stopwatch.Stop();
            Report("WriteVector3", stopwatch);

            stopwatch.Restart();
            var sum = 0f;

            for (var index = 0; index < iterations; index++) {
                sum += Packer.ReadVector3(ref readSpan).x;
            }

            stopwatch.Stop();
            Report("ReadVector3", stopwatch);
            Keep(sum);
        }

        private void BenchmarkQuaternions() {
            var buffer = new byte[sizeof(float) * 4 * iterations];
            var span = new Span<byte>(buffer);
            var readSpan = new ReadOnlySpan<byte>(buffer);
            var stopwatch = new Stopwatch();
            var quaternion = new Quaternion(1f, 2f, 3f, 4f);

            stopwatch.Restart();

            for (var index = 0; index < iterations; index++) {
                Packer.WriteQuaternion(ref span, quaternion);
            }

            stopwatch.Stop();
            Report("WriteQuaternion", stopwatch);

            stopwatch.Restart();
            var sum = 0f;

            for (var index = 0; index < iterations; index++) {
                sum += Packer.ReadQuaternion(ref readSpan).w;
            }

            stopwatch.Stop();
            Report("ReadQuaternion", stopwatch);
            Keep(sum);
        }

        private void Report(string name, Stopwatch stopwatch) =>
            Log.Emit(LogLevel.Info, "FastPackerBenchmark: " + name + " of " + iterations + " took "
                                    + stopwatch.ElapsedMilliseconds + " ms", this);

        private static float _kept;

        private static void Keep(float value) {
            _kept += value;
        }
    }
}
