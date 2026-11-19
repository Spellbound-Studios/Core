// Copyright 2026 Spellbound Studio Inc.

using UnityEngine;

namespace Spellbound.Core.Logging {
    public static class UnityLogCapture {
        private const string SourceName = "Unity";

        private static readonly object Gate = new object();
        private static volatile LogLevel _minimumLevel = LogLevel.Error;
        private static volatile bool _started;

        public static bool IsStarted => _started;

        public static LogLevel MinimumLevel => _minimumLevel;

        public static void Start(LogConfig config) => Start(config != null ? config.globalLevel : LogLevel.Error);

        public static void Start(LogLevel minimumLevel) {
            lock (Gate) {
                _minimumLevel = minimumLevel;

                if (_started)
                    return;

                Application.logMessageReceivedThreaded += OnLogMessageReceived;
                _started = true;
            }
        }

        public static void Stop() {
            lock (Gate) {
                if (!_started)
                    return;

                Application.logMessageReceivedThreaded -= OnLogMessageReceived;
                _started = false;
            }
        }

        public static LogLevel ToLogLevel(LogType type) =>
                type switch {
                    LogType.Log => LogLevel.Info,
                    LogType.Warning => LogLevel.Warning,
                    _ => LogLevel.Error
                };

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type) {
            if (Log.IsEmitting)
                return;

            var level = ToLogLevel(type);

            if (level < _minimumLevel)
                return;

            var message = string.IsNullOrWhiteSpace(stackTrace)
                    ? condition
                    : condition + "\n" + stackTrace.TrimEnd();

            Log.Emit(level, message, SourceName, type.ToString(), 0);
        }
    }
}
