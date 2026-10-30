// Copyright 2026 Spellbound Studio Inc.

using System;
using Spellbound.Core.ObjectHandling;
using Spellbound.Core.Objects;
using Spellbound.Core.Packing;

namespace Spellbound.Core.ObjectData {
    /// <summary>
    /// Represents something that can not only be packed and unpacked, but can be unpacked without knowing what it is first.
    /// Methods with Invoke in the name are boilerplate required to be able to call generics with the concrete class.
    /// </summary>
    public interface IPackerObjectData : ISmartPacker {
        IPackerObjectData GetEmptyData();

        void InvokeChangeCallback(
            byte context, ObjectParent parent, int instanceIndex,
            ObjectPreset preset, byte surfaceIndex, TransformData transformData);

        void InvokeResolveCallback(
            byte context, ObjectParent parent, int instanceIndex,
            ObjectPreset preset, byte surfaceIndex, TransformData transformData);
        
    }
}