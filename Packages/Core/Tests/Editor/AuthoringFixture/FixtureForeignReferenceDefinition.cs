// Copyright (c) Spellbound Studios. All rights reserved.

using Spellbound.Core.Authoring;
using UnityEngine;

namespace Spellbound.Core.Tests.AuthoringFixture {
    public class FixtureForeignReferenceDefinition : ScriptableObject, IAuthoredDefinition {
        [SerializeReference] public object value;

        public void Validate(ValidationReport report) { }
    }
}
