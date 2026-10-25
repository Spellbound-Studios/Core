// Copyright 2026 Spellbound Studio Inc.

using System;

namespace Spellbound.Core.Logging {
    internal readonly struct LogMessageTallyKey : IEquatable<LogMessageTallyKey> {
        public readonly LogLevel Level;
        public readonly string Source;
        public readonly string Member;
        public readonly int Line;
        public readonly string Message;

        public LogMessageTallyKey(LogLevel level, string source, string member, int line, string message) {
            Level = level;
            Source = source;
            Member = member;
            Line = line;
            Message = message;
        }

        public bool Equals(LogMessageTallyKey other) =>
                Level == other.Level &&
                Line == other.Line &&
                string.Equals(Source, other.Source, StringComparison.Ordinal) &&
                string.Equals(Member, other.Member, StringComparison.Ordinal) &&
                string.Equals(Message, other.Message, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is LogMessageTallyKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Level, Source, Member, Line, Message);
    }
}
