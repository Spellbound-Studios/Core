// Copyright 2026 Spellbound Studio Inc.

using System;

namespace Spellbound.Core.Console {
    /// <summary>
    /// Marks a static method as a console utility command that can be invoked directly from the console.
    /// This attribute does not require a presetID and can be added to any static method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class ConsoleUtilityCommandAttribute : Attribute {
        /// <summary>
        /// The command name used in the console.
        /// </summary>
        public string CommandName { get; }

        /// <summary>
        /// Description shown in the help text.
        /// </summary>
        public string Description { get; }

        public ConsoleUtilityCommandAttribute(string commandName, string description = null) {
            CommandName = commandName;
            Description = description ?? $"Executes {commandName}";
        }
    }
}