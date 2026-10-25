// Copyright 2026 Spellbound Studio Inc.

using System;
using System.Collections.Generic;
using Spellbound.Core.Logging;
using Spellbound.Core.ObjectData;
using Spellbound.Core.ObjectHandling;
using Spellbound.Core.Objects;
using Spellbound.Core.Packing;
using Spellbound.Core.ModuleContracts;
using UnityEngine;

namespace Spellbound.Core.Surfaces {
    [RequireComponent(typeof(Collider))]
    public class StaticEventSurface : MonoBehaviour, IEventSurface {
        [SerializeField, Tooltip("Decide your own surface index schema.")]
        private byte surfaceIndex;

        public Vector3 Position => transform.position;

        public GameObject GameObject => gameObject;

        public Transform Transform => transform;

        private IObjectParent _parent;
        private int _instanceIndex;

        private Dictionary<int, IEventSurface> _childEventSurfaces = new();

        public ObjectPreset Preset { get; private set; }

        public int Initialize(
            IObjectParent objectParent, int entityIndex, uint presetHash,
            Dictionary<InstanceDataKey, byte[]> dataSlots = null) {
            _parent = objectParent;
            _instanceIndex = entityIndex;
            Preset = presetHash.ResolvePreset();

            var childSurfaces = GetComponentsInChildren<StaticEventSurface>(true);

            foreach (var childSurface in childSurfaces) {
                if (childSurface == this)
                    continue;

                var childSurfaceIndex = childSurface.Initialize(_parent, _instanceIndex, Preset.Hash, dataSlots);

                if (!_childEventSurfaces.TryAdd(childSurfaceIndex, childSurface))
                    Log.Error($"Duplicate surfaceIndex {childSurfaceIndex} on {childSurface.gameObject.name}");
            }

            return surfaceIndex;
        }

        public void DebugQueryPing() =>
                Debug.Log($"Pinging Event Surface for {Preset.name} " +
                          $"index {_instanceIndex} " +
                          $"and surface index {surfaceIndex}");
        
        public bool Dispatch<TContext>(TContext dispatch) where TContext : IPackerDispatch {
            if (Preset == null)
                return false;
            
            return Preset.TryGetModule(out IDispatch<TContext> handler, surfaceIndex) 
                   && handler.OnDispatch(dispatch, this);
        }

        public event Action OnChanged;

        public void AlertChanged() => OnChanged?.Invoke();

        public bool TryGetEventSurfaceByIndex(int desiredSurfaceIndex, out IEventSurface surface) {
            if (desiredSurfaceIndex == surfaceIndex) {
                surface = this;

                return true;
            }

            return _childEventSurfaces.TryGetValue(desiredSurfaceIndex, out surface);
        }
        
        public bool TryRead<T>(out T data) where T : IPackerObjectData, new() {
            return _parent.ObjectParent.TryReadData(_instanceIndex, Preset.Hash, surfaceIndex,  out data);
        }

        public void Write<T>(T data, byte contextIn) where T : IPackerObjectData, new() {
            _parent.ObjectParent.WriteData(_instanceIndex, Preset.Hash, surfaceIndex, data, contextIn);
        }

        public void Delta<TData, TDispatch>(TDispatch dispatch) where TData : IPackerObjectData, new()
                where TDispatch : IPackerDispatch, new() {
            _parent.ObjectParent.Delta<TData, TDispatch>(_instanceIndex, Preset.Hash, surfaceIndex, dispatch);
        }

        public void Destroy() {
            _parent.ObjectParent.DeleteInstance(_instanceIndex);
        }
    }
}