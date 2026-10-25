// Copyright 2026 Spellbound Studio Inc.

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Spellbound.Core.Logging;
using Spellbound.Core.Packing;
using UnityEngine;

namespace Spellbound.Core.Tests {
    public class PackerTests {
        private struct NumericPayload : IPacker {
            public byte ByteValue;
            public sbyte SByteValue;
            public bool BoolValue;
            public short ShortValue;
            public ushort UShortValue;
            public int IntValue;
            public uint UIntValue;
            public long LongValue;
            public ulong ULongValue;
            public float FloatValue;

            public void Pack(ref Span<byte> buffer) {
                Packer.WriteByte(ref buffer, ByteValue);
                Packer.WriteSByte(ref buffer, SByteValue);
                Packer.WriteBool(ref buffer, BoolValue);
                Packer.WriteShort(ref buffer, ShortValue);
                Packer.WriteUShort(ref buffer, UShortValue);
                Packer.WriteInt(ref buffer, IntValue);
                Packer.WriteUInt(ref buffer, UIntValue);
                Packer.WriteLong(ref buffer, LongValue);
                Packer.WriteULong(ref buffer, ULongValue);
                Packer.WriteFloat(ref buffer, FloatValue);
            }

            public void Unpack(ref ReadOnlySpan<byte> buffer) {
                ByteValue = Packer.ReadByte(ref buffer);
                SByteValue = Packer.ReadSByte(ref buffer);
                BoolValue = Packer.ReadBool(ref buffer);
                ShortValue = Packer.ReadShort(ref buffer);
                UShortValue = Packer.ReadUShort(ref buffer);
                IntValue = Packer.ReadInt(ref buffer);
                UIntValue = Packer.ReadUInt(ref buffer);
                LongValue = Packer.ReadLong(ref buffer);
                ULongValue = Packer.ReadULong(ref buffer);
                FloatValue = Packer.ReadFloat(ref buffer);
            }
        }

        private struct TextPayload : IPacker {
            public string Text;

            public void Pack(ref Span<byte> buffer) => Packer.WriteString(ref buffer, Text);
            public void Unpack(ref ReadOnlySpan<byte> buffer) => Text = Packer.ReadString(ref buffer);
        }

        [PackerId("spellbound.core.tests.counter")]
        private struct CounterState : ISmartPacker {
            public int Count;

            public uint Hash => SmartPackerRegistry.GetHash<CounterState>();
            public ISmartPacker CreateNewInstance() => new CounterState();

            public void Pack(ref Span<byte> buffer) => Packer.WriteInt(ref buffer, Count);
            public void Unpack(ref ReadOnlySpan<byte> buffer) => Count = Packer.ReadInt(ref buffer);
        }

        [PackerId("spellbound.core.tests.label")]
        private class LabelState : ISmartPacker {
            public string Label;

            public uint Hash => SmartPackerRegistry.GetHash<LabelState>();
            public ISmartPacker CreateNewInstance() => new LabelState();

            public void Pack(ref Span<byte> buffer) => Packer.WriteString(ref buffer, Label);
            public void Unpack(ref ReadOnlySpan<byte> buffer) => Label = Packer.ReadString(ref buffer);
        }

        [Test]
        public void ByteRoundTripsAcrossFullRange() {
            var buffer = new byte[256];
            var writeSpan = buffer.AsSpan();

            for (var value = 0; value <= byte.MaxValue; value++)
                Packer.WriteByte(ref writeSpan, (byte)value);

            Assert.AreEqual(0, writeSpan.Length);

            ReadOnlySpan<byte> readSpan = buffer;

            for (var value = 0; value <= byte.MaxValue; value++)
                Assert.AreEqual((byte)value, Packer.ReadByte(ref readSpan));

            Assert.AreEqual(0, readSpan.Length);
        }

        [Test]
        public void SByteRoundTripsAcrossFullRange() {
            var buffer = new byte[256];
            var writeSpan = buffer.AsSpan();

            for (var value = (int)sbyte.MinValue; value <= sbyte.MaxValue; value++)
                Packer.WriteSByte(ref writeSpan, (sbyte)value);

            Assert.AreEqual(0, writeSpan.Length);

            ReadOnlySpan<byte> readSpan = buffer;

            for (var value = (int)sbyte.MinValue; value <= sbyte.MaxValue; value++)
                Assert.AreEqual((sbyte)value, Packer.ReadSByte(ref readSpan));

            Assert.AreEqual(0, readSpan.Length);
        }

