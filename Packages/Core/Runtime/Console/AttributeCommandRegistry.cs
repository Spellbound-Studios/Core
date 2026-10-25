// Copyright 2026 Spellbound Studio Inc.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Spellbound.Core.Tooling;
using UnityEngine;

namespace Spellbound.Core.Console {
    /// <summary>
    /// Discovers and stores all methods marked with [ConsolePresetCommand] and [ConsoleUtilityCommand].
    /// Enables routing console commands to the appropriate game system methods.
    /// </summary>
    public static class AttributeCommandRegistry {
        /// <summary>
        /// Preset command handlers indexed by (commandName, moduleType).
        /// </summary>
        private static readonly Dictionary<(string commandName, Type moduleType), MethodCommandInfo> PresetHandlers =
                new();

        /// <summary>
        /// Utility command handlers indexed by commandName only.
        /// </summary>
        private static readonly Dictionary<string, MethodCommandInfo> UtilityHandlers =
                CommandRegistryUtilities.CreateCaseInsensitiveDictionary<MethodCommandInfo>();

        /// <summary>
        /// All registered command names.
        /// </summary>
        private static readonly HashSet<string> AllCommandNames = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Cached object instances that own the registered methods.
        /// </summary>
        private static readonly Dictionary<MethodInfo, object> MethodInstances = new();

        private static bool _isInitialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlaySession() => Clear();

        /// <summary>
        /// Initializes the method registry by scanning all assemblies for [ConsolePresetCommand] and [ConsoleUtilityCommand].
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize() {
            if (_isInitialized)
                return;

            DiscoverAllMethods();
            _isInitialized = true;
        }

