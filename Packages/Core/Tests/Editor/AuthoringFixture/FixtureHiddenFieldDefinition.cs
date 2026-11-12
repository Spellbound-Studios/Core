// Copyright (c) Spellbound Studios. All rights reserved.

using Spellbound.Core.Authoring;
using UnityEngine;

namespace Spellbound.Core.Tests.AuthoringFixture {
    public class FixtureHiddenFieldDefinition : ScriptableObject, IAuthoredDefinition {
        public string shown;
        [HideInInspector] public int hidden;

        public void Validate(ValidationReport report) { }
    }
}
