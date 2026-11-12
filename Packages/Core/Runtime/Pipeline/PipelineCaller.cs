// Copyright (c) Spellbound Studios. All rights reserved.

using Spellbound.Core.Operations;

namespace Spellbound.Core.Pipeline {
    public static class PipelineCaller {
        public const string Name = "pipeline";

        public static OperationCaller Current => From(CoreKit.Operations.Settings);

        public static OperationCaller From(OperationSettings settings) {
            return new OperationCaller(Name, settings != null ? settings.bridgeAudiences : null);
        }
    }
}
