// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Spellbound.Core.Observations {
    public static class ObservationContexts {
        private static readonly List<ObservationContext> Live = new List<ObservationContext>();
        private static readonly ReadOnlyCollection<ObservationContext> LiveView = Live.AsReadOnly();

        public static IReadOnlyList<ObservationContext> All => LiveView;

        internal static ObservationContext Find(string name) {
            foreach (var context in Live) {
                if (string.Equals(context.Name, name, StringComparison.Ordinal)) {
                    return context;
                }
            }

            return null;
        }

        internal static void Add(ObservationContext context) {
            Live.Add(context);
        }

        internal static void Remove(ObservationContext context) {
            Live.Remove(context);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlaySession() {
            Live.Clear();
        }
    }
}
