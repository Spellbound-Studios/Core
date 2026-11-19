// Copyright 2026 Spellbound Studio Inc.

namespace Spellbound.Core.Console {
    [ConsoleCommandClass("help", "?", "h")]
    public class HelpCommand : ICommand {
        public string Name => "help";
        public string Description => "Lists all available commands or shows help for a specific command";
        public string Usage => "help [command]";

        public CommandResult Execute(string[] args) {
            if (args.Length == 0) {
                var commands = CommandRegistry.Instance.GetAllCommands();
                var helpText = "Available commands:\n";

                foreach (var cmd in commands)
                    helpText += $"  {cmd.Name} - {cmd.Description}\n";

                helpText += "\nType 'help <command>' for more information.";

                return CommandResult.Ok(helpText);
            }

            var commandName = args[0];

            if (!CommandRegistry.Instance.TryGetCommand(commandName, out var command))
                return CommandResult.Fail($"Command '{commandName}' not found.");

            var text = $"{command.Name}\n" +
                       $"Description: {command.Description}\n" +
                       $"Usage: {command.Usage}";

            return CommandResult.Ok(text);
        }
    }
}