// Copyright 2026 Spellbound Studio Inc.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Spellbound.Core.Tooling;
using UnityEngine;

namespace Spellbound.Core.Console {
    /// <summary>
    /// Discovers and stores all methods marked with [ConsoleUtilityCommand].
    /// Enables routing console commands to the appropriate game system methods.
    /// </summary>
    public static class AttributeCommandRegistry {
        /// <summary>
        /// Utility command handlers indexed by commandName only.
        /// </summary>
        private static readonly Dictionary<string, MethodCommandInfo> UtilityHandlers =
                CommandRegistryUtilities.CreateCaseInsensitiveDictionary<MethodCommandInfo>();

        /// <summary>
        /// All registered command names.
        /// </summary>
        private static readonly HashSet<string> AllCommandNames = new(StringComparer.OrdinalIgnoreCase);

        private static bool _isInitialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlaySession() => Clear();

        /// <summary>
        /// Initializes the method registry by scanning all assemblies for [ConsoleUtilityCommand].
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize() {
            if (_isInitialized)
                return;

            DiscoverAllMethods();
            _isInitialized = true;
        }

        /// <summary>
        /// Scans all loaded assemblies for methods with the [ConsoleUtilityCommand] attribute.
        /// </summary>
        private static void DiscoverAllMethods() {
            var assemblies = CommandRegistryUtilities.GetScannableAssemblies();
            var utilityCount = 0;

            foreach (var assembly in assemblies) {
                try {
                    var types = AssemblyScanning.LoadableTypes(assembly);

                    foreach (var type in types) {
                        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                      BindingFlags.Instance | BindingFlags.Static);

                        foreach (var method in methods) {
                            var utilityAttr = method.GetCustomAttribute<ConsoleUtilityCommandAttribute>();

                            if (utilityAttr == null)
                                continue;

                            {
                                var commandInfo = CreateCommandInfo(method, utilityAttr.CommandName,
                                    utilityAttr.Description);

                                if (RegisterUtilityCommand(commandInfo, utilityAttr))
                                    utilityCount++;
                            }
                        }
                    }
                }
                catch (Exception ex) {
                    CommandRegistryUtilities.LogAssemblyScanError(assembly.FullName, ex.Message);
                }
            }

            CommandRegistryUtilities.LogDiscoverySummary("AttributeCommandRegistry",
                ("utility command(s)", utilityCount));
        }

        /// <summary>
        /// Creates a unified MethodCommandInfo from method metadata.
        /// </summary>
        private static MethodCommandInfo CreateCommandInfo(
            MethodInfo method, string commandName, string description) =>
                new() {
                    Method = method,
                    DeclaringClass = method.DeclaringType,
                    Description = description ?? $"Executes {commandName}"
                };

        /// <summary>
        /// Private method that will register any utility commands via an attribute that have been added.
        /// </summary>
        private static bool RegisterUtilityCommand(MethodCommandInfo commandInfo, ConsoleUtilityCommandAttribute attr) {
            var commandName = CommandRegistryUtilities.NormalizeCommandName(attr.CommandName);

            if (commandName == null) {
                Debug.LogError(
                    $"[AttributeCommandRegistry] Invalid command name for {commandInfo.DeclaringClass?.Name}.{commandInfo.Method.Name}");

                return false;
            }

            if (!commandInfo.Method.IsStatic) {
                Debug.LogError(
                    $"[AttributeCommandRegistry] Utility command {commandInfo.DeclaringClass?.Name}.{commandInfo.Method.Name} must be static - skipping");

                return false;
            }

            // Check for duplicates.
            if (!UtilityHandlers.TryAdd(commandName, commandInfo)) {
                CommandRegistryUtilities.LogDuplicateCommand(commandName,
                    $"{commandInfo.DeclaringClass?.Name}.{commandInfo.Method.Name}");

                return false;
            }

            AllCommandNames.Add(commandName);

            CommandRegistryUtilities.LogCommandRegistered(commandName,
                $"{commandInfo.DeclaringClass?.Name}.{commandInfo.Method.Name}");

            return true;
        }

        #region Utility Command API

        /// <summary>
        /// Check if a utility command is registered.
        /// </summary>
        public static bool HasUtilityCommand(string commandName) {
            if (!_isInitialized)
                Initialize();

            var normalizedName = CommandRegistryUtilities.NormalizeCommandName(commandName);

            return normalizedName != null && UtilityHandlers.ContainsKey(normalizedName);
        }

        /// <summary>
        /// Execute a utility command with the given arguments.
        /// </summary>
        public static CommandResult ExecuteUtilityCommand(string commandName, string[] args) {
            if (!_isInitialized)
                Initialize();

            var normalizedName = CommandRegistryUtilities.NormalizeCommandName(commandName);

            if (normalizedName == null || !UtilityHandlers.TryGetValue(normalizedName, out var commandInfo))
                return CommandResult.Fail($"Unknown utility command: '{commandName}'");

            return ExecuteMethodCommand(commandInfo, args, commandName);
        }

        /// <summary>
        /// Get all utility commands from a specific class.
        /// Returns tuples of (commandName, description).
        /// </summary>
        public static IEnumerable<(string commandName, string description)> GetUtilityCommandsByClass(Type classType) {
            if (!_isInitialized)
                Initialize();

            return UtilityHandlers
                    .Where(kvp => kvp.Value.DeclaringClass == classType)
                    .Select(kvp => (kvp.Key, kvp.Value.Description))
                    .OrderBy(tuple => tuple.Key);
        }

