// Copyright 2026 Spellbound Studio Inc.

using System;

namespace Spellbound.Core.Logging {
    public class UnityConsoleSink : ILogSink {
        private const string DisplayNameValue = "Unity Console";

        public string DisplayName => DisplayNameValue;
        public void Initialize(LogConfig config) { }

        public void Emit(LogLevel level, string source, string message, string member, int line,
            UnityEngine.Object context) {
            var formatted = $"[{source}.{member}:{line}] {message}";

            switch (level) {
                case LogLevel.Verbose:
                case LogLevel.Debug:
                case LogLevel.Info:
                    UnityEngine.Debug.Log(formatted, context);

                    break;
                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning(formatted, context);

                    break;
                case LogLevel.Error:
                    UnityEngine.Debug.LogError(formatted, context);

                    break;
                case LogLevel.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }
    }
}
