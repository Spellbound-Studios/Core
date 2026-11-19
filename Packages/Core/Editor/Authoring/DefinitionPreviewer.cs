// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using UnityEngine;

namespace Spellbound.Core.Authoring.Editor {
    public abstract class DefinitionPreviewer {
        public abstract Type DefinitionType { get; }

        public abstract DefinitionPreview Preview(ScriptableObject definition);

        public bool AppliesTo(Type assetType) {
            return assetType != null && DefinitionType.IsAssignableFrom(assetType);
        }
    }

    public abstract class DefinitionPreviewer<T> : DefinitionPreviewer where T : ScriptableObject {
        public sealed override Type DefinitionType => typeof(T);

        public sealed override DefinitionPreview Preview(ScriptableObject definition) {
            return Preview((T)definition);
        }

        protected abstract DefinitionPreview Preview(T definition);
    }
}