        /// <summary>
        /// Get all utility commands from a class by name.
        /// Returns tuples of (commandName, description).
        /// </summary>
        public static IEnumerable<(string commandName, string description)> GetUtilityCommandsByClassName(
            string className) {
            if (!_isInitialized)
                Initialize();

            return UtilityHandlers
                    .Where(kvp =>
                            kvp.Value.DeclaringClass?.Name.Equals(className, StringComparison.OrdinalIgnoreCase) ==
                            true)
                    .Select(kvp => (kvp.Key, kvp.Value.Description))
                    .OrderBy(tuple => tuple.Key);
        }

        #endregion

        #region Shared Execution Logic

        /// <summary>
        /// Executes a method command with parsed arguments.
        /// </summary>
        private static CommandResult ExecuteMethodCommand(
            MethodCommandInfo commandInfo, string[] args, string commandName) {
            var parsedArgs = ParseArguments(commandInfo.Method, args);

            if (parsedArgs == null) {
                var usage = GetUsageString(commandInfo.Method, commandName);

                return CommandResult.Fail($"Invalid arguments.\n{usage}");
            }

            try {
                commandInfo.Method.Invoke(null, parsedArgs);

                return CommandResult.Ok($"Executed {commandName}");
            }
            catch (Exception ex) {
                Debug.LogError($"[AttributeCommandRegistry] Failed to execute command {commandName}: {ex.Message}");

                return CommandResult.Fail($"Execution failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Parse string arguments into typed parameters for a method.
        /// </summary>
        private static object[] ParseArguments(MethodInfo method, string[] args) {
            var parameters = method.GetParameters();

            if (parameters.Length == 0)
                return Array.Empty<object>();

            var parsedArgs = new List<object>();
            var argIndex = 0;

            foreach (var param in parameters) {
                if (argIndex >= args.Length) {
                    if (param.HasDefaultValue) {
                        parsedArgs.Add(param.DefaultValue);

                        continue;
                    }

                    ConsoleLogger.PrintError($"Missing required parameter: {param.Name}");

                    return null;
                }

                // Vector3
                if (param.ParameterType == typeof(Vector3)) {
                    if (CommandParameterParser.TryParseVector3(args, argIndex, out var vec3)) {
                        parsedArgs.Add(vec3);
                        argIndex += 3;

                        continue;
                    }

                    ConsoleLogger.PrintError($"Invalid Vector3 for parameter {param.Name}. Expected: x y z");

                    return null;
                }

                // Vector2
                if (param.ParameterType == typeof(Vector2)) {
                    if (CommandParameterParser.TryParseVector2(args, argIndex, out var vec2)) {
                        parsedArgs.Add(vec2);
                        argIndex += 2;

                        continue;
                    }

                    ConsoleLogger.PrintError($"Invalid Vector2 for parameter {param.Name}. Expected: x y");

                    return null;
                }

                // List<T>
                if (param.ParameterType.IsGenericType &&
                    param.ParameterType.GetGenericTypeDefinition() == typeof(List<>)) {
                    var elementType = param.ParameterType.GetGenericArguments()[0];
                    var listType = typeof(List<>).MakeGenericType(elementType);
                    var list = Activator.CreateInstance(listType);
                    var addMethod = listType.GetMethod("Add");

                    while (argIndex < args.Length) {
                        if (CommandParameterParser.TryParse(args[argIndex], elementType, out var element)) {
                            addMethod?.Invoke(list, new[] { element });
                            argIndex++;
                        }
                        else {
                            ConsoleLogger.PrintError($"Invalid {elementType.Name} in list: {args[argIndex]}");

                            return null;
                        }
                    }

                    parsedArgs.Add(list);

                    continue;
                }

                // Standard
                if (CommandParameterParser.TryParse(args[argIndex], param.ParameterType, out var parsed)) {
                    parsedArgs.Add(parsed);
                    argIndex++;

                    continue;
                }

                ConsoleLogger.PrintError(
                    $"Invalid {CommandParameterParser.GetTypeName(param.ParameterType)} for parameter {param.Name}: {args[argIndex]}");

                return null;
            }

            return parsedArgs.ToArray();
        }

        /// <summary>
        /// Internal helper.
        /// </summary>
        private static string GetUsageString(MethodInfo method, string commandName) {
            var usage = $"Usage: {commandName}";

            var parameters = method.GetParameters();

            foreach (var param in parameters) {
                var typeName = CommandParameterParser.GetTypeName(param.ParameterType);

                if (param.ParameterType == typeof(Vector3))
                    usage += $" <{param.Name}_x> <{param.Name}_y> <{param.Name}_z>";

                else if (param.ParameterType == typeof(Vector2))
                    usage += $" <{param.Name}_x> <{param.Name}_y>";

                else if (param.HasDefaultValue)
                    usage += $" [{param.Name}:{typeName}]";

                else
                    usage += $" <{param.Name}:{typeName}>";
            }

            return usage;
        }

        #endregion

        /// <summary>
        /// Clears all registered methods.
        /// </summary>
        public static void Clear() {
            UtilityHandlers.Clear();
            AllCommandNames.Clear();
            _isInitialized = false;
        }
    }
}