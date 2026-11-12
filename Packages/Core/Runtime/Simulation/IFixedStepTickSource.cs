// Copyright (c) Spellbound Studios. All rights reserved.

namespace Spellbound.Core.Simulation {
    public interface IFixedStepTickSource : ITickSource {
        void FixedStep(double stepSeconds);
    }
}
