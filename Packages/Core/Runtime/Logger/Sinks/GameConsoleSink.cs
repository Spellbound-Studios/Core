// Copyright 2026 Spellbound Studio Inc.

namespace Spellbound.Core.Logging {
    public class GameConsoleSink : ILogSink {
        private const string DisplayNameValue = "Game Console";

        public static IGameConsoleOutput Output { get; set; }

        public string DisplayName => DisplayNameValue;

        public void Initialize(LogConfig config) { }

        public void Emit(LogLevel level, string source, string message, string member, int line) {
            var output = Output;

            if (output == null)
                return;

            var formatted = $"[{source}.{member}:{line}] {message}";

            if (level >= LogLevel.Error)
                output.PrintError(formatted);
            else
                output.PrintLine(formatted);
        }
    }
}
