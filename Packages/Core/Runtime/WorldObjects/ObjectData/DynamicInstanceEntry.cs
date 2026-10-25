// Copyright 2026 Spellbound Studio Inc.

using System;
using System.Collections.Generic;
using Spellbound.Core.Packing;

namespace Spellbound.Core.ObjectData {
    public class DynamicInstanceEntry : IPacker {
        public uint PresetHash;
        public Dictionary<InstanceDataKey, byte[]> DataSlots = new();
        public TransformData Transform;
        public bool WasMovingAtSave;

        public DynamicInstanceEntry() { }

        /// <summary>
        /// Ctor for DynamicInstanceEntry.
        /// </summary>
        /// <param name="presetHash"></param>
        /// <param name="transform"></param>
        /// <param name="wasMovingAtSave"></param>
        public DynamicInstanceEntry(uint presetHash, TransformData transform, bool wasMovingAtSave) {
            PresetHash = presetHash;
            Transform = transform;
            WasMovingAtSave = wasMovingAtSave;
        }

        public void Pack(ref Span<byte> buffer) {
            Packer.WriteUInt(ref buffer, PresetHash);
            Transform.Pack(ref buffer);
            Packer.WriteBool(ref buffer, WasMovingAtSave);

            Packer.WriteInt(ref buffer, DataSlots.Count);

            foreach (var (key, bytes) in DataSlots) {
                Packer.WriteUInt(ref buffer, key.PackerHash);
                Packer.WriteByte(ref buffer, key.SurfaceIndex);
                Packer.WriteBytes(ref buffer, bytes);
            }
        }

        public void Unpack(ref ReadOnlySpan<byte> buffer) {
            PresetHash = Packer.ReadUInt(ref buffer);
            Transform = new TransformData();
            Transform.Unpack(ref buffer);
            WasMovingAtSave = Packer.ReadBool(ref buffer);

            var count = Packer.ReadInt(ref buffer);
            DataSlots = new Dictionary<InstanceDataKey, byte[]>(count);

            for (var i = 0; i < count; i++) {
                var packerHash = Packer.ReadUInt(ref buffer);
                var surfaceIndex = Packer.ReadByte(ref buffer);
                var bytes = Packer.ReadBytes(ref buffer);
                DataSlots[new InstanceDataKey(packerHash, surfaceIndex)] = bytes;
            }
        }
    }
}