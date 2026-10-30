// Copyright 2026 Spellbound Studio Inc.

using Spellbound.Core.ObjectData;
using Spellbound.Core.ObjectHandling;
using Spellbound.Core.Surfaces;

namespace Spellbound.Core.ModuleContracts {
    /// <summary>
    /// For ObjectPreset PresetModules
    /// Interface Contract for a Module to receive a dispatch event from an event surface.
    /// Dispatch is typically the call site for something that ultimately changes some data.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IDispatch<T> where T : IPackerDispatch {
        bool OnDispatch(
            T dispatchContext, IEventSurface eventSurface);
    }
}