        [Test]
        public void SByteWritesOneTwosComplementByte() {
            var buffer = new byte[4];
            var writeSpan = buffer.AsSpan();

            Packer.WriteSByte(ref writeSpan, sbyte.MinValue);
            Packer.WriteSByte(ref writeSpan, -1);
            Packer.WriteSByte(ref writeSpan, 0);
            Packer.WriteSByte(ref writeSpan, sbyte.MaxValue);

            Assert.AreEqual(0, writeSpan.Length);
            Assert.AreEqual(new byte[] { 0x80, 0xFF, 0x00, 0x7F }, buffer);
        }

        [Test]
        public void SByteAndByteShareTheSameByteLayout() {
            var signed = new byte[2];
            var unsigned = new byte[2];
            var signedSpan = signed.AsSpan();
            var unsignedSpan = unsigned.AsSpan();

            Packer.WriteSByte(ref signedSpan, -128);
            Packer.WriteSByte(ref signedSpan, -1);
            Packer.WriteByte(ref unsignedSpan, 128);
            Packer.WriteByte(ref unsignedSpan, 255);

            Assert.AreEqual(unsigned, signed);
        }

        [Test]
        public void SByteReadsBackFromBytesWrittenAsUnsigned() {
            var buffer = new byte[] { 0x80, 0xFF, 0x00, 0x7F };

            ReadOnlySpan<byte> readSpan = buffer;

            Assert.AreEqual(sbyte.MinValue, Packer.ReadSByte(ref readSpan));
            Assert.AreEqual((sbyte)-1, Packer.ReadSByte(ref readSpan));
            Assert.AreEqual((sbyte)0, Packer.ReadSByte(ref readSpan));
            Assert.AreEqual(sbyte.MaxValue, Packer.ReadSByte(ref readSpan));
            Assert.AreEqual(0, readSpan.Length);
        }

        [Test]
        public void BoolRoundTripsBothStates() {
            var buffer = new byte[2];
            var writeSpan = buffer.AsSpan();

            Packer.WriteBool(ref writeSpan, true);
            Packer.WriteBool(ref writeSpan, false);

            Assert.AreEqual(0, writeSpan.Length);
            Assert.AreEqual(new byte[] { 1, 0 }, buffer);

            ReadOnlySpan<byte> readSpan = buffer;

            Assert.IsTrue(Packer.ReadBool(ref readSpan));
            Assert.IsFalse(Packer.ReadBool(ref readSpan));
            Assert.AreEqual(0, readSpan.Length);
        }

        [Test]
        public void BoolReadsAnyNonZeroByteAsTrue() {
            ReadOnlySpan<byte> readSpan = new byte[] { 2, 255 };

            Assert.IsTrue(Packer.ReadBool(ref readSpan));
            Assert.IsTrue(Packer.ReadBool(ref readSpan));
        }

        [Test]
        public void ShortRoundTripsAtBoundaries() {
            var values = new short[] { short.MinValue, -1, 0, 1, short.MaxValue };
            var buffer = new byte[values.Length * sizeof(short)];
            var writeSpan = buffer.AsSpan();

            foreach (var value in values)
                Packer.WriteShort(ref writeSpan, value);

            Assert.AreEqual(0, writeSpan.Length);

            ReadOnlySpan<byte> readSpan = buffer;

            foreach (var value in values)
                Assert.AreEqual(value, Packer.ReadShort(ref readSpan));

            Assert.AreEqual(0, readSpan.Length);
        }

        [Test]
        public void UShortRoundTripsAtBoundaries() {
            var values = new ushort[] { ushort.MinValue, 1, 32768, ushort.MaxValue };
            var buffer = new byte[values.Length * sizeof(ushort)];
            var writeSpan = buffer.AsSpan();

            foreach (var value in values)
                Packer.WriteUShort(ref writeSpan, value);

            Assert.AreEqual(0, writeSpan.Length);

            ReadOnlySpan<byte> readSpan = buffer;

            foreach (var value in values)
                Assert.AreEqual(value, Packer.ReadUShort(ref readSpan));

            Assert.AreEqual(0, readSpan.Length);
        }

