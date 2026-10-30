// Copyright 2026 Spellbound Studio Inc.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Spellbound.Core.Logging;
using UnityEngine;

namespace Spellbound.Core.Packing {
    public static class Packer {
        public delegate void PackWriter(ref Span<byte> buffer);

        private interface IPackStep {
            void Pack(ref Span<byte> buffer);
        }

        private readonly struct ValueStep<T> : IPackStep where T : IPacker {
            private readonly T _value;

            public ValueStep(T value) => _value = value;

            public void Pack(ref Span<byte> buffer) => _value.Pack(ref buffer);
        }

        private readonly struct WriterStep : IPackStep {
            private readonly PackWriter _writer;

            public WriterStep(PackWriter writer) => _writer = writer;

            public void Pack(ref Span<byte> buffer) => _writer(ref buffer);
        }

        private readonly struct ArrayStep<T> : IPackStep where T : IPacker {
            private readonly T[] _items;

            public ArrayStep(T[] items) => _items = items;

            public void Pack(ref Span<byte> buffer) => PackArray(ref buffer, _items);
        }

        private readonly struct ListStep<T> : IPackStep where T : IPacker {
            private readonly List<T> _items;

            public ListStep(List<T> items) => _items = items;

            public void Pack(ref Span<byte> buffer) => PackList(ref buffer, _items);
        }

        private readonly struct SmartStep<T> : IPackStep where T : ISmartPacker {
            private readonly T _value;
            private readonly uint _tag;

            public SmartStep(T value, uint tag) {
                _value = value;
                _tag = tag;
            }

            public void Pack(ref Span<byte> buffer) {
                WriteUInt(ref buffer, _tag);
                _value.Pack(ref buffer);
            }
        }

