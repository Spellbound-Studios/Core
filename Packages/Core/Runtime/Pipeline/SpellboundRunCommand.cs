// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Threading.Tasks;
using Spellbound.Core.Observations;
using Spellbound.Core.Operations;

namespace Spellbound.Core.Pipeline {
    public static class SpellboundRunCommand {
        public const string Description =
            "Runs one Spellbound operation line, the same text the in-game console accepts. Start with: core help";

        public const string LineDescription = "The operation line, for example: core help";
        public const string WorldDescription =
            "The live world whose observations the operation records; leave it out to record into the one live world,"
            + " or nowhere when several are live";

        public const string Tag = "spellbound";

        public static async Task<string> Run(string line, string world) {
            if (!ObservationContexts.TryResolve(world, out var observations, out var failure)) {
                throw new ArgumentException(OperationResult.Invalid(failure).Text);
            }

            var run = CoreKit.Operations.Run(line, PipelineCaller.Current, observations);
            var result = await run.AsTask();

            switch (result.Status) {
                case OperationStatus.Invalid:
                    throw new ArgumentException(result.Text);
                case OperationStatus.Failed:
                case OperationStatus.Refused:
                    throw new InvalidOperationException(result.Text);
                default:
                    return result.Text;
            }
        }
    }
}
