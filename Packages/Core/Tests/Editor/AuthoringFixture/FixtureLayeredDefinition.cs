// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections.Generic;
using Spellbound.Core.Authoring;
using UnityEngine;

namespace Spellbound.Core.Tests.AuthoringFixture {
    public class FixtureLayeredDefinition : ScriptableObject, IAuthoredDefinition {
        [SerializeReference] public List<FixtureLayer> layers = new List<FixtureLayer>();
        [SerializeField] private string caption;

        public void Validate(ValidationReport report) { }
    }
}
