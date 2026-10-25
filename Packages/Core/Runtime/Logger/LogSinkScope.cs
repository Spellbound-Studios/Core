// Copyright 2026 Spellbound Studio Inc.

using System;

namespace Spellbound.Core.Logging {
    public readonly struct LogSinkScope : IDisposable {
        private readonly ILogSink _sink;

        internal LogSinkScope(ILogSink sink) => _sink = sink;

        public ILogSink Sink => _sink;

        public void Dispose() => Log.RemoveSink(_sink);
    }
}
