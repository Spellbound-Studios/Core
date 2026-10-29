// Copyright 2026 Spellbound Studio Inc.

using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace Spellbound.Core.Logging {
    public static class Log {
        internal struct RegisteredSink {
            public ILogSink Sink;
            public LogLevel FilterLevel;
        }

        private static readonly object SinkMutationLock = new object();

        private static volatile RegisteredSink[] _sinks = System.Array.Empty<RegisteredSink>();

        [System.ThreadStatic]
        private static int _emitDepth;

        internal static bool IsEmitting => _emitDepth > 0;

        public static void AddSink(ILogSink sink, LogConfig config, LogLevel filterLevel) {
            if (sink == null)
                return;

            sink.Initialize(config);

            lock (SinkMutationLock) {
                var old = _sinks;
                var next = new RegisteredSink[old.Length + 1];
                System.Array.Copy(old, next, old.Length);
                next[old.Length] = new RegisteredSink { Sink = sink, FilterLevel = filterLevel };
                _sinks = next;
            }
        }

        public static LogSinkScope AddScopedSink(ILogSink sink, LogConfig config, LogLevel filterLevel) {
            AddSink(sink, config, filterLevel);

            return new LogSinkScope(sink);
        }

        public static bool RemoveSink(ILogSink sink) {
            if (sink == null)
                return false;

            lock (SinkMutationLock) {
                var old = _sinks;
                var index = -1;

                for (var i = 0; i < old.Length; i++) {
                    if (!ReferenceEquals(old[i].Sink, sink))
                        continue;

                    index = i;

                    break;
                }

                if (index < 0)
                    return false;

                var next = new RegisteredSink[old.Length - 1];
                System.Array.Copy(old, next, index);
                System.Array.Copy(old, index + 1, next, index, old.Length - index - 1);
                _sinks = next;

                return true;
            }
        }

        public static void ClearSinks() {
            lock (SinkMutationLock)
                _sinks = System.Array.Empty<RegisteredSink>();
        }

        public static SinkSuspension SuspendSinks() {
            lock (SinkMutationLock) {
                var suspended = _sinks;
                _sinks = System.Array.Empty<RegisteredSink>();

                return new SinkSuspension(suspended);
            }
        }

        private static void RestoreSinks(RegisteredSink[] suspended) {
            if (suspended == null || suspended.Length == 0)
                return;

            lock (SinkMutationLock) {
                var current = _sinks;
                var restored = new System.Collections.Generic.List<RegisteredSink>(current);

                foreach (var entry in suspended) {
                    var alreadyPresent = false;

                    foreach (var existing in current) {
                        if (!ReferenceEquals(existing.Sink, entry.Sink))
                            continue;

                        alreadyPresent = true;

                        break;
                    }

                    if (!alreadyPresent)
                        restored.Add(entry);
                }

                _sinks = restored.ToArray();
            }
        }

        public readonly struct SinkSuspension : System.IDisposable {
            private readonly RegisteredSink[] _suspended;

            internal SinkSuspension(RegisteredSink[] suspended) => _suspended = suspended;

            public void Dispose() => RestoreSinks(_suspended);
        }

        [Conditional("SPELLBOUND_LOG_VERBOSE")]
        public static void Verbose(
            string message,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0
        ) =>
                Emit(LogLevel.Verbose, message, file, member, line);

        [Conditional("SPELLBOUND_LOG_DEBUG")]
        public static void Debug(
            string message,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0
        ) =>
                Emit(LogLevel.Debug, message, file, member, line);

        [Conditional("SPELLBOUND_LOG_INFO")]
        public static void Info(
            string message,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0
        ) =>
                Emit(LogLevel.Info, message, file, member, line);

        [Conditional("SPELLBOUND_LOG_WARNING")]
        public static void Warn(
            string message,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0
        ) =>
                Emit(LogLevel.Warning, message, file, member, line);

        public static void Error(
            string message,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0
        ) =>
                Emit(LogLevel.Error, message, file, member, line);

        public static void Emit(LogLevel level, string message, string file, string member, int line) {
            var sinks = _sinks;

            if (sinks.Length == 0)
                return;

            string source = null;
            _emitDepth++;

            try {
                for (var i = 0; i < sinks.Length; i++) {
                    if (level < sinks[i].FilterLevel)
                        continue;

                    source ??= Path.GetFileNameWithoutExtension(file);
                    sinks[i].Sink.Emit(level, source, message, member, line);
                }
            }
            finally {
                _emitDepth--;
            }
        }
    }
}