// Copyright (c) Spellbound Studios. All rights reserved.

#if SPELLBOUND_PIPELINE_0_9
using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using UnityEngine.Scripting;

namespace Spellbound.Core.Pipeline {
    public static class SpellboundRunCommandPipeline09 {
        public const string Group = "spellbound";
        public const string CommandName = "run";

        [CliCommand(Group, CommandName, SpellboundRunCommand.Description, Tags = new[] { SpellboundRunCommand.Tag })]
        [Preserve]
        public static Task<string> Run([CliArg("line", SpellboundRunCommand.LineDescription, Required = true)] string line) {
            return SpellboundRunCommand.Run(line);
        }
    }
}
#endif
