// Copyright (c) Spellbound Studios. All rights reserved.

using Spellbound.Core.Observations;
using Spellbound.Core.Operations;

namespace Spellbound.Core.Tests {
    public sealed class BouncingRouter : IOperationRouter {
        private readonly OperationHost _sender;
        private readonly ObservationContext _observations;

        public BouncingRouter(OperationHost sender, ObservationContext observations) {
            _sender = sender;
            _observations = observations;
        }

        public int Calls { get; private set; }

        public bool TryRoute(OperationInvocation invocation, OperationDescription description, OperationCaller caller,
            out OperationRun run) {
            Calls++;
            run = _sender.Run(invocation, caller, _observations);
            return true;
        }
    }
}
