// Copyright 2026 Spellbound Studio Inc.

using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Spellbound.Core.Packing {
    /// <summary>
    /// FastPackerBenchmark is attached to a bakePrefab in the prefabs folder and simply runs a quick benchmark of the
    /// available packing methods found in the core system. This script can and should be ignored for any game design
    /// purposes and exists only as a series of unit tests.
    /// </summary>
    public sealed class FastPackerBenchmark : MonoBehaviour {
        private const int Iterations = 1_000_000;

        private void Start() {
            BenchmarkInts();
            BenchmarkFloats();
            BenchmarkVectors();
            BenchmarkQuaternions();
        }

        private void BenchmarkInts() {
            var buffer = new byte[4 * Iterations];
            var span = new Span<byte>(buffer);
            var readSpan = new ReadOnlySpan<byte>(buffer);

            var sw = new Stopwatch();

            // Write ints with FastPacker (BitConverter)
            sw.Restart();
            for (var i = 0; i < Iterations; i++) Packer.WriteInt(ref span, i);
            sw.Stop();
            Debug.Log($"WriteInt (BitConverter) took: {sw.ElapsedMilliseconds} ms");

            // Read ints with FastPacker (BitConverter)
            sw.Restart();

            for (var i = 0; i < Iterations; i++) {
                var val = Packer.ReadInt(ref readSpan);
            }

            sw.Stop();
            Debug.Log($"ReadInt (BitConverter) took: {sw.ElapsedMilliseconds} ms");

            // Reset spans
            span = new Span<byte>(buffer);
            readSpan = new ReadOnlySpan<byte>(buffer);

            // Write ints bitwise
            sw.Restart();
            for (var i = 0; i < Iterations; i++) Packer.WriteIntBitwise(ref span, i);
            sw.Stop();
            Debug.Log($"WriteIntBitwise took: {sw.ElapsedMilliseconds} ms");

            // Read ints bitwise
            sw.Restart();

            for (var i = 0; i < Iterations; i++) {
                var val = Packer.ReadIntBitwise(ref readSpan);
            }

            sw.Stop();
            Debug.Log($"ReadIntBitwise took: {sw.ElapsedMilliseconds} ms");
        }

        private void BenchmarkFloats() {
            var buffer = new byte[4 * Iterations];
            var span = new Span<byte>(buffer);
            var readSpan = new ReadOnlySpan<byte>(buffer);
            var sw = new Stopwatch();

            // Write floats
            sw.Restart();
            for (var i = 0; i < Iterations; i++) Packer.WriteFloat(ref span, i * 0.1f);
            sw.Stop();
            Debug.Log($"WriteFloat (BitConverter) took: {sw.ElapsedMilliseconds} ms");

            // Read floats
            sw.Restart();

            for (var i = 0; i < Iterations; i++) {
                var val = Packer.ReadFloat(ref readSpan);
            }

            sw.Stop();
            Debug.Log($"ReadFloat (BitConverter) took: {sw.ElapsedMilliseconds} ms");

            // Reset spans
            span = new Span<byte>(buffer);
            readSpan = new ReadOnlySpan<byte>(buffer);

            // Write floats bitwise
            sw.Restart();
            for (var i = 0; i < Iterations; i++) Packer.WriteFloatBitwise(ref span, i * 0.1f);
            sw.Stop();
            Debug.Log($"WriteFloatBitwise took: {sw.ElapsedMilliseconds} ms");

            // Read floats bitwise
            sw.Restart();

            for (var i = 0; i < Iterations; i++) {
                var val = Packer.ReadFloatBitwise(ref readSpan);
            }

            sw.Stop();
            Debug.Log($"ReadFloatBitwise took: {sw.ElapsedMilliseconds} ms");
        }

        private void BenchmarkVectors() {
            var buffer = new byte[12 * Iterations]; // Vector3 = 3 floats
            var span = new Span<byte>(buffer);
            var readSpan = new ReadOnlySpan<byte>(buffer);
            var sw = new Stopwatch();
            var v = new Vector3(1.1f, 2.2f, 3.3f);

            // Write Vector3
            sw.Restart();

            for (var i = 0; i < Iterations; i++)
                Packer.WriteVector3(ref span, v);
            sw.Stop();
            Debug.Log($"WriteVector3 took: {sw.ElapsedMilliseconds} ms");

            // Read Vector3
            sw.Restart();

            for (var i = 0; i < Iterations; i++) {
                var _ = Packer.ReadVector3(ref readSpan);
            }

            sw.Stop();
            Debug.Log($"ReadVector3 took: {sw.ElapsedMilliseconds} ms");

            // Reset spans
            span = new Span<byte>(buffer);
            readSpan = new ReadOnlySpan<byte>(buffer);

            // Write Vector3 bitwise
            sw.Restart();

            for (var i = 0; i < Iterations; i++)
                Packer.WriteVector3Bitwise(ref span, v);
            sw.Stop();
            Debug.Log($"WriteVector3Bitwise took: {sw.ElapsedMilliseconds} ms");

            // Read Vector3 bitwise
            sw.Restart();

            for (var i = 0; i < Iterations; i++) {
                var _ = Packer.ReadVector3Bitwise(ref readSpan);
            }

            sw.Stop();
            Debug.Log($"ReadVector3Bitwise took: {sw.ElapsedMilliseconds} ms");
        }

        private void BenchmarkQuaternions() {
            var buffer = new byte[16 * Iterations]; // Quaternion = 4 floats
            var span = new Span<byte>(buffer);
            var readSpan = new ReadOnlySpan<byte>(buffer);
            var sw = new Stopwatch();
            var q = new Quaternion(1f, 2f, 3f, 4f);

            // Write Quaternion
            sw.Restart();

            for (var i = 0; i < Iterations; i++)
                Packer.WriteQuaternion(ref span, q);
            sw.Stop();
            Debug.Log($"WriteQuaternion took: {sw.ElapsedMilliseconds} ms");

            // Read Quaternion
            sw.Restart();

            for (var i = 0; i < Iterations; i++) {
                var _ = Packer.ReadQuaternion(ref readSpan);
            }

            sw.Stop();
            Debug.Log($"ReadQuaternion took: {sw.ElapsedMilliseconds} ms");

            // Reset spans
            span = new Span<byte>(buffer);
            readSpan = new ReadOnlySpan<byte>(buffer);

            // Write Quaternion bitwise
            sw.Restart();

            for (var i = 0; i < Iterations; i++)
                Packer.WriteQuaternionBitwise(ref span, q);
            sw.Stop();
            Debug.Log($"WriteQuaternionBitwise took: {sw.ElapsedMilliseconds} ms");

            // Read Quaternion bitwise
            sw.Restart();

            for (var i = 0; i < Iterations; i++) {
                var _ = Packer.ReadQuaternionBitwise(ref readSpan);
            }

            sw.Stop();
            Debug.Log($"ReadQuaternionBitwise took: {sw.ElapsedMilliseconds} ms");
        }
    }
}