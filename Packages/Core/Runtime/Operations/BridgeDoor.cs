// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections.Generic;

namespace Spellbound.Core.Operations {
    public sealed class BridgeDoor {
        public string AssemblyName { get; }
        public string PackageName { get; }
        public string OtherName { get; }
        public string TypeName { get; }
        public IReadOnlyList<string> Members { get; }

        internal BridgeDoor(string assemblyName, string packageName, string otherName, string typeName, IReadOnlyList<string> members) {
            AssemblyName = assemblyName;
            PackageName = packageName;
            OtherName = otherName;
            TypeName = typeName;
            Members = members;
        }

        public override string ToString() {
            return TypeName + " in " + AssemblyName;
        }
    }
}
