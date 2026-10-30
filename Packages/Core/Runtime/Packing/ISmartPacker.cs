// Copyright 2026 Spellbound Studio Inc.

using System;
using Spellbound.Core.Registries;

namespace Spellbound.Core.Packing {
    /// <summary>
    /// A packer whose payload can be reconstructed without knowing the type first: SmartPack prefixes the
    /// payload with the type's stable hash, Id and hash resolve
    /// through <see cref="SmartPackerRegistry"/> — implementors declare nothing beyond the attribute.
    /// </summary>
    public interface ISmartPacker : IPacker, IRegistryEntry {
        ISmartPacker CreateNewInstance();
    }
}
