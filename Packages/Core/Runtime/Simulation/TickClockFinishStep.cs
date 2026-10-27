// Copyright (c) Spellbound Studios. All rights reserved.

using UnityEngine;

namespace Spellbound.Core.Simulation {
    [DefaultExecutionOrder(32000)]
    internal sealed class TickClockFinishStep : MonoBehaviour {
        internal TickClock Clock { get; set; }

        private void FixedUpdate() {
            Clock.RaiseStepped();
        }
    }
}
