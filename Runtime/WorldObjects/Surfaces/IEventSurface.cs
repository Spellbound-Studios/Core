// Copyright 2026 Spellbound Studio Inc.

using System;
using System.Collections.Generic;
using Spellbound.Core.ObjectData;
using Spellbound.Core.ObjectHandling;
using Spellbound.Core.Objects;
using UnityEngine;

namespace Spellbound.Core.Surfaces {
    public interface IEventSurface : IDispatchSurface {
        GameObject GameObject { get; }

        Transform Transform { get; }

        ObjectPreset Preset { get; }

        int Initialize(
            IObjectParent objectParent, int entityIndex, uint presetHash,
            Dictionary<InstanceDataKey, byte[]> dataSlots = null);

        void DebugQueryPing();
        
        public event Action OnChanged;

        void AlertChanged();
        
        bool TryRead<T>(out T data) where T : IPackerObjectData, new();
        void Write<T>(T data, byte contextIn) where T : IPackerObjectData, new();

        void Delta<TData, TDispatch>(TDispatch dispatch)
                where TData : IPackerObjectData, new() where TDispatch : IPackerDispatch, new();
        void Destroy();

        bool TryGetEventSurfaceByIndex(int desiredSurfaceIndex, out IEventSurface surface);
    }
}