        [Test]
        public void IntRoundTripsAtBoundaries() {
            var values = new[] { int.MinValue, -1, 0, 1, int.MaxValue };
            var buffer = new byte[values.Length * sizeof(int)];
            var writeSpan = buffer.AsSpan();

            foreach (var value in values)
                Packer.WriteInt(ref writeSpan, value);

            Assert.AreEqual(0, writeSpan.Length);

            ReadOnlySpan<byte> readSpan = buffer;

            foreach (var value in values)
                Assert.AreEqual(value, Packer.ReadInt(ref readSpan));

            Assert.AreEqual(0, readSpan.Length);
        }

        [Test]
        public void UIntRoundTripsAtBoundaries() {
            var values = new[] { uint.MinValue, 1u, 2147483648u, uint.MaxValue };
            var buffer = new byte[values.Length * sizeof(uint)];
            var writeSpan = buffer.AsSpan();

            foreach (var value in values)
                Packer.WriteUInt(ref writeSpan, value);

            Assert.AreEqual(0, writeSpan.Length);

            ReadOnlySpan<byte> readSpan = buffer;

            foreach (var value in values)
                Assert.AreEqual(value, Packer.ReadUInt(ref readSpan));

            Assert.AreEqual(0, readSpan.Length);
        }

        [Test]
        public void LongRoundTripsAtBoundaries() {
            var values = new[] { long.MinValue, -1L, 0L, 1L, long.MaxValue };
            var buffer = new byte[values.Length * sizeof(long)];
            var writeSpan = buffer.AsSpan();

            foreach (var value in values)
                Packer.WriteLong(ref writeSpan, value);

            Assert.AreEqual(0, writeSpan.Length);

            ReadOnlySpan<byte> readSpan = buffer;

            foreach (var value in values)
                Assert.AreEqual(value, Packer.ReadLong(ref readSpan));

            Assert.AreEqual(0, readSpan.Length);
        }

        [Test]
        public void ULongRoundTripsAtBoundaries() {
            var values = new[] { ulong.MinValue, 1ul, 9223372036854775808ul, ulong.MaxValue };
            var buffer = new byte[values.Length * sizeof(ulong)];
            var writeSpan = buffer.AsSpan();

            foreach (var value in values)
                Packer.WriteULong(ref writeSpan, value);

            Assert.AreEqual(0, writeSpan.Length);

            ReadOnlySpan<byte> readSpan = buffer;

            foreach (var value in values)
                Assert.AreEqual(value, Packer.ReadULong(ref readSpan));

            Assert.AreEqual(0, readSpan.Length);
        }

        [Test]
        public void FloatRoundTripsSpecialValues() {
            var values = new[] {
                0f, -0f, 1f, -1f, float.Epsilon, float.MinValue, float.MaxValue,
                float.PositiveInfinity, float.NegativeInfinity, float.NaN
            };

            var buffer = new byte[values.Length * sizeof(float)];
            var writeSpan = buffer.AsSpan();

            foreach (var value in values)
                Packer.WriteFloat(ref writeSpan, value);

            Assert.AreEqual(0, writeSpan.Length);

            ReadOnlySpan<byte> readSpan = buffer;

            foreach (var value in values) {
                Assert.AreEqual(BitConverter.SingleToInt32Bits(value),
                                BitConverter.SingleToInt32Bits(Packer.ReadFloat(ref readSpan)));
            }

            Assert.AreEqual(0, readSpan.Length);
        }

        [Test]
        public void NumericPrimitivesRoundTrip() {
            var payload = new NumericPayload {
                ByteValue = 200,
                SByteValue = -128,
                BoolValue = true,
                ShortValue = -12345,
                UShortValue = 54321,
                IntValue = int.MinValue,
                UIntValue = uint.MaxValue,
                LongValue = long.MinValue,
                ULongValue = ulong.MaxValue,
                FloatValue = 3.14159f
            };

            var bytes = Packer.ToBytes(payload);

            Assert.AreEqual(35, bytes.Length);
            Assert.AreEqual(payload, Packer.FromBytes<NumericPayload>(bytes));
        }

