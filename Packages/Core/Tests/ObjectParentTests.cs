// Copyright 2026 Spellbound Studio Inc.

using System.Collections.Generic;
using NUnit.Framework;
using Spellbound.Core.ECS;
using Spellbound.Core.ObjectData;
using Spellbound.Core.ObjectHandling;
using Spellbound.Core.Surfaces;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Spellbound.Core.Tests {
    public class ObjectParentTests {
        private class FakeStaticDataAccess : IObjectDataAccess {
            public readonly HashSet<int> Indices = new();

            public void SetConsumer(IObjectInstanceConsumer consumer) { }

            public Dictionary<int, NonProceduralStaticInstanceEntry> GetAllRuntimeInstances() => new();

            public IReadOnlyCollection<int> GetAllSeedInstanceDeletions() => new List<int>();

            public void CreateRuntimeInstance(uint presetHash, Vector3 position, Vector3 rotation, int scale) { }

            public bool HasInstance(int instanceIndex) => Indices.Contains(instanceIndex);

            public bool IsDeleted(int instanceIndex) => false;

            public bool TryRead<T>(int instanceIndex, byte eventSurfaceIndex, out T data)
                    where T : IPackerObjectData, new() {
                data = default;

                return false;
            }

            public bool TryReadAllBySurface(int instanceIndex, byte eventSurfaceIndex, out List<IPackerObjectData> data) {
                data = null;

                return false;
            }

            public bool TryReadAll(int instanceIndex, out List<IPackerObjectData> data) {
                data = null;

                return false;
            }

            public void Write<T>(int instanceIndex, uint presetHash, byte eventSurfaceIndex, T newData, byte contextIn)
                    where T : IPackerObjectData, new() { }

            public void Delta<TData, TDispatch>(
                int instanceIndex, uint presetHash, byte eventSurfaceIndex, TDispatch dispatch)
                    where TData : IPackerObjectData, new() where TDispatch : IPackerDispatch, new() { }

            public void DeleteInstance(int instanceIndex) { }
        }

        private class FakeDynamicDataAccess : IDynamicDataAccess {
            public readonly HashSet<int> Indices = new();

            public void SetConsumer(IObjectInstanceConsumer consumer) { }

            public bool HasInstance(int instanceIndex) => Indices.Contains(instanceIndex);

            public Dictionary<int, DynamicInstanceEntry> GetAllRuntimeDynamicInstances() => new();

            public void CreateRuntimeObject(
                uint presetHash, Vector3 position, Vector3 rotation, int scale,
                List<(InstanceDataKey, byte[])> dataSlots = null) { }

            public void Awaken(int instanceIndex) { }

            public void Sleep(int instanceIndex, DynamicInstanceEntry entry, IEventSurface eventSurface) { }

            public void SetRuntimeDynamicEntry(int instanceIndex, DynamicInstanceEntry entry) { }
        }

        private World _previousWorld;
        private World _world;
        private FakeStaticDataAccess _staticDataAccess;
        private FakeDynamicDataAccess _dynamicDataAccess;
        private ObjectParent _objectParent;

        [SetUp]
        public void SetUp() {
            _previousWorld = World.DefaultGameObjectInjectionWorld;
            _world = new World("ObjectParentTests");
            World.DefaultGameObjectInjectionWorld = _world;

            _staticDataAccess = new FakeStaticDataAccess();
            _dynamicDataAccess = new FakeDynamicDataAccess();
            _objectParent = new ObjectParent(
                null, null, _staticDataAccess, _dynamicDataAccess, Vector3Int.zero, Entity.Null);
        }

        [TearDown]
        public void TearDown() {
            _objectParent.Dispose();
            _world.Dispose();
            World.DefaultGameObjectInjectionWorld = _previousWorld;
        }

        [Test]
        public void ReturnsFirstFreeIndexAtOrAboveSeedCount() {
            _objectParent.SetSeedInstanceCount(3);
            _staticDataAccess.Indices.Add(3);
            _staticDataAccess.Indices.Add(4);

            Assert.AreEqual(5, _objectParent.GetNextInstanceIndex());
        }

        [Test]
        public void SkipsIndicesHeldByEitherStore() {
            _staticDataAccess.Indices.Add(0);
            _dynamicDataAccess.Indices.Add(1);

            Assert.AreEqual(2, _objectParent.GetNextInstanceIndex());
        }

        [Test]
        public void DoesNotReuseFreedLowerIndex() {
            _objectParent.SetSeedInstanceCount(3);
            _staticDataAccess.Indices.Add(3);
            _staticDataAccess.Indices.Add(4);

            var first = _objectParent.GetNextInstanceIndex();
            _staticDataAccess.Indices.Add(first);
            _staticDataAccess.Indices.Remove(3);

            Assert.AreEqual(first + 1, _objectParent.GetNextInstanceIndex());
        }

        [Test]
        public void SnapsForwardWhenSeedCountArrivesLate() {
            Assert.AreEqual(0, _objectParent.GetNextInstanceIndex());

            _objectParent.SetSeedInstanceCount(5);

            Assert.AreEqual(5, _objectParent.GetNextInstanceIndex());
        }

        [Test]
        public void StaticProximityQueryEvaluatesOnlyPastMovementThreshold() {
            Assert.IsTrue(_objectParent.StaticEntityDistanceQuery(float3.zero));
            Assert.IsFalse(_objectParent.StaticEntityDistanceQuery(new float3(2f, 0f, 0f)));
            Assert.IsTrue(_objectParent.StaticEntityDistanceQuery(new float3(5f, 0f, 0f)));
        }

        [Test]
        public void StaticProximityQueryEvaluatesWhenEntityCountChanges() {
            Assert.IsTrue(_objectParent.StaticEntityDistanceQuery(float3.zero));
            Assert.IsFalse(_objectParent.StaticEntityDistanceQuery(float3.zero));

            CreateStaticEntity(new float3(10000f, 0f, 0f));

            Assert.IsTrue(_objectParent.StaticEntityDistanceQuery(float3.zero));
            Assert.IsFalse(_objectParent.StaticEntityDistanceQuery(float3.zero));
        }

        private void CreateStaticEntity(float3 position) {
            var entityManager = _world.EntityManager;
            var entity = entityManager.CreateEntity();

            entityManager.AddSharedComponent(entity, new ChunkParentComponent { ChunkCoord = int3.zero });
            entityManager.AddComponentData(entity, LocalTransform.FromPosition(position));
            entityManager.AddComponentData(entity, new ProximityThresholdComponent { Value = new float2(50f, 70f) });
            entityManager.AddComponentData(entity, new InstanceIndexComponent { Value = 0 });
            entityManager.AddSharedComponent(entity, new PresetHashComponent { Value = 1u });
        }
    }
}
