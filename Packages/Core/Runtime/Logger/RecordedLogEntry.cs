// Copyright 2026 Spellbound Studio Inc.

namespace Spellbound.Core.Logging {
    public readonly struct RecordedLogEntry {
        public readonly LogLevel Level;
        public readonly string Source;
        public readonly string Message;
        public readonly string Member;
        public readonly int Line;

        public RecordedLogEntry(LogLevel level, string source, string message, string member, int line) {
            Level = level;
            Source = source;
            Message = message;
            Member = member;
            Line = line;
        }

        public override string ToString() => $"[{Level}] [{Source}.{Member}:{Line}] {Message}";
    }
}
