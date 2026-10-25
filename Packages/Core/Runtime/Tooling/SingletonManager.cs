// Copyright 2026 Spellbound Studio Inc.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spellbound.Core.Tooling {
    /// <summary>
    /// Centralized Management of Singletons to handle/log errors better.
    /// </summary>
    public static class SingletonManager {
        private static readonly Dictionary<Type, object> Singletons = new();

        /// <summary>
        /// Pretty sure this means we don't need to unregister.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void ClearAll() => Singletons.Clear();

        public static void RegisterSingleton<T>(T singleton) where T : class => Singletons[typeof(T)] = singleton;

        public static void UnregisterSingleton<T>() where T : class => Singletons.Remove(typeof(T));

        public static T GetSingletonInstance<T>() where T : class {
            if (!Singletons.TryGetValue(typeof(T), out var instance))
                throw new KeyNotFoundException($"Singleton of type {typeof(T)} not found");

            return (T)instance;
        }

        public static bool TryGetSingletonInstance<T>(out T instance) where T : class {
            if (!Singletons.TryGetValue(typeof(T), out var obj)) {
                instance = null;

                return false;
            }

            instance = (T)obj;

            return true;
        }
    }
}