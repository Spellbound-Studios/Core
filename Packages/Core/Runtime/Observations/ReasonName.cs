// Copyright (c) Spellbound Studios. All rights reserved.

using Spellbound.Core.Registries;

namespace Spellbound.Core.Observations {
    internal sealed class ReasonName : IRegistryEntry {
        internal ReasonName(uint hash, string name) {
            Hash = hash;
            Name = name;
        }

        public uint Hash { get; }

        public string Name { get; }

        public override string ToString() => Name;
    }
}
