// Copyright 2026 Spellbound Studio Inc.

using System.Text;

namespace Spellbound.Core.Console {
    /// <summary>
    /// Lists all spawnable presets registered with the console.
    /// This is currently using the word spawn and probably needs some love, but I think it's fine for now.
    /// </summary>
    [ConsoleCommandClass("list", "ls")]
    public class ListCommand : ICommand {
        public string Name => "list";
        public string Description => "Lists all spawnable presets available in the console.";
        public string Usage => "list";

        public CommandResult Execute(string[] args) {
            var presetNames = PresetResolver.GetAllPresetNames();
            var count = PresetResolver.GetPresetCount();

            if (count == 0)
                return CommandResult.Ok("No spawnable presets registered.");

            var output = new StringBuilder();
            output.AppendLine($"Spawnable Presets ({count}):");
            output.AppendLine("─────────────────────────────");

            foreach (var name in presetNames)
                output.AppendLine($"  • {name}");

            return CommandResult.Ok(output.ToString());
        }
    }
}