// Copyright 2026 Spellbound Studio Inc.

using System;

namespace Spellbound.Core.Console {
    /// <summary>
    /// Marks a method as a console command that can be invoked from the developer console.
    /// The method will only be called if the target preset has the specified module type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class ConsolePresetCommandAttribute : Attribute {
        /// <summary>
        /// The command name that triggers this method.
        /// </summary>
        public string CommandName { get; }

        /// <summary>
        /// The required PresetModule type that the target preset must have.
        /// Only presets with this module will be routed to this method.
        /// </summary>
        public Type RequiredModuleType { get; }

        public ConsolePresetCommandAttribute(string commandName, Type requiredModuleType) {
            CommandName = commandName;
            RequiredModuleType = requiredModuleType;
        }
    }
}