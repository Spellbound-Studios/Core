// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections.Generic;
using Spellbound.Core.Authoring;
using UnityEngine;

namespace Spellbound.Core.Tests.AuthoringFixture {
    public class FixtureReferencingDefinition : ScriptableObject, IAuthoredDefinition {
        public FixtureDefinition material;
        public List<ScriptableObject> extras = new List<ScriptableObject>();

        public void Validate(ValidationReport report) { }
    }
}
