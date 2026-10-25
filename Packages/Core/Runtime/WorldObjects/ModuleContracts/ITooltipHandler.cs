// Copyright 2026 Spellbound Studio Inc.

using Spellbound.Core.ObjectData;
using Spellbound.Core.Objects;

namespace Spellbound.Core.ModuleContracts {
    /// <summary>
    /// For ObjectPreset PresetModules
    /// Interface Contract for a Module to respond to being moused-over
    /// </summary>
    public interface ITooltipHandler {
        
        string GetTooltip();
    }
    
    /// <summary>
    /// For ObjectPreset PresetModules
    /// Interface Contract for a Module to respond in a data-aware way to being moused-over
    /// </summary>
    public interface ITooltipHandler<T> : ITooltipHandler where T : IPackerObjectData {
        string GetTooltip(T data);
    }
}