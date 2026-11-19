// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using Spellbound.Core.Packing;

namespace Spellbound.Core.Intents {
    public struct IntentKey : IEquatable<IntentKey>, IPacker {
        private const int PackedBytes = sizeof(ulong) + sizeof(uint);
        private const int PackedBits = PackedBytes * 8;

        public readonly IntentActor Actor;
        public readonly uint Sequence;

        public IntentKey(IntentActor actor, uint sequence) {
            Actor = actor;
            Sequence = sequence;
        }

        public static IntentKey None => default;

        public readonly bool IsNone => Actor.Value == 0 && Sequence == 0;

        public readonly void Pack(ref Span<byte> buffer) {
            Packer.WriteULong(ref buffer, Actor.Value);
            Packer.WriteUInt(ref buffer, Sequence);
        }

        public void Unpack(ref ReadOnlySpan<byte> buffer) {
            var actor = Packer.ReadULong(ref buffer);
            this = new IntentKey(new IntentActor(actor), Packer.ReadUInt(ref buffer));
        }

        public static bool TryUnpack(ref ReadOnlySpan<byte> buffer, out IntentKey key) {
            if (buffer.Length < PackedBytes) {
                key = None;
                return false;
            }

            var actor = Packer.ReadULong(ref buffer);
            key = new IntentKey(new IntentActor(actor), Packer.ReadUInt(ref buffer));
            return true;
        }

        public readonly void Write(ref BitWriter writer) {
            if (writer.BitsRemaining < PackedBits) {
                throw new ArgumentOutOfRangeException(nameof(writer),
                    "The buffer has no room for an intent key's " + PackedBits + " bits.");
            }

            writer.WriteLongBits(Actor.Value, 64);
            writer.WriteBits(Sequence, 32);
        }

        public static bool TryRead(ref BitReader reader, out IntentKey key) {
            if (reader.BitsRemaining < PackedBits) {
                key = None;
                return false;
            }

            reader.TryReadLongBits(64, out var actor);
            reader.TryReadBits(32, out var sequence);
            key = new IntentKey(new IntentActor(actor), sequence);
            return true;
        }

        public readonly bool Equals(IntentKey other) => Actor.Value == other.Actor.Value && Sequence == other.Sequence;

        public override readonly bool Equals(object obj) => obj is IntentKey other && Equals(other);

        public override readonly int GetHashCode() => HashCode.Combine(Actor.Value, Sequence);

        public static bool operator ==(IntentKey left, IntentKey right) => left.Equals(right);

        public static bool operator !=(IntentKey left, IntentKey right) => !left.Equals(right);

        public override readonly string ToString() => Actor + "#" + Sequence;
    }
}
