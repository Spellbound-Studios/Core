// Copyright 2026 Spellbound Studio Inc.

using System;

namespace Spellbound.Core.Packing {
    public static class SmartPackerExtensions {
        public static ISmartPacker SmartUnpack(this byte[] data) {
            ReadOnlySpan<byte> span = data;
            var hash = Packer.ReadUInt(ref span);
            if (!SmartPackerRegistry.TryCreateInstance(hash, out var instance))
                throw new Exception($"SmartUnpack: no registered packer for hash {hash}");
            instance.Unpack(ref span);
            return instance;
        }
        
        public static byte[] SmartPack(this ISmartPacker packer) {
            return Packer.BuildPayload((ref Span<byte> buffer) => {
                Packer.WriteUInt(ref buffer, packer.Hash);
                packer.Pack(ref buffer);
            });
        }
    }
}