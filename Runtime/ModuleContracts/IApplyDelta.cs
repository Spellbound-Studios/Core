// Copyright 2026 Spellbound Studio Inc.

using Spellbound.Core.ObjectData;
using Spellbound.Core.Objects;
using Spellbound.Core.Packing;

namespace Spellbound.Core.ModuleContracts {
    /// <summary>
    /// PresetModule Contract for the "Delta" operation.
    /// Implementers must have functionality to apply TDelta to TData to produce a new TData and a consequence.
    /// An Example would be to apply a CombatDispatch to a PassiveBehaviour, to produce a PassiveBehaviour with less
    /// of the "life" resource, and consequences such as the damageDealt and if it was a kill or not, which can be
    /// returned to the sender as an "Award".
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    /// <typeparam name="TDelta"></typeparam>
    public interface IApplyDelta<TData, in TDelta> 
            where TData : IPackerObjectData
            where TDelta : ISmartPacker {
        TData ApplyDelta(TData data, TDelta delta, ObjectPreset preset, byte surfaceIndex, out byte context, out ISmartPacker consequence);
    }
}