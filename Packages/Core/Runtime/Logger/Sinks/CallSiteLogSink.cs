// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using Spellbound.Core.Observations;

namespace Spellbound.Core.Logging {
    public sealed class CallSiteLogSink : ILogSink {
        private const string DisplayNameValue = "Call sites";

        private readonly ObservationContext _observations;
        private readonly int _ownerThreadId;
        private readonly Dictionary<LogCallSite, ObservationReason> _reasons =
            new Dictionary<LogCallSite, ObservationReason>();
        private int _droppedOffThread;

        public CallSiteLogSink(ObservationContext observations) {
            _observations = observations ?? throw new ArgumentNullException(nameof(observations));
            _ownerThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public string DisplayName => DisplayNameValue;

        public ObservationContext Observations => _observations;

        public int DroppedOffThread => _droppedOffThread;

        public void Initialize(LogConfig config) { }

        public void Emit(LogLevel level, string source, string message, string member, int line,
            UnityEngine.Object context) {
            var counter = CoreObservations.LogCounter(level);

            if (counter == null || _observations.IsDiscard) {
                return;
            }

            if (Thread.CurrentThread.ManagedThreadId != _ownerThreadId) {
                Interlocked.Increment(ref _droppedOffThread);
                return;
            }

            var site = new LogCallSite(source, member, line);

            if (!_reasons.TryGetValue(site, out var reason)) {
                reason = CoreKit.Observations.Reason(site.ToString());
                _reasons.Add(site, reason);
            }

            counter.Add(_observations, reason);
        }
    }
}
