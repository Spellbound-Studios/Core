// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections.Generic;
using Spellbound.Core.Authoring;
using Spellbound.Core.Authoring.Editor;
using Spellbound.Core.Tests.AuthoringFixture;

namespace Spellbound.Core.Tests.AuthoringFixture.Check {
    [RegisteredDefinitionCheck(Name, RunsWhileEditing = true)]
    public sealed class FixtureThresholdCheck : IDefinitionCheck<FixtureDefinition> {
        public const string Name = "fixturecheck.threshold-high";
        public const int Ceiling = 100;

        public void Check(FixtureDefinition definition, ValidationReport report) {
            if (definition.threshold > Ceiling) {
                report.Error(Name, "the threshold is " + definition.threshold + ", above " + Ceiling, nameof(definition.threshold),
                    "Lower it to " + Ceiling + " or less.");
            }
        }

        public IReadOnlyList<string> PreviewLines(FixtureDefinition definition) {
            return new[] { "threshold " + definition.threshold + " of " + Ceiling };
        }
    }
}
