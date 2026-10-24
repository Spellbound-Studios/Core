// Copyright 2026 Spellbound Studio Inc.

using Spellbound.Core.Logging;
using Spellbound.Core.ObjectHandling;
using Spellbound.Core.Objects;
using Spellbound.Core.Packing;
using Spellbound.Core.ModuleContracts;

namespace Spellbound.Core.ObjectData {
    public static class ObjectDataExtensions {
        public static IPackerObjectData GetDefaultData<T>(
            this T data, ObjectPreset preset, byte surfaceIndex, byte level = 1)
                where T : IPackerObjectData {
            if (preset.TryGetModule<IDefaultDataProvider<T>>(out var provider, surfaceIndex))
                return provider.GetDefaultData(preset, level);

            return data.GetEmptyData();
        }
        
        
        public static T ApplyDelta<T, TDelta>(
            this T data, TDelta delta, ObjectPreset preset, byte surfaceIndex, out byte context, out ISmartPacker consequence)
                where T : IPackerObjectData
                where TDelta : ISmartPacker {
            if (!preset.TryGetModule<IApplyDelta<T, TDelta>>(out var module, surfaceIndex)) {
                context = 0;
                consequence = null;
                Log.Warn($"ApplyDelta: '{preset.name}' surface {surfaceIndex} has no " +
                         $"IApplyDelta<{typeof(T).Name}, {typeof(TDelta).Name}>; delta dropped.");
                return data;
            }
            return module.ApplyDelta(data, delta, preset, surfaceIndex, out context, out consequence);
        }

        public static void ChangeCallback<T>(
            this T data, byte context, ObjectParent parent,
            int instanceIndex, ObjectPreset preset, byte surfaceIndex, TransformData transformData)
                where T : IPackerObjectData {
            if (!preset.TryGetModules<IChangeHandler<T>>(out var modules, surfaceIndex))
                return;

            foreach (var module in modules)
                module.HandleChange(data, context, parent, instanceIndex, transformData);
        }

        public static void ResolveCallback<T>(
            this T data, byte context, ObjectParent parent,
            int instanceIndex, ObjectPreset preset, byte surfaceIndex, TransformData transformData)
                where T : IPackerObjectData {
            if (!preset.TryGetModules<IChangeResolver<T>>(out var modules, surfaceIndex))
                return;

            foreach (var module in modules)
                module.ResolveChange(data, context, parent, instanceIndex, transformData);
        }
    }
}