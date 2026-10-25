// Copyright 2026 Spellbound Studio Inc.

using System;
using System.Collections.Generic;
using Spellbound.Core.Logging;
using Spellbound.Core.Modules;
using Spellbound.Core.ModuleContracts;
using Spellbound.Core.Registries;
using Spellbound.Core.Surfaces;
using UnityEngine;

namespace Spellbound.Core.Objects {
    /// <summary>
    /// ObjectPreset is the root of most "objects" that a player would want in game. It comes equipped with some baseline
    /// utility but also offers a modules field where additional modules that inherit from PresetModule can be loaded
    /// like MonoBehaviours.
    /// </summary>
    [CreateAssetMenu(fileName = "Object Preset", menuName = "Spellbound/Presets/ObjectPreset")]
    public class ObjectPreset : HashedScriptableObject {
        public string objectName;
        public string objectDescription;
        public bool isDynamic;

        public GameObject bakePrefab; // not the proxy, it is the thing that bakes into an entity
        public GameObject eventSurfacePrefab;
        public Vector2 interactionDistance = new(50, 70);

        [SerializeField] public List<PresetSurface> surfaceModules = new();

        private const int AllSurfaces = -1;

        private readonly Dictionary<(int surfaceIndex, Type moduleType), object> _moduleCache = new();

        private void OnEnable() => RewireModules();

        private void RewireModules() {
            _moduleCache.Clear();

            if (surfaceModules == null)
                return;

            for (byte i = 0; i < surfaceModules.Count; i++) {
                var surface = surfaceModules[i];

                if (surface?.presetModules == null)
                    continue;

                foreach (var module in surface.presetModules)
                    module?.OnPresetLoaded(this, i);
            }
        }

        public bool TryGetModule<T>(out T result, byte surfaceIndex = 0) where T : class {
            var modules = LookupModules<T>(surfaceIndex);

            if (modules.Count > 0) {
                result = modules[0];

                return true;
            }

            result = null;

            return false;
        }

        public bool TryGetModules<T>(out IReadOnlyList<T> results, byte surfaceIndex = 0) where T : class {
            results = LookupModules<T>(surfaceIndex);

            return results.Count > 0;
        }

        public bool TryGetModulesAcrossSurfaces<T>(out IReadOnlyList<T> results) where T : class {
            results = LookupModules<T>(AllSurfaces);

            return results.Count > 0;
        }

        private IReadOnlyList<T> LookupModules<T>(int surfaceIndex) where T : class {
            var key = (surfaceIndex, typeof(T));

            if (_moduleCache.TryGetValue(key, out var cached))
                return (IReadOnlyList<T>)cached;

            List<T> matches = null;

            if (surfaceModules != null) {
                if (surfaceIndex == AllSurfaces) {
                    foreach (var surface in surfaceModules)
                        CollectModules(surface, ref matches);
                }
                else if (surfaceIndex >= 0 && surfaceIndex < surfaceModules.Count)
                    CollectModules(surfaceModules[surfaceIndex], ref matches);
            }

            IReadOnlyList<T> results = matches ?? (IReadOnlyList<T>)Array.Empty<T>();
            _moduleCache[key] = results;

            return results;

            static void CollectModules(PresetSurface surface, ref List<T> collected) {
                if (surface?.presetModules == null)
                    return;

                foreach (var module in surface.presetModules) {
                    if (module is not T match)
                        continue;

                    collected ??= new List<T>();
                    collected.Add(match);
                }
            }
        }

        /// <summary>
        /// Returns all modules across all surfaces.
        /// </summary>
        public IEnumerable<PresetModule> GetAllModules() {
            foreach (var surface in surfaceModules)
            foreach (var pm in surface.presetModules) {
                if (pm != null)
                    yield return pm;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Validates the module composition: the event surface prefab must implement IEventSurface, each surface
        /// allows one IDispatch&lt;T&gt; per T, and one ITooltipHandler of any kind.
        /// </summary>
        protected override void OnValidateAsset() {
            if (eventSurfacePrefab != null && eventSurfacePrefab.GetComponent<IEventSurface>() == null)
                Log.Error("EventSurfacePrefab not found");

            foreach (var surface in surfaceModules) {
                if (surface.presetModules == null) continue;

                var seenDispatchTypes = new HashSet<Type>();
                var hasMouseoverHandler = false;

                foreach (var module in surface.presetModules) {
                    if (module == null) continue;

                    foreach (var iface in module.GetType().GetInterfaces()) {
                        if (!iface.IsGenericType) continue;
                        if (iface.GetGenericTypeDefinition() != typeof(IDispatch<>)) continue;

                        if (!seenDispatchTypes.Add(iface)) {
                            Log.Error(
                                $"Duplicate {iface.Name}<{iface.GenericTypeArguments[0].Name}> " +
                                $"on surface '{surface.surfaceName}' in preset '{name}'");
                        }
                    }

                    if (module is ITooltipHandler) {
                        if (hasMouseoverHandler) {
                            Log.Error(
                                $"Duplicate ITooltipHandler on surface '{surface.surfaceName}' in preset '{name}'");
                        }

                        hasMouseoverHandler = true;
                    }
                }
            }
        }
#endif
    }
}