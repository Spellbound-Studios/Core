// Copyright 2026 Spellbound Studio Inc.

using System.Collections.Generic;
using System.Linq;
using Spellbound.Core.Objects;
using UnityEngine;

namespace Spellbound.Core.Console {
    /// <summary>
    /// Registry that maps preset object names to their UIDs.
    /// Provides lookup functionality for resolving preset names to their unique identifiers.
    /// </summary>
    public static class PresetResolver {
        private static readonly Dictionary<string, string> NameToGuid =
                CommandRegistryUtilities.CreateCaseInsensitiveDictionary<string>();

        private static bool _isInitialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlaySession() => Clear();

        /// <summary>
        /// Initializes the preset registry by scanning all ObjectPresets with ConsoleModules.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize() {
            if (_isInitialized)
                return;

            RegisterAllPresets();
            _isInitialized = true;
        }

        /// <summary>
        /// Registers every preset already discovered by PresetRegistry, avoiding a second Resources scan.
        /// </summary>
        private static void RegisterAllPresets() {
            var allPresets = PresetRegistry.All;
            var registeredCount = 0;

            foreach (var preset in allPresets) {
                RegisterPreset(preset);
                registeredCount++;
            }

            CommandRegistryUtilities.LogDiscoverySummary("PresetResolver", registeredCount);
        }

        /// <summary>
        /// Registers a single preset's objectName → presetGuid mapping.
        /// This will be called internally once all presets are gathered.
        /// </summary>
        private static void RegisterPreset(ObjectPreset preset) {
            var key = preset.objectName.ToLower().Replace(" ", "_");

            if (NameToGuid.ContainsKey(key)) {
                Debug.LogWarning(
                    $"[PresetConsoleRegistry] Duplicate preset name '{preset.objectName}' - " +
                    "overwriting previous registration");
            }

            NameToGuid[key] = preset.Guid;
        }

        /// <summary>
        /// Attempts to resolve a preset name to its UID.
        /// </summary>
        public static bool TryResolvePresetGuid(string objectName, out string presetGuid) {
            if (!_isInitialized)
                Initialize();

            var normalizedName = objectName?.ToLower().Replace(" ", "_");

            if (!string.IsNullOrEmpty(normalizedName))
                return NameToGuid.TryGetValue(normalizedName, out presetGuid);

            presetGuid = null;

            return false;
        }

        /// <summary>
        /// Gets all registered preset names, sorted alphabetically.
        /// </summary>
        public static IEnumerable<string> GetAllPresetNames() {
            if (!_isInitialized)
                Initialize();

            // Chat bro helped me swim the dictionary using a long nested linq expression. This should only happen when
            // a user wants to do something like list all registered presets by name.
            return NameToGuid
                    .GroupBy(kvp => kvp.Value)
                    .Select(group => {
                        // Prefer an underscore version if it exists.
                        var underscoreVersion = group.FirstOrDefault(kvp => kvp.Key.Contains("_"));

                        // If we found an underscore version, use it; otherwise use the first one
                        return !string.IsNullOrEmpty(underscoreVersion.Key)
                                ? underscoreVersion.Key
                                : group.First().Key;
                    })
                    .OrderBy(name => name);
        }

        /// <summary>
        /// Gets the total count of registered presets.
        /// </summary>
        public static int GetPresetCount() {
            if (!_isInitialized)
                Initialize();

            return NameToGuid.Values.Distinct().Count();
        }

        /// <summary>
        /// Clears all registered presets for whatever reason lol.
        /// </summary>
        public static void Clear() {
            NameToGuid.Clear();
            _isInitialized = false;
        }
    }
}