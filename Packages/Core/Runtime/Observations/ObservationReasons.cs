// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using Spellbound.Core.Hashing;
using Spellbound.Core.Logging;
using Spellbound.Core.Registries;
using UnityEngine;

namespace Spellbound.Core.Observations {
    internal static class ObservationReasons {
        private static readonly HashRegistry<ReasonName> Names = new HashRegistry<ReasonName>();

        internal static ObservationReason Register(string name) {
            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("A reason needs a name.", nameof(name));
            }

            var trimmed = name.Trim();
            var id = StableHash.Fnv1A32(trimmed);

            if (id == 0) {
                throw new ArgumentException("The reason '" + trimmed + "' hashes to zero, which means no reason.",
                    nameof(name));
            }

            if (Names.TryGet(id, out var existing)) {
                if (!string.Equals(existing.Name, trimmed, StringComparison.Ordinal)) {
                    Log.Error("The reason '" + trimmed + "' collides with '" + existing.Name + "' on hash " + id
                              + "; the first is kept.");
                }

                return new ObservationReason(id, existing.Name);
            }

            Names.Add(new ReasonName(id, trimmed));
            return new ObservationReason(id, trimmed);
        }

        internal static void Ensure(ObservationReason reason) {
            if (reason.IsNone || reason.Name == null || Names.Contains(reason.Id)) {
                return;
            }

            Names.Add(new ReasonName(reason.Id, reason.Name));
        }

        internal static string Name(uint id) {
            return Names.TryGet(id, out var entry) ? entry.Name : null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlaySession() {
            Names.Clear();
        }
    }
}
