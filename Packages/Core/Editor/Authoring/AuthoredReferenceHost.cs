// Copyright (c) Spellbound Studios. All rights reserved.

using UnityEngine;

namespace Spellbound.Core.Authoring.Editor {
    internal sealed class AuthoredReferenceHost : ScriptableObject {
        public const string ValueField = nameof(value);

        [SerializeReference] public object value;
    }
}
