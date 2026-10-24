// Copyright 2026 Spellbound Studio Inc.

using System;
using System.Collections.Generic;
using System.Reflection;
using Spellbound.Core.ModuleContracts;
using Spellbound.Core.Objects;
using Spellbound.Core.Packing;
using Spellbound.Core.Tooling;
using UnityEngine;

namespace Spellbound.Core.ObjectData {
    /// <summary>
    /// Resolves how to apply a delta to instance data when only the wire hashes are known. At load it finds
    /// every <see cref="IApplyDelta{TData,TDelta}"/> a module declares and bakes a typed apply function for that
    /// (data, delta) pair, keyed by the two <see cref="SmartPackerRegistry"/> hashes that already travel on the
    /// wire. The receive path looks the function up by those hashes and runs it, so the concrete types are
    /// recovered without a per-struct switch and the structs never box. Built before the first scene loads, like
    /// the other registries; the table is one entry per delta-capable pair (a handful), so the cost is trivial
    /// and paid during loading, never mid-game.
    /// </summary>
    public static class DeltaResolver {
        /// <summary>
        /// Applies <paramref name="deltaBytes"/> to the data stored as <paramref name="currentBytes"/> (null when
        /// the instance has no data yet, in which case the module's default data is used). Returns the repacked
        /// result, plus the materialized result for the consumer callback, the change context, and any
        /// consequence to award.
        /// </summary>
        public delegate byte[] DeltaApply(
            byte[] currentBytes, byte[] deltaBytes, ObjectPreset preset, byte surfaceIndex,
            out IPackerObjectData result, out byte context, out ISmartPacker consequence);

        private static readonly Dictionary<(uint dataHash, uint deltaHash), DeltaApply> Table = new();
        private static bool _isLoaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void WarmUp() => EnsureLoaded();

        /// <summary>
        /// The apply function for a (data hash, delta hash) pair, or false if no module declares that pair.
        /// </summary>
        public static bool TryGet(uint dataHash, uint deltaHash, out DeltaApply apply) {
            EnsureLoaded();

            return Table.TryGetValue((dataHash, deltaHash), out apply);
        }

        /// <summary>
        /// Applies a delta whose type the caller already knows — the controller path, where the dispatch is
        /// still a live typed value and never needs serializing. Unpacks the stored current data (or builds the
        /// module default), applies it, and repacks the result. Shared with the reflection path below.
        /// </summary>
        public static byte[] ApplyTyped<TData, TDelta>(
            byte[] currentBytes, TDelta delta, ObjectPreset preset, byte surfaceIndex,
            out IPackerObjectData result, out byte context, out ISmartPacker consequence)
                where TData : IPackerObjectData, new()
                where TDelta : ISmartPacker, new() {
            TData current;

            if (currentBytes != null)
                Packer.TrySmartUnpack(currentBytes, out current);
            else if (preset.TryGetModule<IDefaultDataProvider<TData>>(out var provider, surfaceIndex))
                current = provider.GetDefaultData(preset);
            else
                current = new TData();

            var applied = current.ApplyDelta(delta, preset, surfaceIndex, out context, out consequence);
            result = applied;

            return Packer.SmartToBytes(applied);
        }

        /// <summary>
        /// The receive-path body, baked once per pair via <see cref="MethodInfo.MakeGenericMethod"/>. The type
        /// parameters appear only inside the body, never in the signature, so one <see cref="DeltaApply"/>
        /// delegate type fits every closed version. Unpacks the delta bytes, then defers to
        /// <see cref="ApplyTyped{TData,TDelta}"/>.
        /// </summary>
        private static byte[] Run<TData, TDelta>(
            byte[] currentBytes, byte[] deltaBytes, ObjectPreset preset, byte surfaceIndex,
            out IPackerObjectData result, out byte context, out ISmartPacker consequence)
                where TData : IPackerObjectData, new()
                where TDelta : ISmartPacker, new() {
            Packer.TrySmartUnpack(deltaBytes, out TDelta delta);

            return ApplyTyped<TData, TDelta>(
                currentBytes, delta, preset, surfaceIndex, out result, out context, out consequence);
        }

        private static void EnsureLoaded() {
            if (_isLoaded)
                return;

            _isLoaded = true;

            var open = typeof(DeltaResolver).GetMethod(nameof(Run), BindingFlags.NonPublic | BindingFlags.Static);

            foreach (var assembly in AssemblyScanning.ScannableAssemblies()) {
                foreach (var type in AssemblyScanning.LoadableTypes(assembly)) {
                    if (!type.IsClass || type.IsAbstract)
                        continue;

                    foreach (var iface in type.GetInterfaces()) {
                        if (!iface.IsGenericType || iface.GetGenericTypeDefinition() != typeof(IApplyDelta<,>))
                            continue;

                        var args = iface.GetGenericArguments();
                        var key = (SmartPackerRegistry.GetHash(args[0]), SmartPackerRegistry.GetHash(args[1]));

                        if (Table.ContainsKey(key))
                            continue;

                        var closed = open?.MakeGenericMethod(args[0], args[1]);
                        Table[key] = (DeltaApply)closed?.CreateDelegate(typeof(DeltaApply));
                    }
                }
            }
        }
    }
}
