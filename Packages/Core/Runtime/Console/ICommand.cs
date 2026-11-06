// Copyright 2026 Spellbound Studio Inc.

namespace Spellbound.Core.Console {
    /// <summary>
    /// Base interface for all console commands
    /// </summary>
    public interface ICommand {
        /// <summary>
        /// Primary command name (e.g., "spawn").
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Command description for help text.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Usage example (e.g., "spawn itemName amount").
        /// </summary>
        string Usage { get; }

        /// <summary>
        /// Execute the command with parsed arguments... This is the full usage packed into a string array.
        /// </summary>
        CommandResult Execute(string[] args);
    }
}