// Copyright (c) Spellbound Studios. All rights reserved.

using System.Threading;

namespace Spellbound.Core.Operations {
    public sealed class OperationContext {
        private readonly OperationRun _run;

        public bool DryRun { get; }
        public bool Confirmed { get; }
        public OperationCaller Caller { get; }
        public OperationHost Host { get; }
        public CancellationToken CancellationToken { get; }

        internal OperationContext(OperationHost host, OperationRun run, OperationCaller caller, bool dryRun, bool confirmed,
            CancellationToken cancellationToken) {
            Host = host;
            _run = run;
            Caller = caller;
            DryRun = dryRun;
            Confirmed = confirmed;
            CancellationToken = cancellationToken;
        }

        public void Report(string line) {
            _run.Report(line);
        }
    }
}
