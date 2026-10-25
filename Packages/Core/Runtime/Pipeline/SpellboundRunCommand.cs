// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Threading.Tasks;
using Spellbound.Core.Operations;

namespace Spellbound.Core.Pipeline {
    public static class SpellboundRunCommand {
        public const string Description =
            "Runs one Spellbound operation line, the same text the in-game console accepts. Start with: core help";

        public const string LineDescription = "The operation line, for example: core help";
        public const string Tag = "spellbound";

        public static async Task<string> Run(string line) {
            var run = CoreKit.Operations.Run(line, PipelineCaller.Current);
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
