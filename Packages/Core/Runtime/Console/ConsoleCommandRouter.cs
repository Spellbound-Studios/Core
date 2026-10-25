// Copyright 2026 Spellbound Studio Inc.

using System;
using System.Linq;
using Spellbound.Core.Objects;
using UnityEngine;

namespace Spellbound.Core.Console {
    /// <summary>
    /// Routes console commands to the appropriate handler methods.
    /// Orchestrates preset resolution, module checking, and method invocation.
    /// </summary>
    public static class ConsoleCommandRouter {
        /// <summary>
        /// Executes a console command by routing it to the appropriate handler.
        /// </summary>
        public static CommandResult RouteCommand(string commandName, string targetName, string[] args) {
            if (!PresetResolver.TryResolvePresetGuid(targetName, out var presetGuid))
                return CommandResult.Fail($"Unknown target: '{targetName}'");

            var preset = presetGuid.ResolvePresetGuid();

            if (preset == null)
                return CommandResult.Fail($"Failed to resolve preset: '{targetName}'");

            var quantity = 1;

            if (args.Length > 0 && int.TryParse(args[0], out var parsedQty))
                quantity = parsedQty;

            else if (preset.TryGetModule<ConsoleModule>(out var module))
                quantity = module.defaultQuantity;

            var moduleTypes = preset.GetAllModules()
                    .Select(m => m.GetType())
                    .ToList();

            foreach (var moduleType in moduleTypes) {
                if (!AttributeCommandRegistry.TryGetPresetHandler(commandName, moduleType, out var method))
                    continue;

                // Call the method 'quantity' times... Is this a placeholder? I'm not sure yet. But I think other games
                // do it like this.
                for (var i = 0; i < quantity; i++) {
                    var result = InvokeMethod(method, preset, presetGuid);

                    if (!result.Success)
                        return result; // Stop on first failure
                }

                return CommandResult.Ok($"Spawned {quantity}x {preset.objectName}");
            }

            return CommandResult.Fail($"No handler found for '{commandName}' with preset '{targetName}'");
        }

        /// <summary>
        /// Invokes the discovered method with appropriate arguments.
        /// </summary>
        private static CommandResult InvokeMethod(
            System.Reflection.MethodInfo method, ObjectPreset preset, string presetGuid) {
            try {
                var instance = AttributeCommandRegistry.GetMethodInstance(method);

                preset.TryGetModule<ConsoleModule>(out var consoleModule);

                // This is powerful. It uses reflection to get the parameters of the specified method or constructor.
                // https://learn.microsoft.com/en-us/dotnet/api/system.reflection.methodbase.getparameters?view=net-9.0
                // This will allow us to interpret intent from the command line.
                var methodParams = method.GetParameters();
                var invokeArgs = new object[methodParams.Length];

                // TODO: Smart argument mapping based on parameter types
                // For now, assume:
                // (string presetGuid, Vector3 position, Quaternion rotation, float scale, SbbData[] data)
                for (var i = 0; i < methodParams.Length; i++) {
                    var param = methodParams[i];

                    // See if we have a preset uid or not.
                    if (param.ParameterType == typeof(string) && param.Name == "presetGuid")
                        invokeArgs[i] = presetGuid;

                    // See if we have a rotational argument. (Identity because I don't know how we would do this lol)
                    else if (param.ParameterType == typeof(Quaternion))
                        invokeArgs[i] = Quaternion.identity;

                    // See if we have a value argument. (scale)
                    else if (param.ParameterType == typeof(float))
                        invokeArgs[i] = 1f;

                    // IDK - what to do with arrays yet so null.
                    else if (param.ParameterType.IsArray)
                        invokeArgs[i] = null;
                    else {
                        invokeArgs[i] = param.HasDefaultValue
                                ? param.DefaultValue
                                : null;
                    }
                }

                method.Invoke(instance, invokeArgs);

                return CommandResult.Ok($"Executed {method.Name} for {preset.objectName}");
            }
            catch (Exception ex) {
                Debug.LogError($"[ConsoleCommandRouter] Failed to invoke {method.Name}: {ex.Message}");

                return CommandResult.Fail($"Execution failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the world position where the command should be executed.
        /// </summary>
        private static Vector3 GetExecutionPosition(ConsoleModule module) {
            if (module == null)
                return GetCrosshairPosition();

            return module.spawnLocation switch {
                SpawnLocation.AtCrosshair => GetCrosshairPosition(),
                _ => Vector3.zero
            };
        }

        /// <summary>
        /// Raycasts from the camera center to get crosshair world position.
        /// </summary>
        private static Vector3 GetCrosshairPosition() {
            var camera = Camera.main;

            if (camera == null)
                return Vector3.zero;

            var ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

            if (Physics.Raycast(ray, out var hit, 100f))
                return hit.point;

            return camera.transform.position + camera.transform.forward * 5f;
        }
    }
}