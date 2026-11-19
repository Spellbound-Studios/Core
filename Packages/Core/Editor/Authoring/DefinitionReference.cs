// Copyright (c) Spellbound Studios. All rights reserved.

using UnityEngine;

namespace Spellbound.Core.Authoring.Editor {
    internal sealed class DefinitionReference {
        public string Field { get; }
        public ScriptableObject Target { get; }
        public string Package { get; }

        public DefinitionReference(string field, ScriptableObject target, string package) {
            Field = field;
            Target = target;
            Package = package;
        }
    }
}
