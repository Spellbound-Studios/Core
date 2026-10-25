// Copyright 2026 Spellbound Studio Inc.

using System;

namespace Spellbound.Core.Logging {
    /// <summary>
    /// This wraps the unity console so that you can still get Debug.X to the unity console with our logging tool.
    /// </summary>
    public class UnityConsoleSink : ILogSink {
        private const string DisplayNameValue = "Unity Console";

        public string DisplayName => DisplayNameValue;
        public void Initialize(LogConfig config) { }

        public void Emit(LogLevel level, string source, string message, string member, int line) {
            var formatted = $"[{source}.{member}:{line}] {message}";

            switch (level) {
                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning(formatted);

                    break;
                case LogLevel.Error:
                    UnityEngine.Debug.LogError(formatted);

                    break;
                case LogLevel.Verbose:
                    break;
                case LogLevel.Debug:
                    UnityEngine.Debug.Log(formatted);

                    break;
                case LogLevel.Info:
                case LogLevel.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }
    }
}