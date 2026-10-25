// Copyright 2026 Spellbound Studio Inc.

using System;
using Spellbound.Core.Modules;
using UnityEngine;

namespace Spellbound.Core.Console {
    /// <summary>
    /// Optional module that marks an ObjectPreset as console-accessible.
    /// Presets with this module can be spawned or manipulated via console commands.
    /// </summary>
    [Serializable]
    public class ConsoleModule : PresetModule {
        [Header("Console Command Registration"), Tooltip("Automatically register this preset with the console system.")]
        public bool autoRegister = true;

        [Header("Spawn Settings"), Tooltip("Where command takes place.")]
        public SpawnLocation spawnLocation = SpawnLocation.AtCrosshair;

        [Tooltip("Default quantity if not specified in command.")]
        public int defaultQuantity = 1;
    }

    public enum SpawnLocation {
        AtCrosshair
    }
}