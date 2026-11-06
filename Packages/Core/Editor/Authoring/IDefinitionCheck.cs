// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using Spellbound.Core.Authoring;
using UnityEngine;

namespace Spellbound.Core.Authoring.Editor {
    public interface IDefinitionCheck<in T> where T : ScriptableObject {
        void Check(T definition, ValidationReport report);

        IReadOnlyList<string> PreviewLines(T definition) {
            return Array.Empty<string>();
        }
    }
}
