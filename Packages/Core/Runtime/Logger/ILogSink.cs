// Copyright 2026 Spellbound Studio Inc.

namespace Spellbound.Core.Logging {
    public interface ILogSink {
        string DisplayName { get; }
        void Initialize(LogConfig config);
        void Emit(LogLevel level, string source, string message, string member, int line);
    }
}