// Copyright 2026 Spellbound Studio Inc.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Spellbound.Core.ECS;
using Spellbound.Core.Logging;
using Spellbound.Core.ObjectData;
using Spellbound.Core.Objects;
using Spellbound.Core.ModuleContracts;
using Spellbound.Core.Surfaces;
using Spellbound.Core.Tooling;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Spellbound.Core.ObjectHandling {
    /// <summary>
    /// Poco to assist management of object data by the parent.
    /// </summary>
    public class ObjectParent : IDisposable, IObjectInstanceConsumer {
        private readonly IObjectParent _implementer;
        private readonly Transform _transform;

        private readonly int3 _id;
        private EntityQuery _staticQuery;
        private EntityQuery _dynamicQuery;
        private readonly Entity _ecsChunk;

        private const float SurfaceQueryMovementThreshold = 4f;

        private readonly Dictionary<int, IEventSurface> _eventSurfaces = new();
        private Vector3 _lastPovPosition;
        private bool _hasEvaluatedStaticProximity;
        private int _lastEvaluatedStaticEntityCount;

        public IObjectDataAccess StaticDataAccess { get; }
        public IDynamicDataAccess DynamicDataAccess { get; }

        private int _seedInstanceCount;
        private int _instanceIndexCursor = int.MinValue;

        public event Action<float3[]> OnDynamicProximityEval = delegate { };

        public event Action OnSetRuntimeDynamicEntryRequested = delegate { };

        /// <summary>
        /// Constructor. Includes the creation of ready-made entity queries.
        /// </summary>
        /// <param name="implementer"></param>
        /// <param name="transform"></param>
        /// <param name="staticDataAccess"></param>
        /// <param name="dynamicDataAccess"></param>
        /// <param name="parentId"></param>
        /// <param name="ecsChunk"></param>
        public ObjectParent(
            IObjectParent implementer, Transform transform, IObjectDataAccess staticDataAccess,
            IDynamicDataAccess dynamicDataAccess, Vector3Int parentId,
            Entity ecsChunk) {
            _implementer = implementer;
            _transform = transform;
            _id = new int3(parentId.x, parentId.y, parentId.z);
            _ecsChunk = ecsChunk;

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            _staticQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<ChunkParentComponent>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<ProximityThresholdComponent>(),
                ComponentType.ReadOnly<InstanceIndexComponent>(),
                ComponentType.ReadOnly<PresetHashComponent>(),
                ComponentType.Exclude<DynamicTag>()
            );

            _dynamicQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<ChunkParentComponent>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<ProximityThresholdComponent>(),
                ComponentType.ReadOnly<InstanceIndexComponent>(),
                ComponentType.ReadOnly<PresetHashComponent>(),
                ComponentType.ReadOnly<DynamicTag>()
            );

            _staticQuery.SetSharedComponentFilter(new ChunkParentComponent {
                ChunkCoord = _id
            });

            _dynamicQuery.SetSharedComponentFilter(new ChunkParentComponent {
                ChunkCoord = _id
            });

            StaticDataAccess = staticDataAccess;
            StaticDataAccess.SetConsumer(this);

            DynamicDataAccess = dynamicDataAccess;
            DynamicDataAccess.SetConsumer(this);
        }

        #region API

        public void SetSeedInstanceCount(int seedCount) => _seedInstanceCount = seedCount;

        public void Instantiate(
            ObjectPreset preset, Vector3 position, Vector3 rotation, int scale,
            List<(InstanceDataKey, byte[])> dataSlots = null) {
            if (!preset.isDynamic) {
                StaticDataAccess.CreateRuntimeInstance(preset.Hash, position, rotation, scale);

                return;
            }

            DynamicDataAccess.CreateRuntimeObject(preset.Hash, position, rotation, scale, dataSlots);
        }

        public void AdoptMigratedDynamic(int newIndex, DynamicInstanceEntry entry, IEventSurface surface) {
            surface.Transform.SetParent(_transform, true);
            surface.Initialize(_implementer, newIndex, entry.PresetHash);
        }

        public void CreateNewInstance(ObjectPreset preset, Vector3 position, Vector3 rotation, int scale) {
            if (!preset.isDynamic) {
                StaticDataAccess.CreateRuntimeInstance(preset.Hash, position, rotation, scale);

                return;
            }

            DynamicDataAccess.CreateRuntimeObject(preset.Hash, position, rotation, scale);
        }

        public bool TryReadData<T>(int instanceIndex, uint presetHash, byte eventSurfaceIndex, out T result)
                where T : IPackerObjectData, new() {
            if (StaticDataAccess.TryRead<T>(instanceIndex, eventSurfaceIndex, out var data)) {
                result = data;

                return true;
            }

            result = default;

            return false;
        }

        public bool TryReadDataAllData(
            int instanceIndex, uint presetHash, byte eventSurfaceIndex, out List<IPackerObjectData> results) =>
                StaticDataAccess.TryReadAllBySurface(instanceIndex, eventSurfaceIndex, out results);

        public bool WriteData<T>(
            int instanceIndex, uint presetHash, byte eventSurfaceIndex, T newData, byte context = 0)
                where T : IPackerObjectData, new() {
            StaticDataAccess.Write(instanceIndex, presetHash, eventSurfaceIndex, newData, context);

            return true;
        }

        public void Delta<TData, TDispatch>(
            int instanceIndex, uint presetHash, byte eventSurfaceIndex, TDispatch delta)
                where TData : IPackerObjectData, new()
                where TDispatch : IPackerDispatch, new(){
            StaticDataAccess.Delta<TData, TDispatch>(instanceIndex, presetHash, eventSurfaceIndex, delta);
        }

        public void DeleteInstance(int instanceIndex) =>
                StaticDataAccess.DeleteInstance(instanceIndex);

        #endregion API

        #region ECS

        /// <summary>
        /// Indicates to the ECS world that the Chunk is ready for the Instantiation System to make entities.
        /// </summary>
        public void SetEcsChunkReady() {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            em.AddComponentData(_ecsChunk, new ReadyForObjectsTag());
        }

        /// <summary>
        /// Sends the full state as Entity Spawn Requests to the ECS world.
        /// Note: event surfaces will not be added immediately.
        /// </summary>
        public void BufferFullStateStaticInstances() {
            var instances = StaticDataAccess.GetAllRuntimeInstances();

            if (instances.Count == 0)
                return;

            BufferEntitySpawnRequests(instances.Select(kvp => (kvp.Key, kvp.Value)));
        }

        /// <summary>
        /// Sends the full state of saved dynamic runtime entries as Entity Spawn Requests to the ECS world.
        /// </summary>
        public void BufferFullStateDynamicInstances() {
            var instances = DynamicDataAccess.GetAllRuntimeDynamicInstances();

            if (instances.Count == 0)
                return;

            var entityRequests = new List<(int, uint, TransformData)>(instances.Count);

            foreach (var (instanceIndex, entry) in instances)
                entityRequests.Add((instanceIndex, entry.PresetHash, entry.Transform));

            BufferEntitySpawnRequests(entityRequests);
        }

        /// <summary>
        /// Sends the full state as Deletions to the ECS world.
        /// These are all the procedurally-generated objects that the save state knows are deleted.
        /// These will be skipped over when the Instantiation System instantiates entities.
        /// </summary>
        public void BufferFullStateDeletions() {
            var deletions = StaticDataAccess.GetAllSeedInstanceDeletions();

            if (deletions.Count == 0)
                return;

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            var buffer = em.HasBuffer<DeletionBufferElement>(_ecsChunk)
                    ? em.GetBuffer<DeletionBufferElement>(_ecsChunk)
                    : em.AddBuffer<DeletionBufferElement>(_ecsChunk);

            foreach (var deletion in deletions)
                buffer.Add(new DeletionBufferElement { Value = deletion });
        }

        /// <summary>
        /// Destroys Static Entities from a list. 
        /// </summary>
        /// <param name="instanceIndices"></param>
        /// <param name="query"></param>
        private void DestroyEntities(IReadOnlyList<int> instanceIndices, EntityQuery query) {
            var removedSet = new HashSet<int>(instanceIndices);

            var selectedQuery = query;

            var indices = selectedQuery.ToComponentDataArray<InstanceIndexComponent>(Allocator.Temp);
            var entities = selectedQuery.ToEntityArray(Allocator.Temp);
            var entitiesToDestroy = new NativeList<Entity>(removedSet.Count, Allocator.Temp);

            for (var i = 0; i < indices.Length; i++) {
                if (removedSet.Contains(indices[i].Value))
                    entitiesToDestroy.Add(entities[i]);
            }

            if (entitiesToDestroy.Length > 0) {
                var em = World.DefaultGameObjectInjectionWorld.EntityManager;
                em.DestroyEntity(entitiesToDestroy.AsArray());
            }

            entitiesToDestroy.Dispose();
            indices.Dispose();
            entities.Dispose();
        }

        /// <summary>
        /// Destroys ALL entities, including the _ecsChunk itself,
        /// by making a new query that only queries for the ChunkParentComponent.
        /// </summary>
        private void DestroyAllEntities() {
            var world = World.DefaultGameObjectInjectionWorld;

            if (world is not { IsCreated: true })
                return;

            var em = world.EntityManager;

            var disposeQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<ChunkParentComponent>());

            disposeQuery.SetSharedComponentFilter(new ChunkParentComponent {
                ChunkCoord = _id
            });

            var entities = disposeQuery.ToEntityArray(Allocator.Temp);
            em.DestroyEntity(entities);
            entities.Dispose();
        }

        /// <summary>
        /// Overload for static entries created.
        /// </summary>
        /// <param name="instances"></param>
        private void BufferEntitySpawnRequests(IEnumerable<(int, NonProceduralStaticInstanceEntry)> instances) {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            var buffer = em.HasBuffer<EntitySpawnRequestElement>(_ecsChunk)
                    ? em.GetBuffer<EntitySpawnRequestElement>(_ecsChunk)
                    : em.AddBuffer<EntitySpawnRequestElement>(_ecsChunk);

            foreach (var (instanceIndex, entry) in instances) {
                if (!entry.PresetHash.TryGetEntityPrefab(out var prefab)) {
                    Log.Error($"Entity prefab could not be found for preset: {entry.PresetHash}");

                    continue;
                }

                buffer.Add(new EntitySpawnRequestElement {
                    Prefab = prefab,
                    InstanceIndex = instanceIndex,
                    Transform = entry.Transform.ToLocalTransform()
                });
            }
        }

        /// <summary>
        /// Overload for dynamic requests converting to entities.
        /// </summary>
        /// <param name="requests"></param>
        private void BufferEntitySpawnRequests(IReadOnlyList<(int, uint, TransformData)> requests) {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            var buffer = em.HasBuffer<EntitySpawnRequestElement>(_ecsChunk)
                    ? em.GetBuffer<EntitySpawnRequestElement>(_ecsChunk)
                    : em.AddBuffer<EntitySpawnRequestElement>(_ecsChunk);

            foreach (var (instanceIndex, presetHash, transformData) in requests) {
                if (!presetHash.TryGetEntityPrefab(out var prefab)) {
                    Log.Error($"Entity prefab could not be found for preset: {presetHash}");

                    continue;
                }

                buffer.Add(new EntitySpawnRequestElement {
                    Prefab = prefab,
                    InstanceIndex = instanceIndex,
                    Transform = transformData.ToLocalTransform()
                });
            }
        }

        #endregion ECS

        #region IObjectInstanceConsumer Implementation

        public int GetNextInstanceIndex() {
            if (_instanceIndexCursor < _seedInstanceCount)
                _instanceIndexCursor = _seedInstanceCount;

            while (StaticDataAccess.HasInstance(_instanceIndexCursor)
                   || DynamicDataAccess.HasInstance(_instanceIndexCursor))
                _instanceIndexCursor++;

            return _instanceIndexCursor;
        }

        /// <summary>
        /// Handles the in-game consequences Instances being loaded.
        /// Buffers them to be created as entities,
        /// and spawns an EventSurface for those who pass the proximymath check. 
        /// </summary>
        /// <param name="instances"></param>
        public void OnRuntimeInstancesLoaded(IReadOnlyList<(int, NonProceduralStaticInstanceEntry)> instances) {
            BufferEntitySpawnRequests(instances);

            foreach (var (instanceIndex, entry) in instances) {
                var preset = entry.PresetHash.ResolvePreset();

                var proximityChange = ProximityMath.IsWithinActivationRange(entry.Transform.Position, _lastPovPosition,
                    preset.interactionDistance);

                if (proximityChange == ProximityChange.Whitelist)
                    SpawnSurface(preset, entry.Transform, instanceIndex);
            }
        }

        /// <summary>
        /// Handles the in-game consequences Instances being loaded.
        /// Buffers them to be created as entities,
        /// and for those that pass the proximitymath check, spawns an EventSurface and calls ICreateHandler's OnHandleCreate actions.
        /// </summary>
        /// <param name="instances"></param>
        public void OnRuntimeInstancesCreated(IReadOnlyList<(int, NonProceduralStaticInstanceEntry)> instances) {
            BufferEntitySpawnRequests(instances);

            foreach (var (instanceIndex, entry) in instances) {
                var preset = entry.PresetHash.ResolvePreset();

                var proximityChange = ProximityMath.IsWithinActivationRange(entry.Transform.Position, _lastPovPosition,
                    preset.interactionDistance);

                if (proximityChange != ProximityChange.Whitelist)
                    continue;

                SpawnSurface(preset, entry.Transform, instanceIndex);

                preset.TryGetModules<ICreateHandler>(out var creationHandlers);

                foreach (var creationHandler in creationHandlers) {
                    creationHandler.HandleCreate(this, instanceIndex, entry.Transform);
                }
            }
        }

        public void OnDynamicObjectsLoaded(IReadOnlyList<(int, DynamicInstanceEntry)> loaded) {
            if (loaded.Count == 0)
                return;

            var entityRequests = new List<(int, uint, TransformData)>(loaded.Count);

            foreach (var (instanceIndex, entry) in loaded)
                entityRequests.Add((instanceIndex, entry.PresetHash, entry.Transform));

            BufferEntitySpawnRequests(entityRequests);
        }

        public void OnDynamicObjectsCreated(IReadOnlyList<(int, DynamicInstanceEntry)> creations) {
            foreach (var (instanceIndex, entry) in creations) {
                var preset = entry.PresetHash.ResolvePreset();

                if (preset == null || preset.eventSurfacePrefab == null) {
                    Log.Error($"OnDynamicInstancesCreated: missing preset or prefab for {entry.PresetHash}");

                    continue;
                }

                var instance = UnityEngine.Object.Instantiate(
                    preset.eventSurfacePrefab.gameObject,
                    entry.Transform.Position,
                    entry.Transform.RotAsQuaternion(),
                    _transform);

                if (instance.TryGetComponent<IEventSurface>(out var surface))
                    surface.Initialize(_implementer, instanceIndex, entry.PresetHash, entry.DataSlots);
            }
        }

        public void OnDynamicObjectEntityRequested(IReadOnlyList<(int, uint, TransformData)> entityRequests) =>
                BufferEntitySpawnRequests(entityRequests);

        public void OnDynamicObjectEntityDeleteRequested(IReadOnlyList<int> entityDeleteRequests) =>
                DestroyEntities(entityDeleteRequests, _dynamicQuery);

        public List<int> GetCurrentNonProceduralDynamicEntities() {
            var instanceList = new List<int>();
            var instanceIndices = _dynamicQuery.ToComponentDataArray<InstanceIndexComponent>(Allocator.TempJob);

            foreach (var instanceIndex in instanceIndices) {
                if (instanceIndex.Value >= _seedInstanceCount)
                    instanceList.Add(instanceIndex.Value);
            }

            instanceIndices.Dispose();

            return instanceList;
        }

        /// <summary>
        /// Handles the in-game consequences of an Instances being deleted.
        /// Destroys their associated entities,
        /// Destroys their associated EventSurfaces if they exist.
        /// And calls IDestructionHandler's OnHandleDelete actions.
        /// </summary>
        /// <param name="instanceIndices"></param>
        public void OnInstancesDeleted(IReadOnlyList<int> instanceIndices) {
            if (instanceIndices.Count == 0)
                return;

            foreach (var instanceIndex in instanceIndices) {
                if (!_eventSurfaces.TryGetValue(instanceIndex, out var surface))
                    continue;

                if (surface == null) {
                    Log.Error($"surface is null");

                    continue;
                }
                
                var preset = surface.Preset;

                preset.TryGetModules<IDeleteHandler>(out var destructionHandlers);

                foreach (var destructionHandler in destructionHandlers) {
                    destructionHandler.HandleDelete(this, instanceIndex, new TransformData(surface.GameObject.transform));
                }

                DespawnSurface(instanceIndex);
            }

            DestroyEntities(instanceIndices, _staticQuery);
        }

        public void OnInstanceDeleteResolve(int instanceIndex, Dictionary<InstanceDataKey, byte[]> dataSlots = null) {
            if (!TryGetCallbackParamsFromEventSurface(instanceIndex, 0, out var transformData,
                    out var preset, out var surface)) {
                if (!TryGetCallbackParamsFromEntity(instanceIndex, out transformData, out preset)) {
                    Log.Error("failed to find entity");
                    return;
                }
            }

            if (preset.TryGetModulesAcrossSurfaces<IDeleteResolver>(out var modules)) {
                foreach (var module in modules) {
                    module.ResolveDelete(dataSlots, transformData, this, instanceIndex);
                }
            }
        }

        public void OnInstanceDataChanged(int instanceIndex, InstanceDataKey key, IPackerObjectData data, byte context) {
            if (!TryGetCallbackParamsFromEventSurface(instanceIndex, key.SurfaceIndex, out var transformData,
                    out var preset, out var surface))
                return;

            surface.AlertChanged();

            data.InvokeChangeCallback(context, this, instanceIndex, preset, key.SurfaceIndex,
                transformData);
        }

        public void OnInstanceDataResolved(int instanceIndex, InstanceDataKey key, IPackerObjectData data, byte context) {
            if (!TryGetCallbackParamsFromEventSurface(instanceIndex, key.SurfaceIndex, out var transformData,
                    out var preset, out var surface)) {
                if (!TryGetCallbackParamsFromEntity(instanceIndex, out transformData, out preset)) {
                    Log.Warn("failed to find entity");

                    return;
                }
            }

            data.InvokeResolveCallback(context, this, instanceIndex, preset, key.SurfaceIndex,
                transformData);
        }

        /// <summary>
        /// /// Helper Method to get Transform and Preset from Event Surface
        /// </summary>
        /// <param name="instanceIndex"></param>
        /// <param name="surfaceIndex"></param>
        /// <param name="transformData"></param>
        /// <param name="preset"></param>
        /// <param name="surface"></param>
        /// <returns></returns>
        private bool TryGetCallbackParamsFromEventSurface(
            int instanceIndex, int surfaceIndex, out TransformData transformData, out ObjectPreset preset,
            out IEventSurface surface) {
            surface = null;
            transformData = default;
            preset = null;

            if (!_eventSurfaces.TryGetValue(instanceIndex, out var mainSurface)) return false;

            if (!mainSurface.TryGetEventSurfaceByIndex(surfaceIndex, out surface)) {
                Log.Error($"Surface not found for instanceIndex {instanceIndex} and surfaceIndex  {surfaceIndex}");

                return false;
            }

            transformData = new TransformData(surface.Transform);
            preset = surface.Preset;

            return true;
        }

        /// <summary>
        /// Helper Method to get Transform and Preset from Entity
        /// </summary>
        /// <param name="instanceIndex"></param>
        /// <param name="transformData"></param>
        /// <param name="preset"></param>
        /// <returns></returns>
        private bool TryGetCallbackParamsFromEntity(
            int instanceIndex, out TransformData transformData, out ObjectPreset preset) {
            transformData = default;
            preset = null;
            var instanceIndices = _staticQuery.ToComponentDataArray<InstanceIndexComponent>(Allocator.Temp);
            var entities = _staticQuery.ToEntityArray(Allocator.Temp);

            for (var i = 0; i < instanceIndices.Length; i++) {
                if (instanceIndices[i].Value != instanceIndex) continue;

                var em = World.DefaultGameObjectInjectionWorld.EntityManager;

                transformData = new TransformData(em.GetComponentData<LocalTransform>(entities[i]));
                preset = em.GetSharedComponentManaged<PresetHashComponent>(entities[i]).Value.ResolvePreset();

                instanceIndices.Dispose();
                entities.Dispose();

                return true;
            }

            instanceIndices.Dispose();
            entities.Dispose();

            return false;
        }

        #endregion IObjectInstanceConsumer Implementation

        #region EventSurfaces

        private void SpawnSurface(ObjectPreset preset, TransformData transformData, int instanceIndex) {
            if (_eventSurfaces.ContainsKey(instanceIndex)) {
                Log.Error($"Instance index {instanceIndex} already a key in eventSurfacesDictionary");

                return;
            }

            var eventSurface = UnityEngine.Object.Instantiate(
                preset.eventSurfacePrefab,
                transformData.Position,
                transformData.RotAsQuaternion(),
                _transform
            ).GetComponent<IEventSurface>();
            eventSurface.GameObject.name = $"{preset.name} Event Surface {instanceIndex}";
            eventSurface.GameObject.transform.localScale = transformData.ScaleAsVector3();
            eventSurface.Initialize(_implementer, instanceIndex, preset.Hash);
            _eventSurfaces[instanceIndex] = eventSurface;
        }

        /// <summary>
        /// Destroys an event surface if it exists.
        /// Note remove is allowed to fail safely.
        /// </summary>
        /// <param name="instanceIndex"></param>
        private void DespawnSurface(int instanceIndex) {
            if (!_eventSurfaces.Remove(instanceIndex, out var surface))
                return;

            UnityEngine.Object.Destroy(surface.GameObject);
        }

        public void SleepDynamicObject(int instanceIndex, DynamicInstanceEntry entry, IEventSurface eventSurface) =>
                DynamicDataAccess.Sleep(instanceIndex, entry, eventSurface);

        public void SetRuntimeDynamicEntry(int instanceIndex, DynamicInstanceEntry entry) =>
                DynamicDataAccess.SetRuntimeDynamicEntry(instanceIndex, entry);

        public void RaiseSetRuntimeDynamicEntryRequested() => OnSetRuntimeDynamicEntryRequested?.Invoke();

        #endregion EventSurfaces

        #region Distance Queries

        public void DynamicDistanceQuery(float3[] povs) {
            OnDynamicProximityEval?.Invoke(povs);

            var maxCapacity = _dynamicQuery.CalculateEntityCount();

            if (maxCapacity == 0)
                return;

            var existingEventSurfaces = new NativeHashSet<int>(1, Allocator.TempJob);

            var entities = _dynamicQuery.ToEntityArray(Allocator.TempJob);
            var transforms = _dynamicQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
            var thresholds = _dynamicQuery.ToComponentDataArray<ProximityThresholdComponent>(Allocator.TempJob);
            var instanceIndices = _dynamicQuery.ToComponentDataArray<InstanceIndexComponent>(Allocator.TempJob);
            var povPositions = new NativeArray<float3>(povs, Allocator.TempJob);

            var instancesToAwaken = new NativeParallelHashSet<int>(maxCapacity, Allocator.TempJob);
            var instancesToSleep = new NativeParallelHashSet<int>(maxCapacity, Allocator.TempJob);

            var proximityJob = new EventSurfaceProximityJob {
                PovPositions = povPositions,
                Transforms = transforms,
                Thresholds = thresholds,
                InstanceIndices = instanceIndices,
                ExistingEventSurfaces = existingEventSurfaces,
                InstancesToAwaken = instancesToAwaken.AsParallelWriter(),
                InstancesToSleep = instancesToSleep.AsParallelWriter()
            };

            var jobHandle = proximityJob.Schedule(maxCapacity, 64);
            jobHandle.Complete();

            foreach (var toAwaken in instancesToAwaken)
                DynamicDataAccess.Awaken(instanceIndices[toAwaken].Value);

            povPositions.Dispose();
            transforms.Dispose();
            thresholds.Dispose();
            instanceIndices.Dispose();
            entities.Dispose();
            existingEventSurfaces.Dispose();
            instancesToAwaken.Dispose();
            instancesToSleep.Dispose();
        }

        public bool StaticEntityDistanceQuery(float3 localPov) {
            var maxCapacity = _staticQuery.CalculateEntityCount();

            if (_hasEvaluatedStaticProximity
                && maxCapacity == _lastEvaluatedStaticEntityCount
                && math.distancesq(localPov, _lastPovPosition)
                   < SurfaceQueryMovementThreshold * SurfaceQueryMovementThreshold)
                return false;

            _hasEvaluatedStaticProximity = true;
            _lastEvaluatedStaticEntityCount = maxCapacity;
            _lastPovPosition = localPov;

            var existingEventSurfaces = new NativeHashSet<int>(_eventSurfaces.Count, Allocator.TempJob);

            foreach (var key in _eventSurfaces.Keys)
                existingEventSurfaces.Add(key);

            var entities = _staticQuery.ToEntityArray(Allocator.TempJob);
            var transforms = _staticQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
            var thresholds = _staticQuery.ToComponentDataArray<ProximityThresholdComponent>(Allocator.TempJob);
            var instanceIndices = _staticQuery.ToComponentDataArray<InstanceIndexComponent>(Allocator.TempJob);
            var povPositions = new NativeArray<float3>(1, Allocator.TempJob);
            povPositions[0] = localPov;

            var instancesToAwaken = new NativeParallelHashSet<int>(maxCapacity, Allocator.TempJob);
            var instancesToSleep = new NativeParallelHashSet<int>(maxCapacity, Allocator.TempJob);

            var proximityJob = new EventSurfaceProximityJob {
                PovPositions = povPositions,
                Transforms = transforms,
                Thresholds = thresholds,
                InstanceIndices = instanceIndices,
                ExistingEventSurfaces = existingEventSurfaces,
                InstancesToAwaken = instancesToAwaken.AsParallelWriter(),
                InstancesToSleep = instancesToSleep.AsParallelWriter()
            };
            var jobHandle = proximityJob.Schedule(maxCapacity, 64);
            jobHandle.Complete();

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            foreach (var toAwaken in instancesToAwaken) {
                var presetHash = em.GetSharedComponent<PresetHashComponent>(entities[toAwaken]);

                SpawnSurface(presetHash.Value.ResolvePreset(), new TransformData(transforms[toAwaken]),
                    instanceIndices[toAwaken].Value);
            }

            foreach (var toSleep in instancesToSleep)
                DespawnSurface(instanceIndices[toSleep].Value);

            povPositions.Dispose();
            transforms.Dispose();
            thresholds.Dispose();
            instanceIndices.Dispose();
            entities.Dispose();
            existingEventSurfaces.Dispose();
            instancesToAwaken.Dispose();
            instancesToSleep.Dispose();

            return true;
        }

        #endregion Distance Queries

        #region IDisposable

        public void Dispose() => DestroyAllEntities();

        #endregion IDisposable
    }
}