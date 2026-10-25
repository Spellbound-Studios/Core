// Copyright 2026 Spellbound Studio Inc.

using System.Collections.Generic;
using Spellbound.Core.ObjectData;

namespace Spellbound.Core.ObjectHandling {
    public interface IObjectInstanceConsumer {
        int GetNextInstanceIndex();
        void OnRuntimeInstancesLoaded(IReadOnlyList<(int, NonProceduralStaticInstanceEntry)> creations);
        void OnRuntimeInstancesCreated(IReadOnlyList<(int, NonProceduralStaticInstanceEntry)> creations);

        void OnInstancesDeleted(IReadOnlyList<int> instanceIndices);

        void OnInstanceDeleteResolve(int instanceIndex, Dictionary<InstanceDataKey, byte[]> dataSlots = null);
        void OnInstanceDataChanged(int instanceIndex, InstanceDataKey key, IPackerObjectData data, byte context);
        void OnInstanceDataResolved(int instanceIndex, InstanceDataKey key, IPackerObjectData data, byte context);

        void OnDynamicObjectsLoaded(IReadOnlyList<(int, DynamicInstanceEntry)> loaded);
        void OnDynamicObjectsCreated(IReadOnlyList<(int, DynamicInstanceEntry)> creations);
        void OnDynamicObjectEntityRequested(IReadOnlyList<(int, uint, TransformData)> entityRequests);

        void OnDynamicObjectEntityDeleteRequested(IReadOnlyList<int> entityDeleteRequests);

        List<int> GetCurrentNonProceduralDynamicEntities();
    }
}