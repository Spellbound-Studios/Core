// Copyright (c) Spellbound Studios. All rights reserved.

namespace Spellbound.Core.Operations {
    public interface IOperationRouter {
        bool TryRoute(OperationInvocation invocation, OperationDescription description, OperationCaller caller,
            out OperationRun run);
    }
}
