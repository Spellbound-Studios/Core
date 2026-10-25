// Copyright 2026 Spellbound Studio Inc.

using System.Collections.Generic;
using Spellbound.Core.Registries;
using UnityEngine;

namespace Spellbound.Core.Objects {
    public static class PresetRegistry {
        private static readonly ResourceRegistry<ObjectPreset> Registry = new(string.Empty);

        public static IReadOnlyList<ObjectPreset> All => Registry.All;

        #region Lifecycle

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlaySession() => Registry.Reset();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void WarmUp() => Registry.EnsureLoaded();

        #endregion

        #region API

        public static ObjectPreset ResolvePreset(uint hash) => Registry.Get(hash);

        public static bool TryResolvePreset(uint hash, out ObjectPreset preset) => Registry.TryGet(hash, out preset);

        public static bool Contains(uint hash) => Registry.Contains(hash);

        public static void Reload() => Registry.Reload();

        #endregion
    }
}
