// Copyright (c) Spellbound Studios. All rights reserved.

using Spellbound.Core.Observations;
using Spellbound.Core.Operations;

namespace Spellbound.Core.Tests {
    public sealed class HandingRouter : IOperationRouter {
        private readonly OperationHost _receiver;
        private readonly ObservationContext _observations;

        public HandingRouter(OperationHost receiver, ObservationContext observations) {
            _receiver = receiver;
            _observations = observations;
        }

        public int Calls { get; private set; }
        public OperationInvocation LastInvocation { get; private set; }
        public OperationDescription LastDescription { get; private set; }

        public bool TryRoute(OperationInvocation invocation, OperationDescription description, OperationCaller caller,
            out OperationRun run) {
            Calls++;
            LastInvocation = invocation;
            LastDescription = description;
            run = _receiver.RunHere(invocation, caller, _observations);
            return true;
        }
    }
}
