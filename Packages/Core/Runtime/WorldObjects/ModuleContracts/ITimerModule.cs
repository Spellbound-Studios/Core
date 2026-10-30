// Copyright 2026 Spellbound Studio Inc.

using Spellbound.Core.ObjectData;
using Spellbound.Core.Objects;

namespace Spellbound.Core.ModuleContracts {
    public interface ITimerModule {
        void OnTimerUpdate(
            uint time,
            IObjectDataAccess dataAccess, int instanceIndex, ObjectPreset preset);
    }

    public interface ITimerModule<T> : ITimerModule where T : IPackerObjectData {
        void OnTimerUpdate(
            T data, uint time, IObjectDataAccess dataAccess,
            int instanceIndex, ObjectPreset preset, byte eventSurfaceIndex = 0);
    }
}