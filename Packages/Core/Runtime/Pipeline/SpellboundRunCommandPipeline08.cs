// Copyright (c) Spellbound Studios. All rights reserved.

#if SPELLBOUND_PIPELINE_0_8
using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using UnityEngine.Scripting;

namespace Spellbound.Core.Pipeline {
    public static class SpellboundRunCommandPipeline08 {
        public const string CommandName = "spellbound_run";

        [CliCommand(CommandName, SpellboundRunCommand.Description, Tags = new[] { SpellboundRunCommand.Tag })]
        [Preserve]
        public static Task<string> Run([CliArg("line", SpellboundRunCommand.LineDescription, Required = true)] string line) {
            return SpellboundRunCommand.Run(line);
        }
    }
}
#endif
