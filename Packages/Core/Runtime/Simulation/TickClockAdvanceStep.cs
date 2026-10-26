// Copyright (c) Spellbound Studios. All rights reserved.

using UnityEngine;

namespace Spellbound.Core.Simulation {
    [DefaultExecutionOrder(-32000)]
    internal sealed class TickClockAdvanceStep : MonoBehaviour {
        internal TickClock Clock { get; set; }

        private void FixedUpdate() {
            if (Clock.Source is IFixedStepTickSource fed) {
                fed.FixedStep(Time.fixedDeltaTime);
            }

            Clock.Advance();
        }
    }
}