        [Test]
        public void BitwiseVariantsRoundTrip() {
            var buffer = new byte[32];
            var writeSpan = buffer.AsSpan();

            Packer.WriteShortBitwise(ref writeSpan, short.MinValue);
            Packer.WriteUShortBitwise(ref writeSpan, ushort.MaxValue);
            Packer.WriteIntBitwise(ref writeSpan, int.MinValue);
            Packer.WriteUIntBitwise(ref writeSpan, uint.MaxValue);
            Packer.WriteLongBitwise(ref writeSpan, long.MinValue);
            Packer.WriteULongBitwise(ref writeSpan, ulong.MaxValue);
            Packer.WriteFloatBitwise(ref writeSpan, -12.5f);

            Assert.AreEqual(0, writeSpan.Length);

            ReadOnlySpan<byte> readSpan = buffer;

            Assert.AreEqual(short.MinValue, Packer.ReadShortBitwise(ref readSpan));
            Assert.AreEqual(ushort.MaxValue, Packer.ReadUShortBitwise(ref readSpan));
            Assert.AreEqual(int.MinValue, Packer.ReadIntBitwise(ref readSpan));
            Assert.AreEqual(uint.MaxValue, Packer.ReadUIntBitwise(ref readSpan));
            Assert.AreEqual(long.MinValue, Packer.ReadLongBitwise(ref readSpan));
            Assert.AreEqual(ulong.MaxValue, Packer.ReadULongBitwise(ref readSpan));
            Assert.AreEqual(-12.5f, Packer.ReadFloatBitwise(ref readSpan));
            Assert.AreEqual(0, readSpan.Length);
        }

        [Test]
        public void BitwiseVariantsMatchBitConverterLayout() {
            var standard = new byte[32];
            var bitwise = new byte[32];
            var standardSpan = standard.AsSpan();
            var bitwiseSpan = bitwise.AsSpan();

            Packer.WriteShort(ref standardSpan, -1234);
            Packer.WriteUShort(ref standardSpan, 65000);
            Packer.WriteInt(ref standardSpan, -123456789);
            Packer.WriteUInt(ref standardSpan, 3123456789u);
            Packer.WriteLong(ref standardSpan, -1234567890123L);
            Packer.WriteULong(ref standardSpan, 12345678901234567890ul);
            Packer.WriteFloat(ref standardSpan, 1.5f);

            Packer.WriteShortBitwise(ref bitwiseSpan, -1234);
            Packer.WriteUShortBitwise(ref bitwiseSpan, 65000);
            Packer.WriteIntBitwise(ref bitwiseSpan, -123456789);
            Packer.WriteUIntBitwise(ref bitwiseSpan, 3123456789u);
            Packer.WriteLongBitwise(ref bitwiseSpan, -1234567890123L);
            Packer.WriteULongBitwise(ref bitwiseSpan, 12345678901234567890ul);
            Packer.WriteFloatBitwise(ref bitwiseSpan, 1.5f);

            Assert.AreEqual(standard, bitwise);
        }

        [Test]
        public void BitwiseVariantsReadWhatBitConverterWrote() {
            var buffer = new byte[28];
            var writeSpan = buffer.AsSpan();

            Packer.WriteShort(ref writeSpan, -4321);
            Packer.WriteUShort(ref writeSpan, 64321);
            Packer.WriteInt(ref writeSpan, -987654321);
            Packer.WriteUInt(ref writeSpan, 4000000000u);
            Packer.WriteLong(ref writeSpan, -9876543210123L);
            Packer.WriteULong(ref writeSpan, 18000000000000000000ul);

            Assert.AreEqual(0, writeSpan.Length);

            ReadOnlySpan<byte> readSpan = buffer;

            Assert.AreEqual((short)-4321, Packer.ReadShortBitwise(ref readSpan));
            Assert.AreEqual((ushort)64321, Packer.ReadUShortBitwise(ref readSpan));
            Assert.AreEqual(-987654321, Packer.ReadIntBitwise(ref readSpan));
            Assert.AreEqual(4000000000u, Packer.ReadUIntBitwise(ref readSpan));
            Assert.AreEqual(-9876543210123L, Packer.ReadLongBitwise(ref readSpan));
            Assert.AreEqual(18000000000000000000ul, Packer.ReadULongBitwise(ref readSpan));
            Assert.AreEqual(0, readSpan.Length);
        }

        [Test]
        public void StringsRoundTripIncludingUnicode() {
            var unicode = Packer.FromBytes<TextPayload>(Packer.ToBytes(new TextPayload { Text = "Zażółć 🚀 jaźń" }));
            var empty = Packer.FromBytes<TextPayload>(Packer.ToBytes(new TextPayload { Text = string.Empty }));
            var nullText = Packer.FromBytes<TextPayload>(Packer.ToBytes(new TextPayload { Text = null }));

            Assert.AreEqual("Zażółć 🚀 jaźń", unicode.Text);
            Assert.AreEqual(string.Empty, empty.Text);
            Assert.AreEqual(string.Empty, nullText.Text);
        }

