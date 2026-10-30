// Copyright 2026 Spellbound Studio Inc.

using System;

namespace Spellbound.Core.Packing {
    /// <summary>
    /// Declares the persistent wire / save identity of an <see cref="ISmartPacker"/> type. The id string is
    /// hashed (FNV-1a 32) into the type tag written by SmartPack — renaming it breaks existing data. Required
    /// on every concrete ISmartPacker; <see cref="PackerRegistry"/> throws at startup if it is missing.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class PackerIdAttribute : Attribute {
        public string Id { get; }

        public PackerIdAttribute(string id) => Id = id;
    }
}