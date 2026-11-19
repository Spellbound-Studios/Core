// Copyright (c) Spellbound Studios. All rights reserved.

namespace Spellbound.Core.Simulation {
    public interface ITickSource {
        TickRate Rate { get; }

        Tick Current();
    }
}