        [Test]
        public void StringWritesUtf8WithLengthPrefix() {
            const string text = "jaźń";
            var expected = Encoding.UTF8.GetBytes(text);
            var buffer = new byte[64];
            var writeSpan = buffer.AsSpan();

            Packer.WriteString(ref writeSpan, text);

            Assert.AreEqual(buffer.Length - sizeof(int) - expected.Length, writeSpan.Length);

            ReadOnlySpan<byte> readSpan = buffer;

            Assert.AreEqual(expected.Length, Packer.ReadInt(ref readSpan));
            Assert.AreEqual(expected, readSpan[..expected.Length].ToArray());
        }

        [Test]
        public void StringsRoundTripBackToBack() {
            var buffer = new byte[64];
            var writeSpan = buffer.AsSpan();

            Packer.WriteString(ref writeSpan, "first");
            Packer.WriteString(ref writeSpan, string.Empty);
            Packer.WriteString(ref writeSpan, "second");

            ReadOnlySpan<byte> readSpan = buffer;

            Assert.AreEqual("first", Packer.ReadString(ref readSpan));
            Assert.AreEqual(string.Empty, Packer.ReadString(ref readSpan));
            Assert.AreEqual("second", Packer.ReadString(ref readSpan));
        }

        [Test]
        public void PayloadLargerThanStackBufferRoundTrips() {
            var large = new string('x', 12000);

            var result = Packer.FromBytes<TextPayload>(Packer.ToBytes(new TextPayload { Text = large }));

            Assert.AreEqual(large, result.Text);

            var small = Packer.FromBytes<TextPayload>(Packer.ToBytes(new TextPayload { Text = "small" }));

            Assert.AreEqual("small", small.Text);
        }

        [Test]
        public void VectorAndQuaternionRoundTrip() {
            var buffer = new byte[36];
            var writeSpan = buffer.AsSpan();

            Packer.WriteVector3(ref writeSpan, new Vector3(1.5f, -2.5f, 3.5f));
            Packer.WriteVector2(ref writeSpan, new Vector2(-4.5f, 5.5f));
            Packer.WriteQuaternion(ref writeSpan, new Quaternion(0.1f, 0.2f, 0.3f, 0.4f));

            Assert.AreEqual(0, writeSpan.Length);

            ReadOnlySpan<byte> readSpan = buffer;

            Assert.AreEqual(new Vector3(1.5f, -2.5f, 3.5f), Packer.ReadVector3(ref readSpan));
            Assert.AreEqual(new Vector2(-4.5f, 5.5f), Packer.ReadVector2(ref readSpan));
            Assert.AreEqual(new Quaternion(0.1f, 0.2f, 0.3f, 0.4f), Packer.ReadQuaternion(ref readSpan));
            Assert.AreEqual(0, readSpan.Length);
        }

        [Test]
        public void VectorAndQuaternionBitwiseRoundTrip() {
            var buffer = new byte[36];
            var writeSpan = buffer.AsSpan();

            Packer.WriteVector3Bitwise(ref writeSpan, new Vector3(1.5f, -2.5f, 3.5f));
            Packer.WriteVector2Bitwise(ref writeSpan, new Vector2(-4.5f, 5.5f));
            Packer.WriteQuaternionBitwise(ref writeSpan, new Quaternion(0.1f, 0.2f, 0.3f, 0.4f));

            Assert.AreEqual(0, writeSpan.Length);

            ReadOnlySpan<byte> readSpan = buffer;

            Assert.AreEqual(new Vector3(1.5f, -2.5f, 3.5f), Packer.ReadVector3Bitwise(ref readSpan));
            Assert.AreEqual(new Vector2(-4.5f, 5.5f), Packer.ReadVector2Bitwise(ref readSpan));
            Assert.AreEqual(new Quaternion(0.1f, 0.2f, 0.3f, 0.4f), Packer.ReadQuaternionBitwise(ref readSpan));
            Assert.AreEqual(0, readSpan.Length);
        }

