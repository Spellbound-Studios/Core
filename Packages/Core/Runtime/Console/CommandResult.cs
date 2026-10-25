// Copyright 2026 Spellbound Studio Inc.

namespace Spellbound.Core.Console {
    /// <summary>
    /// Represents the result of a console command execution.
    /// </summary>
    public struct CommandResult {
        public bool Success { get; }
        public string Message { get; }

        public CommandResult(bool success, string message = "") {
            Success = success;
            Message = message ?? string.Empty;
        }

        public static CommandResult Ok(string message = "") => new(true, message);
        public static CommandResult Fail(string message) => new(false, message);
    }
}