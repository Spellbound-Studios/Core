// Copyright 2026 Spellbound Studio Inc.

using System;
using System.Collections.Generic;
using System.Reflection;
using Spellbound.Core.Hashing;
using Spellbound.Core.Logging;
using Spellbound.Core.Registries;
using Spellbound.Core.Tooling;
using UnityEngine;

namespace Spellbound.Core.Packing {
    /// <summary>
    /// Discovers every concrete <see cref="ISmartPacker"/> via its static <c>Id</c> field and resolves
    /// type / hash in all directions. A concrete ISmartPacker without an Id field, a duplicate id, or a
    /// hash collision throws at startup.
    /// </summary>
    public static class SmartPackerRegistry {
        private static readonly HashRegistry<ISmartPacker> Registry = new();
        private static readonly Dictionary<Type, uint> HashesByType = new();
        private static bool _isLoaded;

        #region Lifecycle

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlaySession() {
            Registry.Clear();
            HashesByType.Clear();
            _isLoaded = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void WarmUp() => EnsureLoaded();

        private static void EnsureLoaded() {
            if (_isLoaded) return;
            _isLoaded = true;

            foreach (var assembly in AssemblyScanning.ScannableAssemblies()) {
                foreach (var type in AssemblyScanning.LoadableTypes(assembly)) {
                    if (!typeof(ISmartPacker).IsAssignableFrom(type)) continue;
                    if (type.IsAbstract || type.IsInterface) continue;

                    var attribute = type.GetCustomAttribute<PackerIdAttribute>();

                    if (attribute == null)
                        throw new Exception(
                            $"SmartPackerRegistry: '{type.FullName}' implements ISmartPacker but has no [PackerId] attribute.");

                    var id = attribute.Id;

                    if (string.IsNullOrEmpty(id))
                        throw new Exception(
                            $"SmartPackerRegistry: '{type.FullName}' has an empty Id.");

                    var hash = StableHash.Fnv1A32(id);

                    if (Registry.Contains(hash))
                        throw new Exception(
                            $"SmartPackerRegistry: hash collision or duplicate Id '{id}' (hash: {hash})");

                    HashesByType[type] = hash;
                    var prototype = (ISmartPacker)Activator.CreateInstance(type);
                    Registry.Add(prototype);

                    Log.Debug($"Registered '{id}' ({type.Name}) -> hash {hash}");
                }
            }
        }

        #endregion

        #region API

        public static uint GetHash(Type type) {
            EnsureLoaded();

            if (HashesByType.TryGetValue(type, out var hash)) return hash;
            throw new Exception($"SmartPackerRegistry: '{type.FullName}' is not registered.");
        }
        
        public static uint GetHash<T>() where T : ISmartPacker => Cache<T>.Value;

        private static class Cache<T> where T : ISmartPacker {
            public static readonly uint Value = GetHash(typeof(T));
        }


        /// <summary>
        /// Creates a new instance of the registered type for the given hash; false if not registered.
        /// </summary>
        public static bool TryCreateInstance(uint hash, out ISmartPacker instance) {
            EnsureLoaded();

            if (Registry.TryGet(hash, out var prototype)) {
                instance = prototype.CreateNewInstance();
                return true;
            }
            instance = null;
            return false;
        }

        #endregion
    }
}