        [Test]
        public void VectorBitwiseMatchesBitConverterLayout() {
            var standard = new byte[36];
            var bitwise = new byte[36];
            var standardSpan = standard.AsSpan();
            var bitwiseSpan = bitwise.AsSpan();

            Packer.WriteVector3(ref standardSpan, new Vector3(1.5f, -2.5f, 3.5f));
            Packer.WriteVector2(ref standardSpan, new Vector2(-4.5f, 5.5f));
            Packer.WriteQuaternion(ref standardSpan, new Quaternion(0.1f, 0.2f, 0.3f, 0.4f));

            Packer.WriteVector3Bitwise(ref bitwiseSpan, new Vector3(1.5f, -2.5f, 3.5f));
            Packer.WriteVector2Bitwise(ref bitwiseSpan, new Vector2(-4.5f, 5.5f));
            Packer.WriteQuaternionBitwise(ref bitwiseSpan, new Quaternion(0.1f, 0.2f, 0.3f, 0.4f));

            Assert.AreEqual(standard, bitwise);
        }

        [Test]
        public void BytesRoundTripAndNullBecomesEmpty() {
            var buffer = new byte[64];
            var writeSpan = buffer.AsSpan();

            Packer.WriteBytes(ref writeSpan, new byte[] { 1, 2, 3 });
            Packer.WriteBytes(ref writeSpan, (byte[])null);

            ReadOnlySpan<byte> readSpan = buffer;

            Assert.AreEqual(new byte[] { 1, 2, 3 }, Packer.ReadBytes(ref readSpan));
            Assert.AreEqual(Array.Empty<byte>(), Packer.ReadBytes(ref readSpan));
        }

        [Test]
        public void BytesFromSpanRoundTripIncludingEmpty() {
            var buffer = new byte[64];
            var writeSpan = buffer.AsSpan();

            Packer.WriteBytes(ref writeSpan, (ReadOnlySpan<byte>)new byte[] { 4, 5, 6, 7 });
            Packer.WriteBytes(ref writeSpan, ReadOnlySpan<byte>.Empty);

            Assert.AreEqual(buffer.Length - (sizeof(int) + 4) - sizeof(int), writeSpan.Length);

            ReadOnlySpan<byte> readSpan = buffer;

            Assert.AreEqual(new byte[] { 4, 5, 6, 7 }, Packer.ReadBytes(ref readSpan));
            Assert.AreEqual(Array.Empty<byte>(), Packer.ReadBytes(ref readSpan));
        }

        [Test]
        public void IntListRoundTripAndNullBecomesEmpty() {
            var buffer = new byte[64];
            var writeSpan = buffer.AsSpan();

            Packer.PackIntList(ref writeSpan, new List<int> { 7, -8, 9 });
            Packer.PackIntList(ref writeSpan, null);

            ReadOnlySpan<byte> readSpan = buffer;

            Assert.AreEqual(new List<int> { 7, -8, 9 }, Packer.UnpackIntList(ref readSpan));
            Assert.AreEqual(new List<int>(), Packer.UnpackIntList(ref readSpan));
        }

        [Test]
        public void PackerListAndArrayRoundTripInPlace() {
            var buffer = new byte[128];
            var writeSpan = buffer.AsSpan();

            Packer.PackList(ref writeSpan, new List<TextPayload> { new() { Text = "one" }, new() { Text = "two" } });
            Packer.PackArray(ref writeSpan, new[] { new TextPayload { Text = "three" } });
            Packer.PackList(ref writeSpan, (List<TextPayload>)null);
            Packer.PackArray(ref writeSpan, (TextPayload[])null);

            ReadOnlySpan<byte> readSpan = buffer;

            var list = Packer.UnpackList<TextPayload>(ref readSpan);
            var array = Packer.UnpackArray<TextPayload>(ref readSpan);

            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("one", list[0].Text);
            Assert.AreEqual("two", list[1].Text);
            Assert.AreEqual(1, array.Length);
            Assert.AreEqual("three", array[0].Text);
            Assert.IsEmpty(Packer.UnpackList<TextPayload>(ref readSpan));
            Assert.IsEmpty(Packer.UnpackArray<TextPayload>(ref readSpan));
        }

        [Test]
        public void PackerListAndArrayRoundTrip() {
            var items = new List<TextPayload> {
                new() { Text = "first" },
                new() { Text = "second" }
            };

            var listResult = Packer.UnpackListFromBytes<TextPayload>(Packer.PackListToBytes(items));

            Assert.AreEqual(2, listResult.Count);
            Assert.AreEqual("first", listResult[0].Text);
            Assert.AreEqual("second", listResult[1].Text);

            var arrayResult = Packer.UnpackArrayFromBytes<TextPayload>(Packer.PackArrayToBytes(items.ToArray()));

            Assert.AreEqual(2, arrayResult.Length);
            Assert.AreEqual("first", arrayResult[0].Text);
            Assert.AreEqual("second", arrayResult[1].Text);

            Assert.IsEmpty(Packer.UnpackListFromBytes<TextPayload>(Packer.PackListToBytes<TextPayload>(null)));
            Assert.IsEmpty(Packer.UnpackArrayFromBytes<TextPayload>(Packer.PackArrayToBytes<TextPayload>(null)));
        }

