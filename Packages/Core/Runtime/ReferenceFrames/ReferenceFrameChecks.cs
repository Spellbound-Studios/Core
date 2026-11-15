// Copyright (c) Spellbound Studios. All rights reserved.

using Spellbound.Core.Observations;
using Spellbound.Core.Simulation;

namespace Spellbound.Core.ReferenceFrames {
    public static class ReferenceFrameChecks {
        public static bool RequireConstantUp(ObservationContext context, Tick tick, ReferenceFrameId referenceFrameId,
            in UpField upField) {
            if (upField.IsConstant) {
                return true;
            }

            CoreObservations.ReferenceFrameUpVaries.Check(context, false, referenceFrameId.Value, tick);
            return false;
        }
    }
}