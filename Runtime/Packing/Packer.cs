// Copyright 2026 Spellbound Studio Inc.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Spellbound.Core.Logging;
using UnityEngine;

namespace Spellbound.Core.Packing {
    /// <summary>
    /// Static binary serialization utility using Span-based APIs.
    /// Provides both BitConverter and bitwise methods for flexibility.
    /// Requires IPacker implementers.
    /// </summary>
    public static class Packer {
        public delegate void PackWriter(ref Span<byte> buffer);

        // Starting stack buffer size
        private const int StackBufferSize = 4096;

        // Max heap buffer allowed: 64 MB - tune as needed
        private const int MaxRentedBuffer = 1024 * 1024 * 64;

        private static int _payloadSizeHint = StackBufferSize;

        private static void RecordPayloadSize(int written) =>
                _payloadSizeHint = written > StackBufferSize ? written : StackBufferSize;

        #region Byte

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

        #endregion
        
        #region SByte

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

        #endregion SByte

        #region Bool

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

        #endregion

        #region Int

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt(ref Span<byte> buffer, int value) {
            BitConverter.TryWriteBytes(buffer, value);
            buffer = buffer[sizeof(int)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt(ref ReadOnlySpan<byte> buffer) {
            var value = BitConverter.ToInt32(buffer);
            buffer = buffer[sizeof(int)..];

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteIntBitwise(ref Span<byte> buffer, int value) {
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 16);
            buffer[3] = (byte)(value >> 24);
            buffer = buffer[4..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadIntBitwise(ref ReadOnlySpan<byte> buffer) {
            var value = buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24);
            buffer = buffer[4..];

            return value;
        }

        #endregion

        #region Floats

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFloat(ref Span<byte> buffer, float value) {
            BitConverter.TryWriteBytes(buffer, value);
            buffer = buffer[sizeof(float)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ReadFloat(ref ReadOnlySpan<byte> buffer) {
            var value = BitConverter.ToSingle(buffer);
            buffer = buffer[sizeof(float)..];

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFloatBitwise(ref Span<byte> buffer, float value) {
            var intVal = BitConverter.SingleToInt32Bits(value);
            WriteIntBitwise(ref buffer, intVal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ReadFloatBitwise(ref ReadOnlySpan<byte> buffer) {
            var intVal = ReadIntBitwise(ref buffer);

            return BitConverter.Int32BitsToSingle(intVal);
        }

        #endregion

        #region UInt

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt(ref Span<byte> buffer, uint value) {
            BitConverter.TryWriteBytes(buffer, value);
            buffer = buffer[sizeof(uint)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUInt(ref ReadOnlySpan<byte> buffer) {
            var value = BitConverter.ToUInt32(buffer);
            buffer = buffer[sizeof(uint)..];

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUIntBitwise(ref Span<byte> buffer, uint value) {
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 16);
            buffer[3] = (byte)(value >> 24);
            buffer = buffer[4..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUIntBitwise(ref ReadOnlySpan<byte> buffer) {
            var value = buffer[0]
                        | ((uint)buffer[1] << 8)
                        | ((uint)buffer[2] << 16)
                        | ((uint)buffer[3] << 24);
            buffer = buffer[4..];

            return value;
        }

        #endregion

        #region ULong

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteULong(ref Span<byte> buffer, ulong value) {
            BitConverter.TryWriteBytes(buffer, value);
            buffer = buffer[sizeof(ulong)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReadULong(ref ReadOnlySpan<byte> buffer) {
            var value = BitConverter.ToUInt64(buffer);
            buffer = buffer[sizeof(ulong)..];

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteULongBitwise(ref Span<byte> buffer, ulong value) {
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 16);
            buffer[3] = (byte)(value >> 24);
            buffer[4] = (byte)(value >> 32);
            buffer[5] = (byte)(value >> 40);
            buffer[6] = (byte)(value >> 48);
            buffer[7] = (byte)(value >> 56);
            buffer = buffer[8..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReadULongBitwise(ref ReadOnlySpan<byte> buffer) {
            var value = buffer[0]
                        | ((ulong)buffer[1] << 8)
                        | ((ulong)buffer[2] << 16)
                        | ((ulong)buffer[3] << 24)
                        | ((ulong)buffer[4] << 32)
                        | ((ulong)buffer[5] << 40)
                        | ((ulong)buffer[6] << 48)
                        | ((ulong)buffer[7] << 56);
            buffer = buffer[8..];

            return value;
        }

        #endregion

        #region Long

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLong(ref Span<byte> buffer, long value) {
            BitConverter.TryWriteBytes(buffer, value);
            buffer = buffer[sizeof(long)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadLong(ref ReadOnlySpan<byte> buffer) {
            var value = BitConverter.ToInt64(buffer);
            buffer = buffer[sizeof(long)..];

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLongBitwise(ref Span<byte> buffer, long value) {
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 16);
            buffer[3] = (byte)(value >> 24);
            buffer[4] = (byte)(value >> 32);
            buffer[5] = (byte)(value >> 40);
            buffer[6] = (byte)(value >> 48);
            buffer[7] = (byte)(value >> 56);
            buffer = buffer[8..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadLongBitwise(ref ReadOnlySpan<byte> buffer) {
            var value = buffer[0]
                        | ((long)buffer[1] << 8)
                        | ((long)buffer[2] << 16)
                        | ((long)buffer[3] << 24)
                        | ((long)buffer[4] << 32)
                        | ((long)buffer[5] << 40)
                        | ((long)buffer[6] << 48)
                        | ((long)buffer[7] << 56);
            buffer = buffer[8..];

            return value;
        }

        #endregion

        #region Short

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteShort(ref Span<byte> buffer, short value) {
            BitConverter.TryWriteBytes(buffer, value);
            buffer = buffer[sizeof(short)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadShort(ref ReadOnlySpan<byte> buffer) {
            var value = BitConverter.ToInt16(buffer);
            buffer = buffer[sizeof(short)..];

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteShortBitwise(ref Span<byte> buffer, short value) {
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer = buffer[2..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadShortBitwise(ref ReadOnlySpan<byte> buffer) {
            var value = (short)(buffer[0]
                                | (buffer[1] << 8));
            buffer = buffer[2..];

            return value;
        }

        #endregion
        
        #region UShort

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUShort(ref Span<byte> buffer, ushort value) {
            BitConverter.TryWriteBytes(buffer, value);
            buffer = buffer[sizeof(ushort)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUShort(ref ReadOnlySpan<byte> buffer) {
            var value = BitConverter.ToUInt16(buffer);
            buffer = buffer[sizeof(ushort)..];

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUShortBitwise(ref Span<byte> buffer, ushort value) {
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer = buffer[2..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUShortBitwise(ref ReadOnlySpan<byte> buffer) {
            var value = (ushort)(buffer[0] | (buffer[1] << 8));
            buffer = buffer[2..];

            return value;
        }

        #endregion

        #region Strings

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
            var len = ReadInt(ref buffer);
            var value = Encoding.UTF8.GetString(buffer[..len]);
            buffer = buffer[len..];

            return value;
        }

        #endregion

        #region Vector3

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteVector3Bitwise(ref Span<byte> buffer, in Vector3 v) {
            WriteFloatBitwise(ref buffer, v.x);
            WriteFloatBitwise(ref buffer, v.y);
            WriteFloatBitwise(ref buffer, v.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ReadVector3Bitwise(ref ReadOnlySpan<byte> buffer) {
            var x = ReadFloatBitwise(ref buffer);
            var y = ReadFloatBitwise(ref buffer);
            var z = ReadFloatBitwise(ref buffer);

            return new Vector3(x, y, z);
        }

        #endregion

        #region Vector2

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteVector2Bitwise(ref Span<byte> buffer, in Vector2 v) {
            WriteFloatBitwise(ref buffer, v.x);
            WriteFloatBitwise(ref buffer, v.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ReadVector2Bitwise(ref ReadOnlySpan<byte> buffer) {
            var x = ReadFloatBitwise(ref buffer);
            var y = ReadFloatBitwise(ref buffer);

            return new Vector2(x, y);
        }

        #endregion

        #region Quaternion

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteQuaternionBitwise(ref Span<byte> buffer, in Quaternion q) {
            WriteFloatBitwise(ref buffer, q.x);
            WriteFloatBitwise(ref buffer, q.y);
            WriteFloatBitwise(ref buffer, q.z);
            WriteFloatBitwise(ref buffer, q.w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion ReadQuaternionBitwise(ref ReadOnlySpan<byte> buffer) {
            var x = ReadFloatBitwise(ref buffer);
            var y = ReadFloatBitwise(ref buffer);
            var z = ReadFloatBitwise(ref buffer);
            var w = ReadFloatBitwise(ref buffer);

            return new Quaternion(x, y, z, w);
        }

        #endregion

        #region Byte Arrays

        /// <summary>
        /// Write a byte array with a length prefix (null treated as empty)
        /// </summary>
        public static void WriteBytes(ref Span<byte> buffer, byte[] data) {
            var len = data?.Length ?? 0;
            WriteInt(ref buffer, len);

            if (len <= 0)
                return;

            data.AsSpan().CopyTo(buffer);
            buffer = buffer[len..];
        }

        /// <summary>
        /// Write a ReadOnlySpan with a length prefix
        /// </summary>
        public static void WriteBytes(ref Span<byte> buffer, ReadOnlySpan<byte> data) {
            WriteInt(ref buffer, data.Length);
            data.CopyTo(buffer);
            buffer = buffer[data.Length..];
        }

        /// <summary>
        /// Read a length-prefixed byte array that never returns null and returns an empty array instead.
        /// </summary>
        public static byte[] ReadBytes(ref ReadOnlySpan<byte> buffer) {
            var len = ReadInt(ref buffer);

            if (len == 0)
                return Array.Empty<byte>();

            var result = buffer[..len].ToArray();
            buffer = buffer[len..];

            return result;
        }

        #endregion

        #region Array Packing

        /// <summary>
        /// Pack an array of IPacker items with a length prefix with a null treated as empty.
        /// </summary>
        public static void PackArray<T>(ref Span<byte> buffer, T[] items) where T : IPacker {
            var count = items?.Length ?? 0;
            WriteInt(ref buffer, count);

            if (items == null)
                return;

            foreach (var item in items)
                item.Pack(ref buffer);
        }

        /// <summary>
        /// Unpack an array of IPacker items if empty returns a null array.
        /// </summary>
        public static T[] UnpackArray<T>(ref ReadOnlySpan<byte> buffer) where T : IPacker, new() {
            var count = ReadInt(ref buffer);

            if (count == 0)
                return Array.Empty<T>();

            var result = new T[count];

            for (var i = 0; i < count; i++) {
                var item = new T();
                item.Unpack(ref buffer);
                result[i] = item;
            }

            return result;
        }

        #endregion

        #region List Packing

        /// <summary>
        /// Pack a List of IPacker items with length prefix (null treated as empty)
        /// </summary>
        public static void PackList<T>(ref Span<byte> buffer, List<T> items) where T : IPacker {
            var count = items?.Count ?? 0;
            WriteInt(ref buffer, count);

            if (items == null)
                return;

            foreach (var item in items)
                item.Pack(ref buffer);
        }

        /// <summary>
        /// Unpack a List of IPacker items and returns an empty list instead of null.
        /// </summary>
        public static List<T> UnpackList<T>(ref ReadOnlySpan<byte> buffer) where T : IPacker, new() {
            var count = ReadInt(ref buffer);

            if (count == 0)
                return new List<T>();

            var result = new List<T>(count);

            for (var i = 0; i < count; i++) {
                var item = new T();
                item.Unpack(ref buffer);
                result.Add(item);
            }

            return result;
        }

        /// <summary>
        /// Pack a List of int with length prefix (null treated as empty)
        /// </summary>
        public static void PackIntList(ref Span<byte> buffer, List<int> items) {
            var count = items?.Count ?? 0;
            WriteInt(ref buffer, count);

            if (items == null)
                return;

            foreach (var item in items)
                WriteInt(ref buffer, item);
        }

        /// <summary>
        /// Unpack a List of int and returns an empty list if empty.
        /// </summary>
        public static List<int> UnpackIntList(ref ReadOnlySpan<byte> buffer) {
            var count = ReadInt(ref buffer);

            if (count == 0)
                return new List<int>();

            var result = new List<int>(count);

            for (var i = 0; i < count; i++)
                result.Add(ReadInt(ref buffer));

            return result;
        }

        /// <summary>
        /// Pack a List directly to bytes where null is empty.
        /// </summary>
        public static byte[] PackListToBytes<T>(List<T> items) where T : IPacker {
            var count = items?.Count ?? 0;

            // Handle empty/null case
            if (count == 0) {
                Span<byte> emptyBuf = stackalloc byte[4];
                var emptySpan = emptyBuf;
                WriteInt(ref emptySpan, 0);

                return emptyBuf.ToArray();
            }

            if (_payloadSizeHint <= StackBufferSize) {
                Span<byte> stackBuf = stackalloc byte[StackBufferSize];
                var span = stackBuf;

                try {
                    PackList(ref span, items);
                    var written = stackBuf.Length - span.Length;

                    return stackBuf[..written].ToArray();
                }
                catch (Exception e) when (e is ArgumentException or IndexOutOfRangeException) { }
            }

            // Need larger buffer
            var size = Math.Max(_payloadSizeHint, 8192);

            while (true) {
                var rented = ArrayPool<byte>.Shared.Rent(size);

                try {
                    var rentedSpan = new Span<byte>(rented, 0, size);
                    var working = rentedSpan;

                    try {
                        PackList(ref working, items);
                        var written = size - working.Length;

                        var result = new byte[written];
                        Buffer.BlockCopy(rented, 0, result, 0, written);
                        RecordPayloadSize(written);

                        return result;
                    }
                    catch (Exception e) when (e is ArgumentException or IndexOutOfRangeException) { }
                }
                finally {
                    ArrayPool<byte>.Shared.Return(rented);
                }

                if (size >= MaxRentedBuffer)
                    throw new InvalidOperationException(
                        $"List payload exceeds maximum buffer size of {MaxRentedBuffer} bytes");

                size = Math.Min(size * 2, MaxRentedBuffer);
            }
        }

        /// <summary>
        /// Unpack a List directly from bytes (convenience method, never returns null)
        /// </summary>
        public static List<T> UnpackListFromBytes<T>(byte[] bytes) where T : IPacker, new() {
            if (bytes == null || bytes.Length == 0)
                return new List<T>();

            ReadOnlySpan<byte> span = bytes;

            return UnpackList<T>(ref span);
        }

        #endregion

        #region High-Level Helpers

        /// <summary>
        /// Serialize an ISmartPacker into a byte[] led by its 4-byte stable type tag, so the payload can be
        /// reconstructed without knowing the type first. A struct resolves its tag at JIT time and never boxes;
        /// the returned array is the only allocation. An interface- or class-typed argument resolves its tag
        /// from its runtime type instead.
        /// </summary>
        public static byte[] SmartToBytes<T>(in T obj) where T : ISmartPacker {
            uint tag;

            if (typeof(T).IsValueType)
                tag = SmartPackerRegistry.GetHash<T>();
            else {
                if (obj == null)
                    throw new ArgumentNullException(nameof(obj));

                tag = SmartPackerRegistry.GetHash(obj.GetType());
            }

            if (_payloadSizeHint <= StackBufferSize) {
                Span<byte> stackBuf = stackalloc byte[StackBufferSize];
                var span = stackBuf;

                try {
                    WriteUInt(ref span, tag);
                    obj.Pack(ref span);
                    var written = stackBuf.Length - span.Length;

                    return stackBuf[..written].ToArray();
                }
                catch (Exception e) when (e is ArgumentException or IndexOutOfRangeException) { }
            }

            var size = Math.Max(_payloadSizeHint, 8192);

            while (true) {
                var rented = ArrayPool<byte>.Shared.Rent(size);

                try {
                    var rentedSpan = new Span<byte>(rented, 0, size);
                    var working = rentedSpan;

                    try {
                        WriteUInt(ref working, tag);
                        obj.Pack(ref working);
                        var written = size - working.Length;
                        var result = new byte[written];
                        Buffer.BlockCopy(rented, 0, result, 0, written);
                        RecordPayloadSize(written);

                        return result;
                    }
                    catch (Exception e) when (e is ArgumentException or IndexOutOfRangeException) { }
                }
                finally {
                    ArrayPool<byte>.Shared.Return(rented);
                }

                if (size >= MaxRentedBuffer)
                    throw new InvalidOperationException(
                        $"Payload exceeds maximum buffer size of {MaxRentedBuffer} bytes");

                size = Math.Min(size * 2, MaxRentedBuffer);
            }
        }

        /// <summary>
        /// Deserialize a payload written by <see cref="SmartToBytes{T}"/> whose type the caller already knows,
        /// validating the leading tag against T. Returns false with an error log on null/short data or a tag
        /// that is not T's; value is always a fresh T. A struct unpacks in place with zero heap allocation.
        /// </summary>
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

        /// <summary>
        /// Serialize a polymorphic list of ISmartPackers into <paramref name="buffer"/>: a count of the
        /// non-null entries, then each entry's 4-byte type tag (its <see cref="IRegistryEntry.Hash"/>) and
        /// packed state. The tag comes from each entry's runtime type, so a list typed as a base round-trips
        /// its concrete entries.
        /// </summary>
        public static void WriteSmartList<T>(ref Span<byte> buffer, IReadOnlyList<T> items) where T : ISmartPacker {
            var count = 0;

            if (items != null) {
                for (var i = 0; i < items.Count; i++) {
                    if (items[i] != null)
                        count++;
                }
            }

            WriteInt(ref buffer, count);

            if (items == null)
                return;

            for (var i = 0; i < items.Count; i++) {
                var item = items[i];

                if (item == null)
                    continue;

                WriteUInt(ref buffer, item.Hash);
                item.Pack(ref buffer);
            }
        }

        /// <summary>
        /// Deserialize a list written by <see cref="WriteSmartList{T}"/>, reconstructing each entry's concrete
        /// type through <see cref="SmartPackerRegistry"/>. Throws on an unregistered tag or an entry whose type
        /// is not a <typeparamref name="T"/> — an entry's byte length is only known once its type resolves, so a
        /// miss cannot be skipped.
        /// </summary>
        public static List<T> ReadSmartList<T>(ref ReadOnlySpan<byte> buffer) where T : ISmartPacker {
            var count = ReadInt(ref buffer);
            var result = new List<T>(count);

            for (var i = 0; i < count; i++) {
                var hash = ReadUInt(ref buffer);

                if (!SmartPackerRegistry.TryCreateInstance(hash, out var instance) || instance is not T typed)
                    throw new InvalidOperationException(
                        $"SmartList: tag {hash} did not resolve to a registered {typeof(T).Name}.");

                typed.Unpack(ref buffer);
                result.Add(typed);
            }

            return result;
        }

        /// <summary>
        /// Pack a polymorphic ISmartPacker list directly to a byte[]; null/empty yields an empty array.
        /// </summary>
        public static byte[] SmartListToBytes<T>(IReadOnlyList<T> items) where T : ISmartPacker {
            if (items == null || items.Count == 0)
                return Array.Empty<byte>();

            return BuildPayload((ref Span<byte> buffer) => WriteSmartList(ref buffer, items));
        }

        /// <summary>
        /// Unpack a polymorphic ISmartPacker list from a byte[], returning an empty list on null/empty or
        /// malformed data (a corrupt or stale tag yields empty rather than throwing to the caller).
        /// </summary>
        public static List<T> SmartListFromBytes<T>(byte[] bytes) where T : ISmartPacker {
            if (bytes == null || bytes.Length == 0)
                return new List<T>();

            try {
                ReadOnlySpan<byte> span = bytes;

                return ReadSmartList<T>(ref span);
            }
            catch (Exception e) {
                Log.Error($"SmartListFromBytes<{typeof(T).Name}>: malformed payload; returning empty. ({e.Message})");

                return new List<T>();
            }
        }

        /// <summary>
        /// Serialize a value-type IPacker into a byte[].
        /// Uses stack buffer for small payloads, ArrayPool for larger ones.
        /// </summary>
        public static byte[] ToBytes<T>(in T obj) where T : IPacker {
            // Try stack buffer first for small payloads
            if (_payloadSizeHint <= StackBufferSize) {
                Span<byte> stackBuf = stackalloc byte[StackBufferSize];
                var span = stackBuf;

                try {
                    obj.Pack(ref span);
                    var written = stackBuf.Length - span.Length;

                    return stackBuf[..written].ToArray();
                }
                catch (Exception e) when (e is ArgumentException or IndexOutOfRangeException) { }
            }

            // Payload too large, use ArrayPool with exponential growth
            var size = Math.Max(_payloadSizeHint, 8192);

            while (true) {
                var rented = ArrayPool<byte>.Shared.Rent(size);

                try {
                    var rentedSpan = new Span<byte>(rented, 0, size);
                    var working = rentedSpan;

                    try {
                        obj.Pack(ref working);
                        var written = size - working.Length;

                        var result = new byte[written];
                        Buffer.BlockCopy(rented, 0, result, 0, written);
                        RecordPayloadSize(written);

                        return result;
                    }
                    catch (Exception e) when (e is ArgumentException or IndexOutOfRangeException) { }
                }
                finally {
                    ArrayPool<byte>.Shared.Return(rented);
                }

                if (size >= MaxRentedBuffer)
                    throw new InvalidOperationException(
                        $"Payload exceeds maximum buffer size of {MaxRentedBuffer} bytes");

                size = Math.Min(size * 2, MaxRentedBuffer);
            }
        }

        /// <summary>
        /// Deserialize a struct that implements IPacker from a byte array or ReadOnlySpan
        /// </summary>
        public static T FromBytes<T>(ReadOnlySpan<byte> bytes) where T : IPacker, new() {
            var span = bytes;
            var obj = new T();
            obj.Unpack(ref span);

            return obj;
        }

        /// <summary>
        /// Pack an array directly to bytes where null is empty.
        /// </summary>
        public static byte[] PackArrayToBytes<T>(T[] items) where T : IPacker {
            var count = items?.Length ?? 0;

            // Handle empty/null case
            if (count == 0) {
                Span<byte> emptyBuf = stackalloc byte[4];
                var emptySpan = emptyBuf;
                WriteInt(ref emptySpan, 0);

                return emptyBuf.ToArray();
            }

            if (_payloadSizeHint <= StackBufferSize) {
                Span<byte> stackBuf = stackalloc byte[StackBufferSize];
                var span = stackBuf;

                try {
                    PackArray(ref span, items);
                    var written = stackBuf.Length - span.Length;

                    return stackBuf[..written].ToArray();
                }
                catch (Exception e) when (e is ArgumentException or IndexOutOfRangeException) { }
            }

            // Need larger buffer
            var size = Math.Max(_payloadSizeHint, 8192);

            while (true) {
                var rented = ArrayPool<byte>.Shared.Rent(size);

                try {
                    var rentedSpan = new Span<byte>(rented, 0, size);
                    var working = rentedSpan;

                    try {
                        PackArray(ref working, items);
                        var written = size - working.Length;

                        var result = new byte[written];
                        Buffer.BlockCopy(rented, 0, result, 0, written);
                        RecordPayloadSize(written);

                        return result;
                    }
                    catch (Exception e) when (e is ArgumentException or IndexOutOfRangeException) { }
                }
                finally {
                    ArrayPool<byte>.Shared.Return(rented);
                }

                if (size >= MaxRentedBuffer)
                    throw new InvalidOperationException(
                        $"Array payload exceeds maximum buffer size of {MaxRentedBuffer} bytes");

                size = Math.Min(size * 2, MaxRentedBuffer);
            }
        }

        /// <summary>
        /// Unpack an array directly from bytes (convenience method, never returns null)
        /// </summary>
        public static T[] UnpackArrayFromBytes<T>(byte[] bytes) where T : IPacker, new() {
            if (bytes == null || bytes.Length == 0)
                return Array.Empty<T>();

            ReadOnlySpan<byte> span = bytes;

            return UnpackArray<T>(ref span);
        }

        /// <summary>
        /// Equality check for packed item data.
        /// </summary>
        public static bool AreBytesEqual(byte[] a, byte[] b) {
            if (a == null && b == null)
                return true;

            // If the byte array is null or empty return true.
            // TODO: This potentially has implications on craftable material items but beyond scope for now.
            if (NoData(a) && NoData(b))
                return true;

            if (a == null || b == null)
                return false;

            return a.AsSpan().SequenceEqual(b);

            // Helper method that returns a bool if the byte array is null or empty.
            static bool NoData(byte[] x) => x == null || x.Length == 0;
        }

        /// <summary>
        /// Handles the stack-first + ArrayPool fallback pattern.
        /// Caller provides only the write logic.
        /// </summary>
        public static byte[] BuildPayload(PackWriter writer) {
            if (_payloadSizeHint <= StackBufferSize) {
                Span<byte> stackBuf = stackalloc byte[StackBufferSize];
                var span = stackBuf;

                try {
                    writer(ref span);
                    var written = stackBuf.Length - span.Length;

                    return stackBuf[..written].ToArray();
                }
                catch (Exception e) when (e is ArgumentException or IndexOutOfRangeException) { }
            }

            var size = Math.Max(_payloadSizeHint, 8192);

            while (true) {
                var rented = ArrayPool<byte>.Shared.Rent(size);

                try {
                    var rentedSpan = new Span<byte>(rented, 0, size);
                    var working = rentedSpan;

                    try {
                        writer(ref working);
                        var written = size - working.Length;
                        var result = new byte[written];
                        Buffer.BlockCopy(rented, 0, result, 0, written);
                        RecordPayloadSize(written);

                        return result;
                    }
                    catch (Exception e) when (e is ArgumentException or IndexOutOfRangeException) { }
                }
                finally {
                    ArrayPool<byte>.Shared.Return(rented);
                }

                if (size >= MaxRentedBuffer)
                    throw new InvalidOperationException(
                        $"Payload exceeds maximum buffer size of {MaxRentedBuffer} bytes");

                size = Math.Min(size * 2, MaxRentedBuffer);
            }
        }

        #endregion
    }
}