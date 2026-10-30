// Copyright 2026 Spellbound Studio Inc.

using System.Collections.Generic;
using Spellbound.Core.ObjectHandling;
using Spellbound.Core.Surfaces;
using UnityEngine;

namespace Spellbound.Core.ObjectData {
    public interface IDynamicDataAccess {
        void SetConsumer(IObjectInstanceConsumer consumer);

        public bool HasInstance(int instanceIndex);

        Dictionary<int, DynamicInstanceEntry> GetAllRuntimeDynamicInstances();

        void CreateRuntimeObject(
            uint presetHash, Vector3 position, Vector3 rotation, int scale,
            List<(InstanceDataKey, byte[])> dataSlots = null);

        void Awaken(int instanceIndex);
        void Sleep(int instanceIndex, DynamicInstanceEntry entry, IEventSurface eventSurface);
        void SetRuntimeDynamicEntry(int instanceIndex, DynamicInstanceEntry entry);
    }
}