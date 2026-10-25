// Copyright 2026 Spellbound Studio Inc.

using System;

namespace Spellbound.Core.Packing {
    /// <summary>
    /// IPacker is an interface that implements Pack and Unpack methods. It provides these extensions polymorphically to
    /// any struct that implements IPacker.
    /// </summary>
    public interface IPacker {
        public void Pack(ref Span<byte> buffer);
        public void Unpack(ref ReadOnlySpan<byte> buffer);
    }
}