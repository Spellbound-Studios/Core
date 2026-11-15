// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using Spellbound.Core.Observations;

namespace Spellbound.Core.Authoring.Editor {
    [RegisteredDefinitionCheck("core.guard-policy", RunsWhileEditing = true)]
    public sealed class GuardPolicyCheck : IDefinitionCheck<GuardPolicy> {
        public const string Code = "core.guard-policy";
        public const int SlowestTicksPerSecond = 20;

        public void Check(GuardPolicy definition, ValidationReport report) {
            if (definition == null) {
                throw new ArgumentNullException(nameof(definition));
            }

            if (report == null) {
                throw new ArgumentNullException(nameof(report));
            }

            var own = new ValidationReport(report.Subject);
            definition.Validate(own);

            if (own.HasErrors) {
                return;
            }

            var tickSeconds = 1.0 / SlowestTicksPerSecond;

            if (definition.windowSeconds < tickSeconds) {
                report.Warning(Code,
                    "The window is " + Seconds(definition.windowSeconds) + " seconds, shorter than one tick at "
                    + SlowestTicksPerSecond.ToString(CultureInfo.InvariantCulture) + " a second.",
                    nameof(definition.windowSeconds),
                    "Set a window of " + Seconds(tickSeconds) + " seconds or more, or repairs on separate ticks"
                    + " never count together.");
            }
        }

        public IReadOnlyList<string> PreviewLines(GuardPolicy definition) {
            if (definition == null) {
                return Array.Empty<string>();
            }

            return new[] {
                definition.repairs.ToString(CultureInfo.InvariantCulture) + " repairs in a window of "
                + Seconds(definition.windowSeconds) + " s, held " + Seconds(definition.holdSeconds) + " s"
            };
        }

        private static string Seconds(double seconds) {
            return seconds.ToString(CultureInfo.InvariantCulture);
        }
    }
}
