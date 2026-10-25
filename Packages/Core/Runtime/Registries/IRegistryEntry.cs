// Copyright 2026 Spellbound Studio Inc.

namespace Spellbound.Core.Registries {
    /// <summary>
    /// Implemented by anything that can live in an <see cref="IRegistry{TEntry}"/>. Exposes a stable,
    /// deterministic <see cref="Hash"/> that is identical on every machine and build. A hash of 0 is reserved
    /// as the "null / none" id and is never stored in a registry.
    /// </summary>
    public interface IRegistryEntry {
        uint Hash { get; }
    }
}
