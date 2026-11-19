// Copyright 2026 Spellbound Studio Inc.

namespace Spellbound.Core.Logging {
    public readonly struct LogMessageTally {
        public readonly LogLevel Level;
        public readonly string Source;
        public readonly string Member;
        public readonly int Line;
        public readonly string Message;
        public readonly int Count;

        public LogMessageTally(LogLevel level, string source, string member, int line, string message, int count) {
            Level = level;
            Source = source;
            Member = member;
            Line = line;
            Message = message;
            Count = count;
        }

        public override string ToString() => $"[{Level}] [{Source}.{Member}:{Line}] {Message} (x{Count})";
    }
}
