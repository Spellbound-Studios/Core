// Copyright 2026 Spellbound Studio Inc.

using Spellbound.Core.ObjectData;
using Spellbound.Core.Objects;

namespace Spellbound.Core.ModuleContracts {
    /// <summary>
    /// For ObjectPreset PresetModules
    /// Interface Contract for a Module to provide the default data of the ObjectPreset it is on.
    /// Level is scaffolding for future "1star", "2star", etc variants of an Enemy
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IDefaultDataProvider<T> where T : IPackerObjectData {
        T GetDefaultData(ObjectPreset preset, byte level = 1);
    }
}