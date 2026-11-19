// Copyright 2026 Spellbound Studio Inc.

using System;
using System.Collections.Generic;

namespace Spellbound.Core.Logging {
    public class TallyingLogSink : ILogSink {
        private const string DisplayNameValue = "Tally";

        private readonly object _gate = new object();
        private readonly Dictionary<LogMessageTallyKey, int> _indexByKey = new Dictionary<LogMessageTallyKey, int>();
        private readonly List<LogMessageTallyKey> _keys = new List<LogMessageTallyKey>();
        private readonly List<int> _counts = new List<int>();
        private readonly int _maximumDistinctEntries;
        private int _droppedCount;

        public TallyingLogSink(int maximumDistinctEntries) => _maximumDistinctEntries = Math.Max(1, maximumDistinctEntries);

        public string DisplayName => DisplayNameValue;

        public int MaximumDistinctEntries => _maximumDistinctEntries;

        public int DistinctCount {
            get {
                lock (_gate)
                    return _keys.Count;
            }
        }

        public int DroppedCount {
            get {
                lock (_gate)
                    return _droppedCount;
            }
        }

        public void Initialize(LogConfig config) { }

        public void Emit(LogLevel level, string source, string message, string member, int line,
            UnityEngine.Object context) {
            var key = new LogMessageTallyKey(level, source, member, line, message);

            lock (_gate) {
                if (_indexByKey.TryGetValue(key, out var index)) {
                    _counts[index]++;

                    return;
                }

                if (_keys.Count >= _maximumDistinctEntries) {
                    _droppedCount++;

                    return;
                }

                _indexByKey[key] = _keys.Count;
                _keys.Add(key);
                _counts.Add(1);
            }
        }

        public LogMessageTally[] Drain() => Drain(out _);

        public LogMessageTally[] Drain(out int droppedCount) {
            lock (_gate) {
                droppedCount = _droppedCount;
                _droppedCount = 0;

                if (_keys.Count == 0)
                    return Array.Empty<LogMessageTally>();

                var tallies = new LogMessageTally[_keys.Count];

                for (var i = 0; i < _keys.Count; i++) {
                    var key = _keys[i];
                    tallies[i] = new LogMessageTally(key.Level, key.Source, key.Member, key.Line, key.Message, _counts[i]);
                }

                _indexByKey.Clear();
                _keys.Clear();
                _counts.Clear();

                return tallies;
            }
        }
    }
}