        [Test]
        public void ListAndArrayBytesLargerThanStackBufferRoundTrip() {
            var items = new List<TextPayload>();

            for (var i = 0; i < 400; i++)
                items.Add(new TextPayload { Text = new string('y', 40) + i });

            var listResult = Packer.UnpackListFromBytes<TextPayload>(Packer.PackListToBytes(items));
            var arrayResult = Packer.UnpackArrayFromBytes<TextPayload>(Packer.PackArrayToBytes(items.ToArray()));

            Assert.AreEqual(items.Count, listResult.Count);
            Assert.AreEqual(items.Count, arrayResult.Length);
            Assert.AreEqual(items[399].Text, listResult[399].Text);
            Assert.AreEqual(items[399].Text, arrayResult[399].Text);
        }

        [Test]
        public void UnpackFromBytesTreatsNullAndEmptyAsEmpty() {
            Assert.IsEmpty(Packer.UnpackListFromBytes<TextPayload>(null));
            Assert.IsEmpty(Packer.UnpackListFromBytes<TextPayload>(Array.Empty<byte>()));
            Assert.IsEmpty(Packer.UnpackArrayFromBytes<TextPayload>(null));
            Assert.IsEmpty(Packer.UnpackArrayFromBytes<TextPayload>(Array.Empty<byte>()));
        }

        [Test]
        public void TransformDataRoundTrips() {
            var data = new TransformData(new Vector3(10f, 20f, 30f), new Vector3(0f, 90f, 45f), 2.5f);

            var result = Packer.FromBytes<TransformData>(Packer.ToBytes(data));

            Assert.AreEqual(data.Position, result.Position);
            Assert.AreEqual(data.Rotation, result.Rotation);
            Assert.AreEqual(data.Scale, result.Scale);
        }

        [Test]
        public void SmartToBytesTagsThePayloadAndUnpacksBack() {
            var bytes = Packer.SmartToBytes(new CounterState { Count = 42 });

            ReadOnlySpan<byte> span = bytes;

            Assert.AreEqual(SmartPackerRegistry.GetHash<CounterState>(), Packer.ReadUInt(ref span));
            Assert.AreEqual(sizeof(uint) + sizeof(int), bytes.Length);
            Assert.IsTrue(Packer.TrySmartUnpack<CounterState>(bytes, out var value));
            Assert.AreEqual(42, value.Count);
        }

        [Test]
        public void SmartToBytesUsesRuntimeTypeForReferenceTypes() {
            ISmartPacker packer = new LabelState { Label = "reference" };

            var bytes = Packer.SmartToBytes(packer);

            ReadOnlySpan<byte> span = bytes;

            Assert.AreEqual(SmartPackerRegistry.GetHash<LabelState>(), Packer.ReadUInt(ref span));
            Assert.IsTrue(Packer.TrySmartUnpack<LabelState>(bytes, out var value));
            Assert.AreEqual("reference", value.Label);
        }

        [Test]
        public void SmartToBytesThrowsOnNullReference() {
            Assert.Throws<ArgumentNullException>(() => Packer.SmartToBytes<ISmartPacker>(null));
        }

        [Test]
        public void TrySmartUnpackRejectsShortAndMismatchedPayloads() {
            var labelBytes = Packer.SmartToBytes(new LabelState { Label = "mismatch" });

            using (Log.SuspendSinks()) {
                Assert.IsFalse(Packer.TrySmartUnpack<CounterState>(null, out var fromNull));
                Assert.AreEqual(0, fromNull.Count);
                Assert.IsFalse(Packer.TrySmartUnpack<CounterState>(new byte[] { 1, 2, 3 }, out _));
                Assert.IsFalse(Packer.TrySmartUnpack<CounterState>(labelBytes, out _));
            }
        }

