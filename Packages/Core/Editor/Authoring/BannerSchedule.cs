// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;

namespace Spellbound.Core.Authoring.Editor {
    public sealed class BannerSchedule {
        private const char EntrySeparator = '\n';
        private const char ValueSeparator = '\t';

        private readonly Dictionary<string, DateTime> _lastChanges =
            new Dictionary<string, DateTime>(StringComparer.Ordinal);
        private readonly List<string> _waiting = new List<string>();
        private readonly Dictionary<string, double> _demotions = new Dictionary<string, double>(StringComparer.Ordinal);
        private readonly string _sessionKey;

        public int StillnessDelayMilliseconds { get; }
        public int CheckBudgetMilliseconds { get; }
        public bool IsWaiting => _waiting.Count > 0;

        public BannerSchedule(int stillnessDelayMilliseconds, int checkBudgetMilliseconds) : this(
            stillnessDelayMilliseconds, checkBudgetMilliseconds, null) { }

        public BannerSchedule(int stillnessDelayMilliseconds, int checkBudgetMilliseconds, string sessionKey) {
            StillnessDelayMilliseconds = Math.Max(0, stillnessDelayMilliseconds);
            CheckBudgetMilliseconds = Math.Max(0, checkBudgetMilliseconds);
            _sessionKey = string.IsNullOrEmpty(sessionKey) ? null : sessionKey;
            RestoreDemotions();
        }

        public void Changed(string path, DateTime now) {
            if (string.IsNullOrEmpty(path)) {
                return;
            }

            _lastChanges[path] = now;

            if (!_waiting.Contains(path)) {
                _waiting.Add(path);
            }
        }

        public DateTime ChangedAt(string path) {
            return path != null && _lastChanges.TryGetValue(path, out var changed) ? changed : DateTime.MinValue;
        }

        public string Due(DateTime now) {
            for (var index = 0; index < _waiting.Count; index++) {
                var path = _waiting[index];

                if ((now - _lastChanges[path]).TotalMilliseconds >= StillnessDelayMilliseconds) {
                    _waiting.RemoveAt(index);
                    return path;
                }
            }

            return null;
        }

        public bool IsOverBudget(double milliseconds) {
            return milliseconds > CheckBudgetMilliseconds;
        }

        public void Demote(string check, double milliseconds) {
            if (string.IsNullOrEmpty(check)) {
                return;
            }

            _demotions[check] = milliseconds;
            StoreDemotions();
        }

        public bool IsDemoted(string check) {
            return check != null && _demotions.ContainsKey(check);
        }

        public bool TryGetDemotion(string check, out double milliseconds) {
            milliseconds = 0;
            return check != null && _demotions.TryGetValue(check, out milliseconds);
        }

        private void StoreDemotions() {
            if (_sessionKey == null) {
                return;
            }

            var builder = new StringBuilder();

            foreach (var demotion in _demotions) {
                if (builder.Length > 0) {
                    builder.Append(EntrySeparator);
                }

                builder.Append(demotion.Key).Append(ValueSeparator)
                    .Append(demotion.Value.ToString("R", CultureInfo.InvariantCulture));
            }

            SessionState.SetString(_sessionKey, builder.ToString());
        }

        private void RestoreDemotions() {
            if (_sessionKey == null) {
                return;
            }

            foreach (var entry in SessionState.GetString(_sessionKey, string.Empty).Split(EntrySeparator)) {
                var separator = entry.LastIndexOf(ValueSeparator);

                if (separator > 0 && double.TryParse(entry.Substring(separator + 1), NumberStyles.Float,
                        CultureInfo.InvariantCulture, out var milliseconds)) {
                    _demotions[entry.Substring(0, separator)] = milliseconds;
                }
            }
        }
    }
}
