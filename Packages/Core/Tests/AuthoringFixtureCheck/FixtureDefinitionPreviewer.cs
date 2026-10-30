// Copyright (c) Spellbound Studios. All rights reserved.

using Spellbound.Core.Authoring.Editor;
using Spellbound.Core.Tests.AuthoringFixture;

namespace Spellbound.Core.Tests.AuthoringFixture.Check {
    public sealed class FixtureDefinitionPreviewer : DefinitionPreviewer<FixtureDefinition> {
        public const string Line = "a fixture previewer line";

        protected override DefinitionPreview Preview(FixtureDefinition definition) {
            return new DefinitionPreview("'" + definition.title + "' at threshold " + definition.threshold, null, new[] { Line });
        }
    }
}