        [Test]
        public void SmartListRoundTripsPolymorphically() {
            var items = new List<ISmartPacker> {
                new CounterState { Count = 7 },
                new LabelState { Label = "mixed" },
                null,
                new CounterState { Count = -7 }
            };

            var bytes = Packer.SmartListToBytes(items);
            var result = Packer.SmartListFromBytes<ISmartPacker>(bytes);

            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(7, ((CounterState)result[0]).Count);
            Assert.AreEqual("mixed", ((LabelState)result[1]).Label);
            Assert.AreEqual(-7, ((CounterState)result[2]).Count);
        }

        [Test]
        public void SmartListRoundTripsInPlace() {
            var buffer = new byte[64];
            var writeSpan = buffer.AsSpan();

            Packer.WriteSmartList(ref writeSpan, new List<ISmartPacker> { new CounterState { Count = 3 } });
            Packer.WriteSmartList(ref writeSpan, (List<ISmartPacker>)null);

            ReadOnlySpan<byte> readSpan = buffer;

            var first = Packer.ReadSmartList<ISmartPacker>(ref readSpan);

            Assert.AreEqual(1, first.Count);
            Assert.AreEqual(3, ((CounterState)first[0]).Count);
            Assert.IsEmpty(Packer.ReadSmartList<ISmartPacker>(ref readSpan));
        }

        [Test]
        public void SmartListReadThrowsOnUnregisteredTag() {
            var buffer = new byte[16];
            var writeSpan = buffer.AsSpan();

            Packer.WriteInt(ref writeSpan, 1);
            Packer.WriteUInt(ref writeSpan, 123456789u);

            Assert.Throws<InvalidOperationException>(() => {
                ReadOnlySpan<byte> readSpan = buffer;
                Packer.ReadSmartList<ISmartPacker>(ref readSpan);
            });
        }

        [Test]
        public void SmartListFromBytesReturnsEmptyOnMissingOrMalformedData() {
            var malformed = new byte[8];
            var writeSpan = malformed.AsSpan();
            Packer.WriteInt(ref writeSpan, 1);
            Packer.WriteUInt(ref writeSpan, 987654321u);

            Assert.IsEmpty(Packer.SmartListToBytes<ISmartPacker>(null));
            Assert.IsEmpty(Packer.SmartListToBytes(new List<ISmartPacker>()));
            Assert.IsEmpty(Packer.SmartListFromBytes<ISmartPacker>(null));
            Assert.IsEmpty(Packer.SmartListFromBytes<ISmartPacker>(Array.Empty<byte>()));

            using (Log.SuspendSinks())
                Assert.IsEmpty(Packer.SmartListFromBytes<ISmartPacker>(malformed));
        }

        [Test]
        public void BuildPayloadReturnsOnlyTheBytesWritten() {
            var payload = Packer.BuildPayload((ref Span<byte> buffer) => {
                Packer.WriteInt(ref buffer, 42);
                Packer.WriteString(ref buffer, "core");
            });

            Assert.AreEqual(sizeof(int) + sizeof(int) + 4, payload.Length);

            ReadOnlySpan<byte> readSpan = payload;

            Assert.AreEqual(42, Packer.ReadInt(ref readSpan));
            Assert.AreEqual("core", Packer.ReadString(ref readSpan));
            Assert.AreEqual(0, readSpan.Length);
        }

        [Test]
        public void BuildPayloadGrowsBeyondTheStackBuffer() {
            var large = new string('z', 20000);

            var payload = Packer.BuildPayload((ref Span<byte> buffer) => Packer.WriteString(ref buffer, large));

            Assert.AreEqual(sizeof(int) + large.Length, payload.Length);

            ReadOnlySpan<byte> readSpan = payload;

            Assert.AreEqual(large, Packer.ReadString(ref readSpan));
        }

        [Test]
        public void AreBytesEqualHandlesNullEmptyAndContent() {
            Assert.IsTrue(Packer.AreBytesEqual(null, null));
            Assert.IsTrue(Packer.AreBytesEqual(null, Array.Empty<byte>()));
            Assert.IsTrue(Packer.AreBytesEqual(Array.Empty<byte>(), Array.Empty<byte>()));
            Assert.IsTrue(Packer.AreBytesEqual(new byte[] { 1, 2, 3 }, new byte[] { 1, 2, 3 }));
            Assert.IsFalse(Packer.AreBytesEqual(new byte[] { 1, 2, 3 }, new byte[] { 1, 2, 4 }));
            Assert.IsFalse(Packer.AreBytesEqual(new byte[] { 1, 2, 3 }, new byte[] { 1, 2 }));
            Assert.IsFalse(Packer.AreBytesEqual(null, new byte[] { 1 }));
        }
    }
}
