// Copyright 2026 Spellbound Studio Inc.

namespace Spellbound.Core.Console {
    [ConsoleCommandClass("clear", "cls")]
    public class ClearCommand : ICommand {
        public string Name => "clear";
        public string Description => "Clears the console output";
        public string Usage => "clear";

        public CommandResult Execute(string[] args) {
            ConsoleLogger.Clear();

            return CommandResult.Ok();
        }
    }
}
