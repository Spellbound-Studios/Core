// Copyright (c) Spellbound Studios. All rights reserved.

using Spellbound.Core.Authoring;
using UnityEngine;
using UnityEngine.Serialization;

namespace Spellbound.Core.Tests.AuthoringFixture {
    public class FixtureRenamedDefinition : ScriptableObject, IAuthoredDefinition {
        [FormerlySerializedAs("oldName")] public string label;

        public void Validate(ValidationReport report) { }
    }
}
