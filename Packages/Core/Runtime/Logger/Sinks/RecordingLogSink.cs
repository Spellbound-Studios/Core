// Copyright 2026 Spellbound Studio Inc.

using System;
using System.Collections.Generic;

namespace Spellbound.Core.Logging {
    public class RecordingLogSink : ILogSink {
        private const string DisplayNameValue = "Recording";

        private readonly List<RecordedLogEntry> _entries = new List<RecordedLogEntry>();
        private readonly object _gate = new object();
        private readonly LogLevel _minimumLevel;

        public RecordingLogSink(LogLevel minimumLevel) => _minimumLevel = minimumLevel;

        public string DisplayName => DisplayNameValue;

        public void Initialize(LogConfig config) { }

        public void Emit(LogLevel level, string source, string message, string member, int line) {
            if (level < _minimumLevel)
                return;

            lock (_gate)
                _entries.Add(new RecordedLogEntry(level, source, message, member, line));
        }

        public RecordedLogEntry[] Entries {
            get {
                lock (_gate)
                    return _entries.ToArray();
            }
        }

        public int Count {
            get {
                lock (_gate)
                    return _entries.Count;
            }
        }

        public int CountOf(LogLevel level) {
            lock (_gate) {
                var count = 0;

                for (var i = 0; i < _entries.Count; i++) {
                    if (_entries[i].Level == level)
                        count++;
                }

                return count;
            }
        }

        public bool Contains(LogLevel level, string substring) {
            lock (_gate) {
                for (var i = 0; i < _entries.Count; i++) {
                    if (_entries[i].Level != level)
                        continue;

                    var message = _entries[i].Message;

                    if (message != null && message.IndexOf(substring, StringComparison.Ordinal) >= 0)
                        return true;
                }

                return false;
            }
        }

        public void Clear() {
            lock (_gate)
                _entries.Clear();
        }
    }
}