        /// <summary>
        /// Scans all loaded assemblies for methods with the [ConsolePresetCommand] attribute.
        /// </summary>
        private static void DiscoverAllMethods() {
            var assemblies = CommandRegistryUtilities.GetScannableAssemblies();
            var presetCount = 0;
            var utilityCount = 0;

            foreach (var assembly in assemblies) {
                try {
                    var types = AssemblyScanning.LoadableTypes(assembly);

                    foreach (var type in types) {
                        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                      BindingFlags.Instance | BindingFlags.Static);

                        foreach (var method in methods) {
                            // Check for preset command
                            var presetAttr = method.GetCustomAttribute<ConsolePresetCommandAttribute>();

                            if (presetAttr != null) {
                                var commandInfo = CreateCommandInfo(method, presetAttr.CommandName, null,
                                    presetAttr.RequiredModuleType);

                                if (RegisterPresetCommand(commandInfo, presetAttr))
                                    presetCount++;

                                continue;
                            }

                            // Check for utility command
                            var utilityAttr = method.GetCustomAttribute<ConsoleUtilityCommandAttribute>();

                            if (utilityAttr == null)
                                continue;

                            {
                                var commandInfo = CreateCommandInfo(method, utilityAttr.CommandName,
                                    utilityAttr.Description, null);

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
                ("preset command(s)", presetCount),
                ("utility command(s)", utilityCount));
        }

        /// <summary>
        /// Creates a unified MethodCommandInfo from method metadata.
        /// </summary>
        private static MethodCommandInfo CreateCommandInfo(
            MethodInfo method, string commandName, string description, Type requiredModuleType) =>
                new() {
                    Method = method,
                    DeclaringClass = method.DeclaringType,
                    Description = description ?? $"Executes {commandName}",
                    RequiredModuleType = requiredModuleType
                };

        /// <summary>
        /// Private method that will register any preset commands that have been added.
        /// </summary>
        private static bool RegisterPresetCommand(MethodCommandInfo commandInfo, ConsolePresetCommandAttribute attr) {
            var commandName = CommandRegistryUtilities.NormalizeCommandName(attr.CommandName);

            if (commandName == null) {
                Debug.LogError(
                    $"[AttributeCommandRegistry] Invalid command name for {commandInfo.DeclaringClass?.Name}.{commandInfo.Method.Name}");

                return false;
            }

            // Check for duplicates but allow the same name with different modules.
            var key = (commandName, attr.RequiredModuleType);

            if (!PresetHandlers.TryAdd(key, commandInfo)) {
                Debug.LogWarning(
                    $"[AttributeCommandRegistry] Duplicate preset handler for '{commandName}' with module {attr.RequiredModuleType.Name}");

                return false;
            }

            AllCommandNames.Add(commandName);

            CommandRegistryUtilities.LogCommandRegistered(
                commandName,
                $"{commandInfo.DeclaringClass?.Name}.{commandInfo.Method.Name} [Module: {attr.RequiredModuleType.Name}]");

            return true;
        }

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

        #region Preset Command API

        /// <summary>
        /// Checks if a preset command with the given name is registered (for any module type).
        /// </summary>
        public static bool HasPresetCommand(string commandName) {
            if (!_isInitialized)
                Initialize();

            var normalizedName = CommandRegistryUtilities.NormalizeCommandName(commandName);

            return normalizedName != null &&
                   PresetHandlers.Keys.Any(key => key.commandName == normalizedName);
        }

        /// <summary>
        /// Attempts to find a preset command handler for the given command and module type.
        /// </summary>
        public static bool TryGetPresetHandler(string commandName, Type moduleType, out MethodInfo method) {
            if (!_isInitialized)
                Initialize();

            var normalizedName = CommandRegistryUtilities.NormalizeCommandName(commandName);

            if (normalizedName == null) {
                method = null;

                return false;
            }

            var key = (normalizedName, moduleType);

            if (PresetHandlers.TryGetValue(key, out var commandInfo)) {
                method = commandInfo.Method;

                return true;
            }

            method = null;

            return false;
        }

        #endregion

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
        /// Unified execution logic for both preset and utility commands.
        /// </summary>
        private static CommandResult ExecuteMethodCommand(
            MethodCommandInfo commandInfo, string[] args, string commandName) {
            var parsedArgs = ParseArguments(commandInfo.Method, args);

            if (parsedArgs == null) {
                var usage = GetUsageString(commandInfo.Method, commandName);

                return CommandResult.Fail($"Invalid arguments.\n{usage}");
            }

            try {
                // Recall: For static methods (utility commands), the instance is null.
                // Recall: For instance methods (preset commands), the caller resolves the instance.
                var instance = commandInfo.Method.IsStatic
                        ? null
                        : GetMethodInstance(commandInfo.Method);

                commandInfo.Method.Invoke(instance, parsedArgs);

                return CommandResult.Ok($"Executed {commandName}");
            }
            catch (Exception ex) {
                Debug.LogError($"[AttributeCommandRegistry] Failed to execute command {commandName}: {ex.Message}");

                return CommandResult.Fail($"Execution failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Parse string arguments into typed parameters for a method.
        /// Shared by both preset and utility commands.
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

        /// <summary>
        /// Gets or creates an instance for non-static methods.
        /// Used by ConsoleCommandRouter for preset commands.
        /// </summary>
        public static object GetMethodInstance(MethodInfo method) {
            if (method.IsStatic)
                return null;

            if (MethodInstances.TryGetValue(method, out var cached)) {
                if (cached is not UnityEngine.Object unityObject || unityObject)
                    return cached;

                MethodInstances.Remove(method);
            }

            // I really dislike this right now, but I'm not sure how to improve it just yet.
            if (typeof(MonoBehaviour).IsAssignableFrom(method.DeclaringType)) {
                var instance = UnityEngine.Object.FindAnyObjectByType(method.DeclaringType) as MonoBehaviour;

                if (instance != null) {
                    MethodInstances[method] = instance;

                    return instance;
                }

                Debug.LogError(
                    $"[PresetCommandRegistry] No instance of {method.DeclaringType?.Name} found in scene for method {method.Name}");

                return null;
            }

            try {
                if (method.DeclaringType != null) {
                    var instance = Activator.CreateInstance(method.DeclaringType);
                    MethodInstances[method] = instance;

                    return instance;
                }
            }
            catch (Exception ex) {
                Debug.LogError(
                    $"[PresetCommandRegistry] Failed to create instance of {method.DeclaringType?.Name}: {ex.Message}");
            }

            return null;
        }

        #endregion

        /// <summary>
        /// Clears all registered methods.
        /// </summary>
        public static void Clear() {
            PresetHandlers.Clear();
            UtilityHandlers.Clear();
            AllCommandNames.Clear();
            MethodInstances.Clear();
            _isInitialized = false;
        }
    }
}