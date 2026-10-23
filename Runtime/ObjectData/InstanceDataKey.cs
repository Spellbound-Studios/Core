// Copyright 2026 Spellbound Studio Inc.

using System;

namespace Spellbound.Core.ObjectData {
    public readonly struct InstanceDataKey : IEquatable<InstanceDataKey> {
        public readonly uint PackerHash;
        public readonly byte SurfaceIndex;

        public InstanceDataKey(uint packerHash, byte surfaceIndex) {
            PackerHash = packerHash;
            SurfaceIndex = surfaceIndex;
        }

        public bool Equals(InstanceDataKey other) =>
                PackerHash == other.PackerHash && SurfaceIndex == other.SurfaceIndex;

        public override bool Equals(object obj) => obj is InstanceDataKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(PackerHash, SurfaceIndex);
        public override string ToString() => $"{PackerHash}[{SurfaceIndex}]";
    }
}
