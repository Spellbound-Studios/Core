// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Buffers;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Spellbound.Core.Packing;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Spellbound.Core.Tests {
    public class PackerBufferTests {
        private struct Pose : IPacker {
            public int Id;
            public float X;
            public string Name;

            public void Pack(ref Span<byte> buffer) {
                Packer.WriteInt(ref buffer, Id);
                Packer.WriteFloat(ref buffer, X);
                Packer.WriteString(ref buffer, Name);
            }

            public void Unpack(ref ReadOnlySpan<byte> buffer) {
                Id = Packer.ReadInt(ref buffer);
                X = Packer.ReadFloat(ref buffer);
                Name = Packer.ReadString(ref buffer);
            }
        }

        private struct Wide : IPacker {
            public int Length;

            public void Pack(ref Span<byte> buffer) {
                Packer.WriteInt(ref buffer, Length);

                for (var index = 0; index < Length; index++) {
                    Packer.WriteByte(ref buffer, (byte)index);
                }
            }

            public void Unpack(ref ReadOnlySpan<byte> buffer) {
                Length = Packer.ReadInt(ref buffer);
                buffer = buffer[Length..];
            }
        }

        private struct Throwing : IPacker {
            public void Pack(ref Span<byte> buffer) => throw new ArgumentNullException("inside");

            public void Unpack(ref ReadOnlySpan<byte> buffer) {
            }
        }

        private static readonly Pose Sample = new() { Id = 7, X = 1.5f, Name = "pose" };

        [Test]
        public void TryPackWritesIntoACallerOwnedBufferAndCountsTheBytes() {
            var destination = new byte[64];

            Assert.IsTrue(Packer.TryPack(in Sample, destination, out var written));
            Assert.AreEqual(16, written);
            Assert.AreEqual(Packer.ToBytes(Sample), destination.AsSpan(0, written).ToArray());
        }

        [Test]
        public void TryPackRefusesABufferOneByteShortAndAnEmptyOne() {
            var exact = new byte[16];
            var short1 = new byte[15];

            Assert.IsTrue(Packer.TryPack(in Sample, exact, out var written));
            Assert.AreEqual(16, written);
            Assert.IsFalse(Packer.TryPack(in Sample, short1, out written));
            Assert.AreEqual(0, written);
            Assert.IsFalse(Packer.TryPack(in Sample, Span<byte>.Empty, out written));
            Assert.AreEqual(0, written);
        }

        [Test]
        public void TryPackTakesAWriter() {
            var destination = new byte[8];

            Assert.IsTrue(Packer.TryPack((ref Span<byte> buffer) => Packer.WriteInt(ref buffer, 42), destination,
                out var written));
            Assert.AreEqual(4, written);
            Assert.IsFalse(Packer.TryPack((ref Span<byte> buffer) => Packer.WriteLong(ref buffer, 1L),
                destination.AsSpan(0, 4), out written));
            Assert.AreEqual(0, written);
        }

        [Test]
        public void AnExceptionThatIsNotAnOverflowLeavesTryPackAndToBytes() {
            var throwing = new Throwing();

            Assert.Throws<ArgumentNullException>(() => Packer.TryPack(in throwing, new byte[8], out _));
            Assert.Throws<ArgumentNullException>(() => Packer.ToBytes(throwing));
        }

        [Test]
        public void PackPooledHoldsTheSameBytesAsToBytesAndReturnsItsArray() {
            byte[] array;

            using (var packed = Packer.PackPooled(in Sample)) {
                array = packed.Array;
                Assert.AreEqual(16, packed.Length);
                Assert.AreEqual(Packer.ToBytes(Sample), packed.Bytes.ToArray());
                Assert.GreaterOrEqual(array.Length, 16);
            }

            using var again = Packer.PackPooled((ref Span<byte> buffer) => Packer.WriteString(ref buffer, "pooled"));
            Assert.AreEqual(10, again.Length);
            ReadOnlySpan<byte> bytes = again.Bytes;
            Assert.AreEqual("pooled", Packer.ReadString(ref bytes));
        }

        [Test]
        public void DisposingThroughACopyReturnsTheArrayOnce() {
            var packed = Packer.PackPooled(in Sample);
            var copy = packed;
            copy.Dispose();
            packed.Dispose();

            Assert.AreEqual(0, packed.Length);
            Assert.AreEqual(0, packed.Bytes.Length);
            Assert.AreEqual(0, packed.Array.Length);

            var first = ArrayPool<byte>.Shared.Rent(16);
            var second = ArrayPool<byte>.Shared.Rent(16);
            Assert.AreNotSame(first, second);
            ArrayPool<byte>.Shared.Return(first);
            ArrayPool<byte>.Shared.Return(second);
        }

        [Test]
        public void PackPooledGrowsPastTheFirstRentedSize() {
            var wide = new Wide { Length = 20000 };

            using var packed = Packer.PackPooled(in wide);
            Assert.AreEqual(20004, packed.Length);
            Assert.AreEqual(19, packed.Bytes[23]);
        }

        [Test]
        public void TheLimitsComeFromTheArgumentAndADefaultValueIsTheDefault() {
            var limits = new PackingLimits(16, 16, 64);
            var wide = new Wide { Length = 40 };
            var tooWide = new Wide { Length = 100 };
            var large = new PackingLimits(16, 1 << 20, 1 << 20);

            Assert.AreEqual(44, Packer.ToBytes(wide, limits).Length);
            Assert.Throws<InvalidOperationException>(() => Packer.ToBytes(tooWide, limits));
            Assert.Throws<InvalidOperationException>(() => Packer.PackPooled(in tooWide, limits));
            Assert.AreEqual(104, Packer.ToBytes(tooWide).Length);
            Assert.AreEqual(Packer.ToBytes(tooWide), Packer.ToBytes(tooWide, large));
            Assert.AreEqual(Packer.ToBytes(tooWide), Packer.ToBytes(tooWide, default(PackingLimits)));
            Assert.AreEqual(4096, PackingLimits.Default.StackBufferBytes);
            Assert.AreEqual(8192, PackingLimits.Default.FirstRentBytes);
            Assert.AreEqual(64 * 1024 * 1024, PackingLimits.Default.MaximumPayloadBytes);
            Assert.AreEqual(4096, default(PackingLimits).StackBufferBytes);
            Assert.Throws<ArgumentOutOfRangeException>(() => new PackingLimits(0, 10, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PackingLimits(10, 10, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PackingLimits(10, 0, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PackingLimits(PackingLimits.LargestStackBufferBytes + 1, 1 << 20, 1 << 20));
        }

        [Test]
        public void TheFrontDoorForwardsToThePacker() {
            var destination = new byte[32];

            Assert.IsTrue(CoreKit.Packing.TryPack(in Sample, destination, out var written));
            Assert.AreEqual(16, written);
            Assert.IsTrue(CoreKit.Packing.TryUnpack<Pose>(destination.AsSpan(0, written), out var pose));
            Assert.AreEqual("pose", pose.Name);

            using var packed = CoreKit.Packing.PackPooled(in Sample);
            Assert.AreEqual(16, packed.Length);
            using var written2 = CoreKit.Packing.PackPooled((ref Span<byte> buffer) => Packer.WriteInt(ref buffer, 1));
            Assert.AreEqual(4, written2.Length);
        }

        [Test]
        public void TryPack_NoAllocation() {
            var destination = new byte[64];
            var pose = new Pose { Id = 1, X = 2f, Name = null };
            TestDelegate action = () => { Packer.TryPack(in pose, destination, out _); };

            for (var warm = 0; warm < 3; warm++) {
                action();
            }

            var constraint = ((IResolveConstraint)Is.Not.AllocatingGCMemory()).Resolve();
            constraint.ApplyTo(action);
            Assert.That(action, constraint);
        }
    }
}
