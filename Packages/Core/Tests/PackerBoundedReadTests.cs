// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Spellbound.Core.Packing;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Spellbound.Core.Tests {
    public class PackerBoundedReadTests {
        private struct Label : IPacker {
            public string Text;

            public void Pack(ref Span<byte> buffer) => Packer.WriteString(ref buffer, Text);
            public void Unpack(ref ReadOnlySpan<byte> buffer) => Text = Packer.ReadString(ref buffer);
        }

        private struct Line : IPacker {
            public uint Kind;
            public ulong Sender;
            public string Name;
            public string Text;
            public int Count;
            public long SentAt;

            public void Pack(ref Span<byte> buffer) {
                Packer.WriteUInt(ref buffer, Kind);
                Packer.WriteULong(ref buffer, Sender);
                Packer.WriteString(ref buffer, Name);
                Packer.WriteString(ref buffer, Text);
                Packer.WriteInt(ref buffer, Count);
                Packer.WriteLong(ref buffer, SentAt);
            }

            public void Unpack(ref ReadOnlySpan<byte> buffer) {
                Kind = Packer.ReadUInt(ref buffer);
                Sender = Packer.ReadULong(ref buffer);
                Name = Packer.ReadString(ref buffer);
                Text = Packer.ReadString(ref buffer);
                Count = Packer.ReadInt(ref buffer);
                SentAt = Packer.ReadLong(ref buffer);
            }
        }

        [Test]
        public void EveryPrimitiveTryReadFailsOnAShortBufferWithoutMoving() {
            var one = new byte[] { 1 };
            ReadOnlySpan<byte> empty = ReadOnlySpan<byte>.Empty;

            Assert.IsFalse(Packer.TryReadByte(ref empty, out _));
            Assert.IsFalse(Packer.TryReadSByte(ref empty, out _));
            Assert.IsFalse(Packer.TryReadBool(ref empty, out _));

            ReadOnlySpan<byte> span = one;
            Assert.IsFalse(Packer.TryReadShort(ref span, out _));
            Assert.IsFalse(Packer.TryReadUShort(ref span, out _));
            Assert.IsFalse(Packer.TryReadInt(ref span, out _));
            Assert.IsFalse(Packer.TryReadUInt(ref span, out _));
            Assert.IsFalse(Packer.TryReadLong(ref span, out _));
            Assert.IsFalse(Packer.TryReadULong(ref span, out _));
            Assert.IsFalse(Packer.TryReadFloat(ref span, out _));
            Assert.IsFalse(Packer.TryReadVector2(ref span, out _));
            Assert.IsFalse(Packer.TryReadVector3(ref span, out _));
            Assert.IsFalse(Packer.TryReadQuaternion(ref span, out _));
            Assert.IsFalse(Packer.TryReadString(ref span, 10, out _));
            Assert.IsFalse(Packer.TryReadBytes(ref span, 10, out _));
            Assert.AreEqual(1, span.Length);
            Assert.IsTrue(Packer.TryReadByte(ref span, out var value));
            Assert.AreEqual(1, value);
            Assert.AreEqual(0, span.Length);
        }

        [Test]
        public void EveryPrimitiveTryReadReadsWhatTheWriteWrote() {
            var buffer = new byte[128];
            var write = buffer.AsSpan();
            Packer.WriteByte(ref write, 9);
            Packer.WriteSByte(ref write, -9);
            Packer.WriteBool(ref write, true);
            Packer.WriteShort(ref write, -300);
            Packer.WriteUShort(ref write, 60000);
            Packer.WriteInt(ref write, -70000);
            Packer.WriteUInt(ref write, 4000000000u);
            Packer.WriteLong(ref write, -5000000000L);
            Packer.WriteULong(ref write, 18000000000000000000UL);
            Packer.WriteFloat(ref write, 2.5f);
            Packer.WriteVector2(ref write, new Vector2(1f, 2f));
            Packer.WriteVector3(ref write, new Vector3(3f, 4f, 5f));
            Packer.WriteQuaternion(ref write, new Quaternion(0f, 1f, 0f, 0f));
            Packer.WriteVarUInt(ref write, 300u);
            Packer.WriteVarInt(ref write, -300);
            Packer.WriteVarULong(ref write, 1UL << 40);
            Packer.WriteVarLong(ref write, -(1L << 40));

            ReadOnlySpan<byte> read = buffer;
            Assert.IsTrue(Packer.TryReadByte(ref read, out var b) && b == 9);
            Assert.IsTrue(Packer.TryReadSByte(ref read, out var sb) && sb == -9);
            Assert.IsTrue(Packer.TryReadBool(ref read, out var flag) && flag);
            Assert.IsTrue(Packer.TryReadShort(ref read, out var s) && s == -300);
            Assert.IsTrue(Packer.TryReadUShort(ref read, out var us) && us == 60000);
            Assert.IsTrue(Packer.TryReadInt(ref read, out var i) && i == -70000);
            Assert.IsTrue(Packer.TryReadUInt(ref read, out var ui) && ui == 4000000000u);
            Assert.IsTrue(Packer.TryReadLong(ref read, out var l) && l == -5000000000L);
            Assert.IsTrue(Packer.TryReadULong(ref read, out var ul) && ul == 18000000000000000000UL);
            Assert.IsTrue(Packer.TryReadFloat(ref read, out var f) && f == 2.5f);
            Assert.IsTrue(Packer.TryReadVector2(ref read, out var v2) && v2 == new Vector2(1f, 2f));
            Assert.IsTrue(Packer.TryReadVector3(ref read, out var v3) && v3 == new Vector3(3f, 4f, 5f));
            Assert.IsTrue(Packer.TryReadQuaternion(ref read, out var q) && q == new Quaternion(0f, 1f, 0f, 0f));
            Assert.IsTrue(Packer.TryReadVarUInt(ref read, out var vu) && vu == 300u);
            Assert.IsTrue(Packer.TryReadVarInt(ref read, out var vi) && vi == -300);
            Assert.IsTrue(Packer.TryReadVarULong(ref read, out var vul) && vul == 1UL << 40);
            Assert.IsTrue(Packer.TryReadVarLong(ref read, out var vl) && vl == -(1L << 40));
            Assert.AreEqual(buffer.Length - (write.Length), buffer.Length - read.Length);
        }

        [Test]
        public void ByteAlignedVarIntsMatchTheBitWriterLayout() {
            var buffer = new byte[16];
            var write = buffer.AsSpan();
            Packer.WriteVarUInt(ref write, 16384u);
            Packer.WriteVarUInt(ref write, uint.MaxValue);
            Assert.AreEqual(new byte[] { 0x80, 0x80, 0x01, 0xFF, 0xFF, 0xFF, 0xFF, 0x0F },
                buffer.AsSpan(0, 8).ToArray());
            Assert.AreEqual(3, Packer.VarIntBytesOf(16384u));
            Assert.AreEqual(10, Packer.VarIntBytesOf(ulong.MaxValue));
            Assert.AreEqual(1, Packer.VarIntBytesOf(-1));
            Assert.AreEqual(5, Packer.VarIntBytesOf(int.MinValue));
            Assert.AreEqual(10, Packer.VarIntBytesOf(long.MinValue));

            var bits = new byte[16];
            var writer = new BitWriter(bits);
            writer.WriteVarUInt(16384u);
            writer.WriteVarUInt(uint.MaxValue);
            Assert.AreEqual(buffer.AsSpan(0, 8).ToArray(), writer.Written.ToArray());

            ReadOnlySpan<byte> read = buffer;
            Assert.AreEqual(16384u, Packer.ReadVarUInt(ref read));
            Assert.AreEqual(uint.MaxValue, Packer.ReadVarUInt(ref read));
            ReadOnlySpan<byte> cut = buffer.AsSpan(0, 2);
            Assert.IsFalse(Packer.TryReadVarUInt(ref cut, out _));
            Assert.AreEqual(2, cut.Length);
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                ReadOnlySpan<byte> failing = new byte[] { 0x80 };
                Packer.ReadVarUInt(ref failing);
            });

            var tenth = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x02 };
            ReadOnlySpan<byte> overflow = tenth;
            Assert.IsFalse(Packer.TryReadVarULong(ref overflow, out _));
            Assert.AreEqual(10, overflow.Length);
            tenth[9] = 0x01;
            ReadOnlySpan<byte> largest = tenth;
            Assert.IsTrue(Packer.TryReadVarULong(ref largest, out var all));
            Assert.AreEqual(ulong.MaxValue, all);
        }

        [Test]
        public void StringAndByteLimitsRefuseLongNegativeAndTruncatedLengths() {
            var buffer = new byte[64];
            var write = buffer.AsSpan();
            Packer.WriteString(ref write, "twelve chars");
            var written = buffer.Length - write.Length;

            ReadOnlySpan<byte> read = buffer.AsSpan(0, written);
            Assert.IsFalse(Packer.TryReadString(ref read, 11, out var refused));
            Assert.IsNull(refused);
            Assert.AreEqual(written, read.Length);
            Assert.IsTrue(Packer.TryReadString(ref read, 12, out var accepted));
            Assert.AreEqual("twelve chars", accepted);
            Assert.AreEqual(0, read.Length);

            ReadOnlySpan<byte> truncated = buffer.AsSpan(0, written - 1);
            Assert.IsFalse(Packer.TryReadString(ref truncated, 100, out _));
            Assert.AreEqual(written - 1, truncated.Length);

            var negative = new byte[8];
            var negativeSpan = negative.AsSpan();
            Packer.WriteInt(ref negativeSpan, -1);
            ReadOnlySpan<byte> negativeRead = negative;
            Assert.IsFalse(Packer.TryReadString(ref negativeRead, 100, out _));
            Assert.IsFalse(Packer.TryReadBytes(ref negativeRead, 100, out _));
            Assert.AreEqual(8, negativeRead.Length);

            var empty = new byte[4];
            ReadOnlySpan<byte> emptyRead = empty;
            Assert.IsTrue(Packer.TryReadString(ref emptyRead, 0, out var emptyText));
            Assert.AreEqual(string.Empty, emptyText);
            emptyRead = empty;
            Assert.IsTrue(Packer.TryReadBytes(ref emptyRead, DecodeLimits.Default, out var emptyBytes));
            Assert.AreEqual(0, emptyBytes.Length);
        }

        [Test]
        public void CountsAboveTheLimitAreRefusedForArraysAndLists() {
            var items = new[] { new Label { Text = "a" }, new Label { Text = "b" }, new Label { Text = "c" } };
            var bytes = Packer.PackArrayToBytes(items);

            ReadOnlySpan<byte> read = bytes;
            Assert.IsFalse(Packer.TryUnpackArray<Label>(ref read, 2, out var refused));
            Assert.IsNull(refused);
            Assert.AreEqual(bytes.Length, read.Length);
            Assert.IsTrue(Packer.TryUnpackArray<Label>(ref read, 3, out var array));
            Assert.AreEqual(3, array.Length);
            Assert.AreEqual("c", array[2].Text);
            Assert.AreEqual(0, read.Length);

            read = bytes;
            Assert.IsTrue(Packer.TryUnpackList<Label>(ref read, DecodeLimits.Default, out var list));
            Assert.AreEqual(3, list.Count);

            ReadOnlySpan<byte> truncated = bytes.AsSpan(0, bytes.Length - 2);
            Assert.IsFalse(Packer.TryUnpackList<Label>(ref truncated, 10, out _));
            Assert.AreEqual(bytes.Length - 2, truncated.Length);

            var ints = new byte[32];
            var intSpan = ints.AsSpan();
            Packer.PackIntList(ref intSpan, new List<int> { 1, 2, 3 });
            ReadOnlySpan<byte> intRead = ints.AsSpan(0, 16);
            Assert.IsFalse(Packer.TryUnpackIntList(ref intRead, 2, out _));
            Assert.IsTrue(Packer.TryUnpackIntList(ref intRead, 3, out var intList));
            Assert.AreEqual(new List<int> { 1, 2, 3 }, intList);
            ReadOnlySpan<byte> intTruncated = ints.AsSpan(0, 12);
            Assert.IsFalse(Packer.TryUnpackIntList(ref intTruncated, 3, out _));
        }

        [Test]
        public void TryUnpackRefusesTrailingBytesAndShortPayloads() {
            var bytes = Packer.ToBytes(new Label { Text = "whole" });

            Assert.IsTrue(Packer.TryUnpack<Label>(bytes, out var label));
            Assert.AreEqual("whole", label.Text);

            var trailing = new byte[bytes.Length + 1];
            bytes.CopyTo(trailing, 0);
            Assert.IsFalse(Packer.TryUnpack<Label>(trailing, out var refused));
            Assert.IsNull(refused.Text);
            Assert.IsFalse(Packer.TryUnpack<Label>(bytes.AsSpan(0, bytes.Length - 1), out _));
            Assert.IsFalse(Packer.TryUnpack<Label>(ReadOnlySpan<byte>.Empty, out _));
        }

        [Test]
        public void AMessageShapedPayloadTruncatedAtEveryLengthNeverThrows() {
            var line = new Line { Kind = 3, Sender = 99, Name = "sender", Text = "hello there", Count = 2, SentAt = 5 };
            var bytes = Packer.ToBytes(line);

            for (var length = 0; length <= bytes.Length; length++) {
                var accepted = Packer.TryUnpack<Line>(bytes.AsSpan(0, length), out var result);
                Assert.AreEqual(length == bytes.Length, accepted, "length " + length);

                if (accepted) {
                    Assert.AreEqual("hello there", result.Text);
                }
            }

            var corrupt = (byte[])bytes.Clone();
            corrupt[12] = 0x7F;
            Assert.IsFalse(Packer.TryUnpack<Line>(corrupt, out _));
        }

        private struct Numbers : IPacker {
            public int Count;
            public float Scale;

            public void Pack(ref Span<byte> buffer) {
                Packer.WriteInt(ref buffer, Count);
                Packer.WriteFloat(ref buffer, Scale);
            }

            public void Unpack(ref ReadOnlySpan<byte> buffer) {
                Count = Packer.ReadInt(ref buffer);
                Scale = Packer.ReadFloat(ref buffer);
            }
        }

        private struct Labels : IPacker {
            public Label[] Items;
            public List<int> Numbers;

            public void Pack(ref Span<byte> buffer) {
                Packer.PackArray(ref buffer, Items);
                Packer.PackIntList(ref buffer, Numbers);
            }

            public void Unpack(ref ReadOnlySpan<byte> buffer) {
                Items = Packer.UnpackArray<Label>(ref buffer);
                Numbers = Packer.UnpackIntList(ref buffer);
            }
        }

        [Test]
        public void TryUnpackRefusesAHugeCountWithoutAllocatingIt() {
            var buffer = new byte[16];
            var write = buffer.AsSpan();
            Packer.WriteInt(ref write, int.MaxValue);

            Assert.IsFalse(Packer.TryUnpack<Labels>(buffer, out _));

            write = buffer.AsSpan();
            Packer.WriteInt(ref write, 0);
            Packer.WriteInt(ref write, 0x10000000);
            Assert.IsFalse(Packer.TryUnpack<Labels>(buffer, out _));

            write = buffer.AsSpan();
            Packer.WriteInt(ref write, 0);
            Packer.WriteInt(ref write, 2);
            Packer.WriteInt(ref write, 5);
            Packer.WriteInt(ref write, 6);
            Assert.IsTrue(Packer.TryUnpack<Labels>(buffer, out var labels));
            Assert.AreEqual(new List<int> { 5, 6 }, labels.Numbers);
        }

        [Test]
        public void TryUnpack_NoAllocation() {
            var bytes = Packer.ToBytes(new Numbers { Count = 3, Scale = 0.5f });
            TestDelegate action = () => { Packer.TryUnpack<Numbers>(bytes, out _); };

            for (var warm = 0; warm < 3; warm++) {
                action();
            }

            var constraint = ((IResolveConstraint)Is.Not.AllocatingGCMemory()).Resolve();
            constraint.ApplyTo(action);
            Assert.That(action, constraint);
        }

        [Test]
        public void TryRead_NoAllocation() {
            var buffer = new byte[64];
            var write = buffer.AsSpan();
            Packer.WriteInt(ref write, 5);
            Packer.WriteVarUInt(ref write, 300u);
            Packer.WriteFloat(ref write, 1f);
            Packer.WriteVector3(ref write, Vector3.one);
            TestDelegate action = () => {
                ReadOnlySpan<byte> read = buffer;
                Packer.TryReadInt(ref read, out _);
                Packer.TryReadVarUInt(ref read, out _);
                Packer.TryReadFloat(ref read, out _);
                Packer.TryReadVector3(ref read, out _);
            };

            for (var warm = 0; warm < 3; warm++) {
                action();
            }

            var constraint = ((IResolveConstraint)Is.Not.AllocatingGCMemory()).Resolve();
            constraint.ApplyTo(action);
            Assert.That(action, constraint);
        }
    }
}
