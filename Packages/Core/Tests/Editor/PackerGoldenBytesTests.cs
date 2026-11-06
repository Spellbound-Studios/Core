// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using Spellbound.Core.Packing;
using UnityEngine;

namespace Spellbound.Core.Tests {
    public class PackerGoldenBytesTests {
        private struct TextPayload : IPacker {
            public string Text;

            public void Pack(ref Span<byte> buffer) => Packer.WriteString(ref buffer, Text);
            public void Unpack(ref ReadOnlySpan<byte> buffer) => Text = Packer.ReadString(ref buffer);
        }

        [PackerId("spellbound.core.tests.fresh")]
        private struct FreshState : ISmartPacker {
            public uint Hash => SmartPackerRegistry.GetHash<FreshState>();
            public ISmartPacker CreateNewInstance() => new FreshState();

            public void Pack(ref Span<byte> buffer) {
            }

            public void Unpack(ref ReadOnlySpan<byte> buffer) {
            }
        }

        [PackerId("spellbound.core.tests.golden")]
        private struct GoldenState : ISmartPacker {
            public int Count;

            public uint Hash => SmartPackerRegistry.GetHash<GoldenState>();
            public ISmartPacker CreateNewInstance() => new GoldenState();

            public void Pack(ref Span<byte> buffer) => Packer.WriteInt(ref buffer, Count);
            public void Unpack(ref ReadOnlySpan<byte> buffer) => Count = Packer.ReadInt(ref buffer);
        }

        [Test]
        public void PrimitivesWriteLittleEndianBytes() {
            var buffer = new byte[64];
            var span = buffer.AsSpan();

            Packer.WriteByte(ref span, 0xAB);
            Packer.WriteSByte(ref span, -2);
            Packer.WriteBool(ref span, true);
            Packer.WriteBool(ref span, false);
            Packer.WriteShort(ref span, -2);
            Packer.WriteUShort(ref span, 0x1234);
            Packer.WriteInt(ref span, 0x12345678);
            Packer.WriteUInt(ref span, 0xDEADBEEFu);
            Packer.WriteLong(ref span, 0x0102030405060708L);
            Packer.WriteULong(ref span, 0xFFEEDDCCBBAA9988UL);
            Packer.WriteFloat(ref span, 1.5f);

            var written = buffer.Length - span.Length;

            Assert.AreEqual(
                "ab fe 01 00 fe ff 34 12 78 56 34 12 ef be ad de 08 07 06 05 04 03 02 01 88 99 aa bb cc dd ee ff "
                + "00 00 c0 3f", Hex(buffer, written));
        }

        [Test]
        public void StringsBytesAndIntListsWriteLengthPrefixedBytes() {
            var buffer = new byte[64];
            var span = buffer.AsSpan();

            Packer.WriteString(ref span, "ab");
            Packer.WriteString(ref span, null);
            Packer.WriteBytes(ref span, new byte[] { 9, 8 });
            Packer.PackIntList(ref span, new List<int> { 1, -1 });

            var written = buffer.Length - span.Length;

            Assert.AreEqual(
                "02 00 00 00 61 62 00 00 00 00 02 00 00 00 09 08 02 00 00 00 01 00 00 00 ff ff ff ff",
                Hex(buffer, written));
        }

        [Test]
        public void VectorsAndQuaternionsWriteFloatBytesInOrder() {
            var buffer = new byte[64];
            var span = buffer.AsSpan();

            Packer.WriteVector3(ref span, new Vector3(1f, 2f, 3f));
            Packer.WriteVector2(ref span, new Vector2(0.5f, -1f));
            Packer.WriteQuaternion(ref span, new Quaternion(0f, 0f, 0f, 1f));

            var written = buffer.Length - span.Length;

            Assert.AreEqual(
                "00 00 80 3f 00 00 00 40 00 00 40 40 00 00 00 3f 00 00 80 bf 00 00 00 00 00 00 00 00 00 00 00 00 "
                + "00 00 80 3f", Hex(buffer, written));
        }

        [Test]
        public void ArraysListsAndPayloadsWriteCountsThenItems() {
            var items = new[] { new TextPayload { Text = "x" }, new TextPayload { Text = "yz" } };

            Assert.AreEqual("02 00 00 00 01 00 00 00 78 02 00 00 00 79 7a", Hex(Packer.PackArrayToBytes(items)));
            Assert.AreEqual("02 00 00 00 01 00 00 00 78 02 00 00 00 79 7a",
                Hex(Packer.PackListToBytes(new List<TextPayload>(items))));
            Assert.AreEqual("02 00 00 00 79 7a", Hex(Packer.ToBytes(items[1])));
            Assert.AreEqual("2a 00 00 00 04 00 00 00 63 6f 72 65", Hex(Packer.BuildPayload((ref Span<byte> buffer) => {
                Packer.WriteInt(ref buffer, 42);
                Packer.WriteString(ref buffer, "core");
            })));
        }

        [Test]
        public void SmartFormsWriteTheStableTagThenThePayload() {
            Assert.AreEqual("f2 62 0a 84 05 00 00 00", Hex(Packer.SmartToBytes(new GoldenState { Count = 5 })));
            Assert.AreEqual("01 00 00 00 f2 62 0a 84 07 00 00 00",
                Hex(Packer.SmartListToBytes(new List<ISmartPacker> { new GoldenState { Count = 7 } })));
        }

        [Test]
        public void APayloadAboveTheStackBufferWritesTheSameBytesThroughTheRentedPath() {
            var text = new string('x', 5000);
            var bytes = Packer.ToBytes(new TextPayload { Text = text });
            var payload = Packer.BuildPayload((ref Span<byte> buffer) => Packer.WriteString(ref buffer, text));

            Assert.AreEqual(5004, bytes.Length);
            Assert.AreEqual("88 13 00 00 78 78", Hex(bytes, 6));
            Assert.AreEqual(bytes, payload);
            Assert.AreEqual(bytes, Packer.ToBytes(new TextPayload { Text = text }, new PackingLimits(16, 16, 1 << 20)));

            for (var index = 4; index < bytes.Length; index++) {
                Assert.AreEqual(0x78, bytes[index]);
            }
        }

        [Test]
        public void TheFirstHashLookupOutsidePlayModeRegistersTheType() {
            var reset = typeof(SmartPackerRegistry).GetMethod("ResetForPlaySession",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(reset);
            reset.Invoke(null, null);

            var hash = SmartPackerRegistry.GetHash<FreshState>();

            Assert.AreNotEqual(0u, hash);
            Assert.IsTrue(SmartPackerRegistry.TryCreateInstance(hash, out var instance));
            Assert.IsInstanceOf<FreshState>(instance);
        }

        private static string Hex(byte[] bytes) => Hex(bytes, bytes.Length);

        private static string Hex(byte[] bytes, int count) {
            var builder = new StringBuilder(count * 3);

            for (var index = 0; index < count; index++) {
                if (index > 0) {
                    builder.Append(' ');
                }

                builder.Append(bytes[index].ToString("x2"));
            }

            return builder.ToString();
        }
    }
}
