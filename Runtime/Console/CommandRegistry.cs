// Copyright 2026 Spellbound Studio Inc.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Spellbound.Core.Tooling;
using UnityEngine;

namespace Spellbound.Core.Console {
    /// <summary>
    /// Central registry for all console commands. The act of adding the attribute auto registers things here.
    /// </summary>
    public class CommandRegistry {
        private static CommandRegistry _instance;
        public static CommandRegistry Instance => _instance ??= new CommandRegistry();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlaySession() => _instance = null;

        // Stores all of our commands that implement the interface ICommand.
        private readonly Dictionary<string, ICommand> _commands =
                CommandRegistryUtilities.CreateCaseInsensitiveDictionary<ICommand>();

        // Stores the aliases that get registered via the ConsoleCommandAttribute.
        private readonly Dictionary<string, string> _aliases =
                CommandRegistryUtilities.CreateCaseInsensitiveDictionary<string>();

        private CommandRegistry() { }
        public static CommandRegistry CreateInstance() => new();

        /// <summary>
        /// Auto-discover and register all commands with the [ConsoleCommand] attribute.
        /// Call this during initialization.
        /// </summary>
        public void AutoRegisterCommands() {
            var assemblies = CommandRegistryUtilities.GetScannableAssemblies();

            foreach (var assembly in assemblies) {
                try {
                    AutoRegisterCommandsFromAssembly(assembly);
                }
                catch (Exception ex) {
                    CommandRegistryUtilities.LogAssemblyScanError(assembly.FullName, ex.Message);
                }
            }
        }

        /// <summary>
        /// Auto-register commands from a specific assembly.
        /// </summary>
        public void AutoRegisterCommandsFromAssembly(Assembly assembly) {
            var commandTypes = AssemblyScanning.LoadableTypes(assembly)
                    .Where(t => t.IsClass && !t.IsAbstract && typeof(ICommand).IsAssignableFrom(t))
                    .Where(t => t.GetCustomAttribute<ConsoleCommandClassAttribute>() != null);

            foreach (var type in commandTypes) {
                try {
                    var attribute = type.GetCustomAttribute<ConsoleCommandClassAttribute>();
                    var command = (ICommand)Activator.CreateInstance(type);

                    Register(command, attribute.Aliases);
                }
                catch (Exception ex) {
                    Debug.LogError($"Failed to register command {type.Name}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Manually register a command instance.
        /// </summary>
        public void Register(ICommand command, params string[] aliases) {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            CommandRegistryUtilities.ValidateCommandName(command.Name, nameof(command.Name));

            var commandName = CommandRegistryUtilities.NormalizeCommandName(command.Name);

            if (!_commands.TryAdd(commandName, command)) {
                CommandRegistryUtilities.LogDuplicateCommand(commandName, $"ICommand: {command.GetType().Name}");

                return;
            }

            if (aliases != null) {
                foreach (var alias in aliases) {
                    var normalizedAlias = CommandRegistryUtilities.NormalizeCommandName(alias);

                    if (normalizedAlias != null)
                        _aliases[normalizedAlias] = commandName;
                }
            }

            CommandRegistryUtilities.LogCommandRegistered(commandName, command.GetType().Name, aliases);
        }

        /// <summary>
        /// Unregister a command.
        /// </summary>
        public bool Unregister(string commandName) {
            var normalizedName = CommandRegistryUtilities.NormalizeCommandName(commandName);

            if (normalizedName == null)
                return false;

            var aliasesToRemove = _aliases
                    .Where(kvp => kvp.Value == normalizedName)
                    .Select(kvp => kvp.Key)
                    .ToList();

            foreach (var alias in aliasesToRemove)
                _aliases.Remove(alias);

            return _commands.Remove(normalizedName);
        }

        /// <summary>
        /// Try to get a command by name or alias.
        /// </summary>
        public bool TryGetCommand(string name, out ICommand command) {
            var normalizedName = CommandRegistryUtilities.NormalizeCommandName(name);

            if (normalizedName == null) {
                command = null;

                return false;
            }

            // Check direct command name
            if (_commands.TryGetValue(normalizedName, out command))
                return true;

            // Check aliases
            return _aliases.TryGetValue(normalizedName, out var commandName) &&
                   _commands.TryGetValue(commandName, out command);
        }

        /// <summary>
        /// Execute a command - routes to either class-based or method-based handlers.
        /// </summary>
        public CommandResult ExecuteCommand(string input) {
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
                return CommandResult.Fail("Empty command");

            var commandName = parts[0];
            var args = parts.Skip(1).ToArray();

            // Check ICommand
            if (TryGetCommand(commandName, out var command))
                return command.Execute(args);

            // Check utility commands
            if (AttributeCommandRegistry.HasUtilityCommand(commandName))
                return AttributeCommandRegistry.ExecuteUtilityCommand(commandName, args);

            // Check if it's a known preset command
            if (!AttributeCommandRegistry.HasPresetCommand(commandName))
                return CommandResult.Fail($"Unknown command: '{commandName}'\nType 'help' for available commands.");

            // Preset commands require a target
            if (args.Length == 0) {
                return CommandResult.Fail(
                    $"Command '{commandName}' requires a target.\nUsage: {commandName} <target> [quantity]");
            }

            var targetName = args[0];
            var remainingArgs = args.Skip(1).ToArray();

            return ConsoleCommandRouter.RouteCommand(commandName, targetName, remainingArgs);
        }

        /// <summary>
        /// Get all registered commands.
        /// </summary>
        public IEnumerable<ICommand> GetAllCommands() => _commands.Values;

        /// <summary>
        /// Clear all registered commands.
        /// </summary>
        public void Clear() {
            _commands.Clear();
            _aliases.Clear();
        }
    }
}