        private readonly struct SmartListStep<T> : IPackStep where T : ISmartPacker {
            private readonly IReadOnlyList<T> _items;

            public SmartListStep(IReadOnlyList<T> items) => _items = items;

            public void Pack(ref Span<byte> buffer) => WriteSmartList(ref buffer, _items);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteByte(ref Span<byte> buffer, byte value) {
            buffer[0] = value;
            buffer = buffer[1..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ReadByte(ref ReadOnlySpan<byte> buffer) {
            var value = buffer[0];
            buffer = buffer[1..];
            return value;
        }

        public static bool TryReadByte(ref ReadOnlySpan<byte> buffer, out byte value) {
            if (buffer.Length < 1) {
                value = 0;
                return false;
            }

            value = buffer[0];
            buffer = buffer[1..];
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSByte(ref Span<byte> buffer, sbyte value) {
            buffer[0] = unchecked((byte)value);
            buffer = buffer[1..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte ReadSByte(ref ReadOnlySpan<byte> buffer) {
            var value = unchecked((sbyte)buffer[0]);
            buffer = buffer[1..];
            return value;
        }

        public static bool TryReadSByte(ref ReadOnlySpan<byte> buffer, out sbyte value) {
            if (buffer.Length < 1) {
                value = 0;
                return false;
            }

            value = unchecked((sbyte)buffer[0]);
            buffer = buffer[1..];
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBool(ref Span<byte> buffer, bool value) {
            buffer[0] = value ? (byte)1 : (byte)0;
            buffer = buffer[1..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadBool(ref ReadOnlySpan<byte> buffer) {
            var value = buffer[0] != 0;
            buffer = buffer[1..];
            return value;
        }

        public static bool TryReadBool(ref ReadOnlySpan<byte> buffer, out bool value) {
            if (buffer.Length < 1) {
                value = false;
                return false;
            }

            value = buffer[0] != 0;
            buffer = buffer[1..];
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteShort(ref Span<byte> buffer, short value) {
            buffer[1] = (byte)(value >> 8);
            buffer[0] = (byte)value;
            buffer = buffer[sizeof(short)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadShort(ref ReadOnlySpan<byte> buffer) {
            var value = (short)(buffer[0] | (buffer[1] << 8));
            buffer = buffer[sizeof(short)..];
            return value;
        }

        public static bool TryReadShort(ref ReadOnlySpan<byte> buffer, out short value) {
            if (buffer.Length < sizeof(short)) {
                value = 0;
                return false;
            }

            value = ReadShort(ref buffer);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUShort(ref Span<byte> buffer, ushort value) {
            buffer[1] = (byte)(value >> 8);
            buffer[0] = (byte)value;
            buffer = buffer[sizeof(ushort)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUShort(ref ReadOnlySpan<byte> buffer) {
            var value = (ushort)(buffer[0] | (buffer[1] << 8));
            buffer = buffer[sizeof(ushort)..];
            return value;
        }

        public static bool TryReadUShort(ref ReadOnlySpan<byte> buffer, out ushort value) {
            if (buffer.Length < sizeof(ushort)) {
                value = 0;
                return false;
            }

            value = ReadUShort(ref buffer);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt(ref Span<byte> buffer, int value) {
            buffer[3] = (byte)(value >> 24);
            buffer[2] = (byte)(value >> 16);
            buffer[1] = (byte)(value >> 8);
            buffer[0] = (byte)value;
            buffer = buffer[sizeof(int)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt(ref ReadOnlySpan<byte> buffer) {
            var value = buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24);
            buffer = buffer[sizeof(int)..];
            return value;
        }

        public static bool TryReadInt(ref ReadOnlySpan<byte> buffer, out int value) {
            if (buffer.Length < sizeof(int)) {
                value = 0;
                return false;
            }

            value = ReadInt(ref buffer);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt(ref Span<byte> buffer, uint value) {
            buffer[3] = (byte)(value >> 24);
            buffer[2] = (byte)(value >> 16);
            buffer[1] = (byte)(value >> 8);
            buffer[0] = (byte)value;
            buffer = buffer[sizeof(uint)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUInt(ref ReadOnlySpan<byte> buffer) {
            var value = buffer[0] | ((uint)buffer[1] << 8) | ((uint)buffer[2] << 16) | ((uint)buffer[3] << 24);
            buffer = buffer[sizeof(uint)..];
            return value;
        }

        public static bool TryReadUInt(ref ReadOnlySpan<byte> buffer, out uint value) {
            if (buffer.Length < sizeof(uint)) {
                value = 0;
                return false;
            }

            value = ReadUInt(ref buffer);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLong(ref Span<byte> buffer, long value) {
            buffer[7] = (byte)(value >> 56);
            buffer[6] = (byte)(value >> 48);
            buffer[5] = (byte)(value >> 40);
            buffer[4] = (byte)(value >> 32);
            buffer[3] = (byte)(value >> 24);
            buffer[2] = (byte)(value >> 16);
            buffer[1] = (byte)(value >> 8);
            buffer[0] = (byte)value;
            buffer = buffer[sizeof(long)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadLong(ref ReadOnlySpan<byte> buffer) {
            var value = buffer[0] | ((long)buffer[1] << 8) | ((long)buffer[2] << 16) | ((long)buffer[3] << 24)
                        | ((long)buffer[4] << 32) | ((long)buffer[5] << 40) | ((long)buffer[6] << 48)
                        | ((long)buffer[7] << 56);
            buffer = buffer[sizeof(long)..];
            return value;
        }

        public static bool TryReadLong(ref ReadOnlySpan<byte> buffer, out long value) {
            if (buffer.Length < sizeof(long)) {
                value = 0;
                return false;
            }

            value = ReadLong(ref buffer);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteULong(ref Span<byte> buffer, ulong value) {
            buffer[7] = (byte)(value >> 56);
            buffer[6] = (byte)(value >> 48);
            buffer[5] = (byte)(value >> 40);
            buffer[4] = (byte)(value >> 32);
            buffer[3] = (byte)(value >> 24);
            buffer[2] = (byte)(value >> 16);
            buffer[1] = (byte)(value >> 8);
            buffer[0] = (byte)value;
            buffer = buffer[sizeof(ulong)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReadULong(ref ReadOnlySpan<byte> buffer) {
            var value = buffer[0] | ((ulong)buffer[1] << 8) | ((ulong)buffer[2] << 16) | ((ulong)buffer[3] << 24)
                        | ((ulong)buffer[4] << 32) | ((ulong)buffer[5] << 40) | ((ulong)buffer[6] << 48)
                        | ((ulong)buffer[7] << 56);
            buffer = buffer[sizeof(ulong)..];
            return value;
        }

        public static bool TryReadULong(ref ReadOnlySpan<byte> buffer, out ulong value) {
            if (buffer.Length < sizeof(ulong)) {
                value = 0;
                return false;
            }

            value = ReadULong(ref buffer);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFloat(ref Span<byte> buffer, float value) =>
            WriteInt(ref buffer, BitConverter.SingleToInt32Bits(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ReadFloat(ref ReadOnlySpan<byte> buffer) =>
            BitConverter.Int32BitsToSingle(ReadInt(ref buffer));

        public static bool TryReadFloat(ref ReadOnlySpan<byte> buffer, out float value) {
            if (buffer.Length < sizeof(float)) {
                value = 0f;
                return false;
            }

            value = ReadFloat(ref buffer);
            return true;
        }

        public static void WriteVarUInt(ref Span<byte> buffer, uint value) {
            if (!VarIntCodec.TryWrite(buffer, value, out var written)) {
                throw new ArgumentOutOfRangeException(nameof(buffer), "The buffer has no room for the varint.");
            }

            buffer = buffer[written..];
        }

        public static uint ReadVarUInt(ref ReadOnlySpan<byte> buffer) {
            if (!VarIntCodec.TryRead(buffer, out uint value, out var read)) {
                throw new ArgumentOutOfRangeException(nameof(buffer), "The buffer holds no complete varint.");
            }

            buffer = buffer[read..];
            return value;
        }

        public static bool TryReadVarUInt(ref ReadOnlySpan<byte> buffer, out uint value) {
            if (!VarIntCodec.TryRead(buffer, out value, out var read)) {
                return false;
            }

            buffer = buffer[read..];
            return true;
        }

        public static void WriteVarInt(ref Span<byte> buffer, int value) =>
            WriteVarUInt(ref buffer, VarIntCodec.ZigZag(value));

        public static int ReadVarInt(ref ReadOnlySpan<byte> buffer) => VarIntCodec.UnZigZag(ReadVarUInt(ref buffer));

        public static bool TryReadVarInt(ref ReadOnlySpan<byte> buffer, out int value) {
            var read = TryReadVarUInt(ref buffer, out var raw);
            value = read ? VarIntCodec.UnZigZag(raw) : 0;
            return read;
        }

        public static void WriteVarULong(ref Span<byte> buffer, ulong value) {
            if (!VarIntCodec.TryWrite(buffer, value, out var written)) {
                throw new ArgumentOutOfRangeException(nameof(buffer), "The buffer has no room for the varint.");
            }

            buffer = buffer[written..];
        }

        public static ulong ReadVarULong(ref ReadOnlySpan<byte> buffer) {
            if (!VarIntCodec.TryRead(buffer, out ulong value, out var read)) {
                throw new ArgumentOutOfRangeException(nameof(buffer), "The buffer holds no complete varint.");
            }

            buffer = buffer[read..];
            return value;
        }

        public static bool TryReadVarULong(ref ReadOnlySpan<byte> buffer, out ulong value) {
            if (!VarIntCodec.TryRead(buffer, out value, out var read)) {
                return false;
            }

            buffer = buffer[read..];
            return true;
        }

        public static void WriteVarLong(ref Span<byte> buffer, long value) =>
            WriteVarULong(ref buffer, VarIntCodec.ZigZag(value));

        public static long ReadVarLong(ref ReadOnlySpan<byte> buffer) =>
            VarIntCodec.UnZigZag(ReadVarULong(ref buffer));

        public static bool TryReadVarLong(ref ReadOnlySpan<byte> buffer, out long value) {
            var read = TryReadVarULong(ref buffer, out var raw);
            value = read ? VarIntCodec.UnZigZag(raw) : 0;
            return read;
        }

        public static int VarIntBytesOf(uint value) => VarIntCodec.BytesOf(value);

        public static int VarIntBytesOf(ulong value) => VarIntCodec.BytesOf(value);

        public static int VarIntBytesOf(int value) => VarIntCodec.BytesOf(VarIntCodec.ZigZag(value));

        public static int VarIntBytesOf(long value) => VarIntCodec.BytesOf(VarIntCodec.ZigZag(value));

        public static void WriteString(ref Span<byte> buffer, string value) {
            if (string.IsNullOrEmpty(value)) {
                WriteInt(ref buffer, 0);
                return;
            }

            var byteCount = Encoding.UTF8.GetByteCount(value);
            WriteInt(ref buffer, byteCount);
            Encoding.UTF8.GetBytes(value.AsSpan(), buffer);
            buffer = buffer[byteCount..];
        }

        public static string ReadString(ref ReadOnlySpan<byte> buffer) {
            var length = ReadInt(ref buffer);
            var value = Encoding.UTF8.GetString(buffer[..length]);
            buffer = buffer[length..];
            return value;
        }

        public static bool TryReadString(ref ReadOnlySpan<byte> buffer, int maximumBytes, out string value) {
            value = null;
            var remaining = buffer;

            if (!TryReadInt(ref remaining, out var length) || length < 0 || length > maximumBytes
                || length > remaining.Length) {
                return false;
            }

            value = length == 0 ? string.Empty : Encoding.UTF8.GetString(remaining[..length]);
            buffer = remaining[length..];
            return true;
        }

        public static bool TryReadString(ref ReadOnlySpan<byte> buffer, in DecodeLimits limits, out string value) =>
            TryReadString(ref buffer, limits.MaximumStringBytes, out value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteVector3(ref Span<byte> buffer, in Vector3 v) {
            WriteFloat(ref buffer, v.x);
            WriteFloat(ref buffer, v.y);
            WriteFloat(ref buffer, v.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ReadVector3(ref ReadOnlySpan<byte> buffer) {
            var x = ReadFloat(ref buffer);
            var y = ReadFloat(ref buffer);
            var z = ReadFloat(ref buffer);
            return new Vector3(x, y, z);
        }

        public static bool TryReadVector3(ref ReadOnlySpan<byte> buffer, out Vector3 value) {
            if (buffer.Length < sizeof(float) * 3) {
                value = default;
                return false;
            }

            value = ReadVector3(ref buffer);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteVector2(ref Span<byte> buffer, in Vector2 v) {
            WriteFloat(ref buffer, v.x);
            WriteFloat(ref buffer, v.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ReadVector2(ref ReadOnlySpan<byte> buffer) {
            var x = ReadFloat(ref buffer);
            var y = ReadFloat(ref buffer);
            return new Vector2(x, y);
        }

        public static bool TryReadVector2(ref ReadOnlySpan<byte> buffer, out Vector2 value) {
            if (buffer.Length < sizeof(float) * 2) {
                value = default;
                return false;
            }

            value = ReadVector2(ref buffer);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteQuaternion(ref Span<byte> buffer, in Quaternion q) {
            WriteFloat(ref buffer, q.x);
            WriteFloat(ref buffer, q.y);
            WriteFloat(ref buffer, q.z);
            WriteFloat(ref buffer, q.w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion ReadQuaternion(ref ReadOnlySpan<byte> buffer) {
            var x = ReadFloat(ref buffer);
            var y = ReadFloat(ref buffer);
            var z = ReadFloat(ref buffer);
            var w = ReadFloat(ref buffer);
            return new Quaternion(x, y, z, w);
        }

        public static bool TryReadQuaternion(ref ReadOnlySpan<byte> buffer, out Quaternion value) {
            if (buffer.Length < sizeof(float) * 4) {
                value = default;
                return false;
            }

            value = ReadQuaternion(ref buffer);
            return true;
        }

        public static void WriteBytes(ref Span<byte> buffer, byte[] data) {
            var length = data?.Length ?? 0;
            WriteInt(ref buffer, length);

            if (length <= 0) {
                return;
            }

            data.AsSpan().CopyTo(buffer);
            buffer = buffer[length..];
        }

        public static void WriteBytes(ref Span<byte> buffer, ReadOnlySpan<byte> data) {
            WriteInt(ref buffer, data.Length);
            data.CopyTo(buffer);
            buffer = buffer[data.Length..];
        }

        public static byte[] ReadBytes(ref ReadOnlySpan<byte> buffer) {
            var length = ReadInt(ref buffer);

            if (length == 0) {
                return Array.Empty<byte>();
            }

            var result = buffer[..length].ToArray();
            buffer = buffer[length..];
            return result;
        }

        public static bool TryReadBytes(ref ReadOnlySpan<byte> buffer, int maximumBytes, out byte[] value) {
            value = null;
            var remaining = buffer;

            if (!TryReadInt(ref remaining, out var length) || length < 0 || length > maximumBytes
                || length > remaining.Length) {
                return false;
            }

            value = length == 0 ? Array.Empty<byte>() : remaining[..length].ToArray();
            buffer = remaining[length..];
            return true;
        }

        public static bool TryReadBytes(ref ReadOnlySpan<byte> buffer, in DecodeLimits limits, out byte[] value) =>
            TryReadBytes(ref buffer, limits.MaximumByteRun, out value);

        public static void PackArray<T>(ref Span<byte> buffer, T[] items) where T : IPacker {
            var count = items?.Length ?? 0;
            WriteInt(ref buffer, count);

            if (items == null) {
                return;
            }

            foreach (var item in items) {
                item.Pack(ref buffer);
            }
        }

        public static T[] UnpackArray<T>(ref ReadOnlySpan<byte> buffer) where T : IPacker, new() {
            var count = ReadInt(ref buffer);

            if (count == 0) {
                return Array.Empty<T>();
            }

            if (count > buffer.Length) {
                var grown = new List<T>(buffer.Length);

                for (var index = 0; index < count; index++) {
                    var item = new T();
                    item.Unpack(ref buffer);
                    grown.Add(item);
                }

                return grown.ToArray();
            }

            var result = new T[count];

            for (var index = 0; index < count; index++) {
                var item = new T();
                item.Unpack(ref buffer);
                result[index] = item;
            }

            return result;
        }

        public static bool TryUnpackArray<T>(ref ReadOnlySpan<byte> buffer, int maximumCount, out T[] value)
            where T : IPacker, new() {
            value = null;
            var remaining = buffer;

            if (!TryReadInt(ref remaining, out var count) || count < 0 || count > maximumCount) {
                return false;
            }

            var result = new List<T>(Math.Min(count, remaining.Length));

            for (var index = 0; index < count; index++) {
                var item = new T();

                if (!TryUnpackItem(ref item, ref remaining)) {
                    return false;
                }

                result.Add(item);
            }

            value = count == 0 ? Array.Empty<T>() : result.ToArray();
            buffer = remaining;
            return true;
        }

        public static bool TryUnpackArray<T>(ref ReadOnlySpan<byte> buffer, in DecodeLimits limits, out T[] value)
            where T : IPacker, new() => TryUnpackArray(ref buffer, limits.MaximumCount, out value);

        public static void PackList<T>(ref Span<byte> buffer, List<T> items) where T : IPacker {
            var count = items?.Count ?? 0;
            WriteInt(ref buffer, count);

            if (items == null) {
                return;
            }

            foreach (var item in items) {
                item.Pack(ref buffer);
            }
        }

        public static List<T> UnpackList<T>(ref ReadOnlySpan<byte> buffer) where T : IPacker, new() {
            var count = ReadInt(ref buffer);

            if (count == 0) {
                return new List<T>();
            }

            var result = new List<T>(Math.Min(count, buffer.Length));

            for (var index = 0; index < count; index++) {
                var item = new T();
                item.Unpack(ref buffer);
                result.Add(item);
            }

            return result;
        }

        public static bool TryUnpackList<T>(ref ReadOnlySpan<byte> buffer, int maximumCount, out List<T> value)
            where T : IPacker, new() {
            value = null;
            var remaining = buffer;

            if (!TryReadInt(ref remaining, out var count) || count < 0 || count > maximumCount) {
                return false;
            }

            var result = new List<T>(Math.Min(count, remaining.Length));

            for (var index = 0; index < count; index++) {
                var item = new T();

                if (!TryUnpackItem(ref item, ref remaining)) {
                    return false;
                }

                result.Add(item);
            }

            value = result;
            buffer = remaining;
            return true;
        }

        public static bool TryUnpackList<T>(ref ReadOnlySpan<byte> buffer, in DecodeLimits limits, out List<T> value)
            where T : IPacker, new() => TryUnpackList(ref buffer, limits.MaximumCount, out value);

        public static void PackIntList(ref Span<byte> buffer, List<int> items) {
            var count = items?.Count ?? 0;
            WriteInt(ref buffer, count);

            if (items == null) {
                return;
            }

            foreach (var item in items) {
                WriteInt(ref buffer, item);
            }
        }

        public static List<int> UnpackIntList(ref ReadOnlySpan<byte> buffer) {
            var count = ReadInt(ref buffer);

            if (count == 0) {
                return new List<int>();
            }

            if (count < 0 || (long)count * sizeof(int) > buffer.Length) {
                throw new ArgumentOutOfRangeException(nameof(buffer), "The buffer holds fewer integers than its count.");
            }

            var result = new List<int>(count);

            for (var index = 0; index < count; index++) {
                result.Add(ReadInt(ref buffer));
            }

            return result;
        }

        public static bool TryUnpackIntList(ref ReadOnlySpan<byte> buffer, int maximumCount, out List<int> value) {
            value = null;
            var remaining = buffer;

            if (!TryReadInt(ref remaining, out var count) || count < 0 || count > maximumCount
                || (long)count * sizeof(int) > remaining.Length) {
                return false;
            }

            var result = new List<int>(count);

            for (var index = 0; index < count; index++) {
                result.Add(ReadInt(ref remaining));
            }

            value = result;
            buffer = remaining;
            return true;
        }

        public static bool TryUnpackIntList(ref ReadOnlySpan<byte> buffer, in DecodeLimits limits,
            out List<int> value) => TryUnpackIntList(ref buffer, limits.MaximumCount, out value);

        public static byte[] PackListToBytes<T>(List<T> items) where T : IPacker =>
            PackListToBytes(items, PackingLimits.Default);

        public static byte[] PackListToBytes<T>(List<T> items, in PackingLimits limits) where T : IPacker =>
            BuildBytes(new ListStep<T>(items), limits);

        public static List<T> UnpackListFromBytes<T>(byte[] bytes) where T : IPacker, new() {
            if (bytes == null || bytes.Length == 0) {
                return new List<T>();
            }

            ReadOnlySpan<byte> span = bytes;
            return UnpackList<T>(ref span);
        }

        public static byte[] SmartToBytes<T>(in T obj) where T : ISmartPacker =>
            SmartToBytes(in obj, PackingLimits.Default);

        public static byte[] SmartToBytes<T>(in T obj, in PackingLimits limits) where T : ISmartPacker {
            uint tag;

            if (typeof(T).IsValueType) {
                tag = SmartPackerRegistry.GetHash<T>();
            }
            else {
                if (obj == null) {
                    throw new ArgumentNullException(nameof(obj));
                }

                tag = SmartPackerRegistry.GetHash(obj.GetType());
            }

            return BuildBytes(new SmartStep<T>(obj, tag), limits);
        }

        public static bool TrySmartUnpack<T>(byte[] data, out T value) where T : ISmartPacker, new() {
            value = new T();

            if (data == null || data.Length < sizeof(uint)) {
                Log.Error($"SmartUnpack: '{typeof(T).Name}' payload is too small to hold a 4-byte type tag.");
                return false;
            }

            ReadOnlySpan<byte> span = data;
            var tag = ReadUInt(ref span);
            var expected = SmartPackerRegistry.GetHash<T>();

            if (tag != expected) {
                Log.Error($"SmartUnpack: tag {tag} is not '{typeof(T).Name}' ({expected}); " +
                          "payload is corrupt or keyed to a different type.");
                return false;
            }

            value.Unpack(ref span);
            return true;
        }

        public static void WriteSmartList<T>(ref Span<byte> buffer, IReadOnlyList<T> items) where T : ISmartPacker {
            var count = 0;

            if (items != null) {
                for (var index = 0; index < items.Count; index++) {
                    if (items[index] != null) {
                        count++;
                    }
                }
            }

            WriteInt(ref buffer, count);

            if (items == null) {
                return;
            }

            for (var index = 0; index < items.Count; index++) {
                var item = items[index];

                if (item == null) {
                    continue;
                }

                WriteUInt(ref buffer, item.Hash);
                item.Pack(ref buffer);
            }
        }

        public static List<T> ReadSmartList<T>(ref ReadOnlySpan<byte> buffer) where T : ISmartPacker {
            var count = ReadInt(ref buffer);

            if (count < 0 || (long)count * sizeof(uint) > buffer.Length) {
                throw new ArgumentOutOfRangeException(nameof(buffer), "The buffer holds fewer entries than its count.");
            }

            var result = new List<T>(count);

            for (var index = 0; index < count; index++) {
                var hash = ReadUInt(ref buffer);

                if (!SmartPackerRegistry.TryCreateInstance(hash, out var instance) || instance is not T typed) {
                    throw new InvalidOperationException(
                        $"SmartList: tag {hash} did not resolve to a registered {typeof(T).Name}.");
                }

                typed.Unpack(ref buffer);
                result.Add(typed);
            }

            return result;
        }

        public static byte[] SmartListToBytes<T>(IReadOnlyList<T> items) where T : ISmartPacker =>
            SmartListToBytes(items, PackingLimits.Default);

        public static byte[] SmartListToBytes<T>(IReadOnlyList<T> items, in PackingLimits limits)
            where T : ISmartPacker {
            if (items == null || items.Count == 0) {
                return Array.Empty<byte>();
            }

            return BuildBytes(new SmartListStep<T>(items), limits);
        }

        public static List<T> SmartListFromBytes<T>(byte[] bytes) where T : ISmartPacker {
            if (bytes == null || bytes.Length == 0) {
                return new List<T>();
            }

            try {
                ReadOnlySpan<byte> span = bytes;
                return ReadSmartList<T>(ref span);
            }
            catch (Exception e) {
                Log.Error($"SmartListFromBytes<{typeof(T).Name}>: malformed payload; returning empty. ({e.Message})");
                return new List<T>();
            }
        }

        public static byte[] ToBytes<T>(in T obj) where T : IPacker => ToBytes(in obj, PackingLimits.Default);

        public static byte[] ToBytes<T>(in T obj, in PackingLimits limits) where T : IPacker =>
            BuildBytes(new ValueStep<T>(obj), limits);

        public static T FromBytes<T>(ReadOnlySpan<byte> bytes) where T : IPacker, new() {
            var span = bytes;
            var obj = new T();
            obj.Unpack(ref span);
            return obj;
        }

        public static bool TryUnpack<T>(ReadOnlySpan<byte> bytes, out T value) where T : IPacker, new() {
            value = new T();
            var span = bytes;

            if (!TryUnpackItem(ref value, ref span) || span.Length != 0) {
                value = new T();
                return false;
            }

            return true;
        }

        public static bool TryPack<T>(in T value, Span<byte> destination, out int written) where T : IPacker {
            var span = destination;

            try {
                value.Pack(ref span);
            }
            catch (Exception e) when (IsOverflow(e)) {
                written = 0;
                return false;
            }

            written = destination.Length - span.Length;
            return true;
        }

        public static bool TryPack(PackWriter writer, Span<byte> destination, out int written) {
            if (writer == null) {
                throw new ArgumentNullException(nameof(writer));
            }

            var span = destination;

            try {
                writer(ref span);
            }
            catch (Exception e) when (IsOverflow(e)) {
                written = 0;
                return false;
            }

            written = destination.Length - span.Length;
            return true;
        }

        public static PooledPackedBytes PackPooled<T>(in T value) where T : IPacker =>
            PackPooled(in value, PackingLimits.Default);

        public static PooledPackedBytes PackPooled<T>(in T value, in PackingLimits limits) where T : IPacker =>
            RentBytes(new ValueStep<T>(value), limits);

        public static PooledPackedBytes PackPooled(PackWriter writer) => PackPooled(writer, PackingLimits.Default);

        public static PooledPackedBytes PackPooled(PackWriter writer, in PackingLimits limits) {
            if (writer == null) {
                throw new ArgumentNullException(nameof(writer));
            }

            return RentBytes(new WriterStep(writer), limits);
        }

        public static byte[] PackArrayToBytes<T>(T[] items) where T : IPacker =>
            PackArrayToBytes(items, PackingLimits.Default);

        public static byte[] PackArrayToBytes<T>(T[] items, in PackingLimits limits) where T : IPacker =>
            BuildBytes(new ArrayStep<T>(items), limits);

        public static T[] UnpackArrayFromBytes<T>(byte[] bytes) where T : IPacker, new() {
            if (bytes == null || bytes.Length == 0) {
                return Array.Empty<T>();
            }

            ReadOnlySpan<byte> span = bytes;
            return UnpackArray<T>(ref span);
        }

        public static bool AreBytesEqual(byte[] a, byte[] b) {
            if (a == null && b == null) {
                return true;
            }

            if (NoData(a) && NoData(b)) {
                return true;
            }

            if (a == null || b == null) {
                return false;
            }

            return a.AsSpan().SequenceEqual(b);

            static bool NoData(byte[] x) => x == null || x.Length == 0;
        }

        public static byte[] BuildPayload(PackWriter writer) => BuildPayload(writer, PackingLimits.Default);

        public static byte[] BuildPayload(PackWriter writer, in PackingLimits limits) {
            if (writer == null) {
                throw new ArgumentNullException(nameof(writer));
            }

            return BuildBytes(new WriterStep(writer), limits);
        }

        private static bool TryUnpackItem<T>(ref T item, ref ReadOnlySpan<byte> buffer) where T : IPacker {
            var span = buffer;

            try {
                item.Unpack(ref span);
            }
            catch (Exception e) when (IsOverflow(e) || e is InvalidOperationException || e is OverflowException) {
                return false;
            }

            buffer = span;
            return true;
        }

        private static bool IsOverflow(Exception e) =>
            e is IndexOutOfRangeException || (e is ArgumentException && e is not ArgumentNullException);

        private static byte[] BuildBytes<TStep>(in TStep step, in PackingLimits limits)
            where TStep : struct, IPackStep {
            if (limits.FirstRentBytes > limits.StackBufferBytes) {
                Span<byte> stack = stackalloc byte[limits.StackBufferBytes];
                var span = stack;

                try {
                    step.Pack(ref span);
                    return stack[..(stack.Length - span.Length)].ToArray();
                }
                catch (Exception e) when (IsOverflow(e)) {
                }
            }

            using var rented = RentBytes(step, limits);
            return rented.Bytes.ToArray();
        }

        private static PooledPackedBytes RentBytes<TStep>(in TStep step, in PackingLimits limits)
            where TStep : struct, IPackStep {
            var size = Math.Min(limits.FirstRentBytes, limits.MaximumPayloadBytes);

            while (true) {
                var rented = ArrayPool<byte>.Shared.Rent(size);
                var capacity = Math.Min(rented.Length, limits.MaximumPayloadBytes);
                var span = rented.AsSpan(0, capacity);

                try {
                    step.Pack(ref span);
                    return new PooledPackedBytes(rented, capacity - span.Length);
                }
                catch (Exception e) when (IsOverflow(e)) {
                    ArrayPool<byte>.Shared.Return(rented);
                }

                if (size >= limits.MaximumPayloadBytes) {
                    throw new InvalidOperationException(
                        $"Payload exceeds the largest allowed payload of {limits.MaximumPayloadBytes} bytes.");
                }

                size = (int)Math.Min((long)size * 2, limits.MaximumPayloadBytes);
            }
        }
    }
}
