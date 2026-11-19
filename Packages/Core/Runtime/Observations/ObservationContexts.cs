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

        public static bool TryResolve(string name, out ObservationContext context, out string failure) {
            failure = null;

            if (string.IsNullOrWhiteSpace(name)) {
                context = Live.Count == 1 ? Live[0] : ObservationContext.Discard;
                return true;
            }

            context = Find(name.Trim());

            if (context != null) {
                return true;
            }

            context = ObservationContext.Discard;
            failure = "no world named '" + name + "' is live" + LiveWorldList();
            return false;
        }

        internal static string LiveWorldList() {
            if (Live.Count == 0) {
                return string.Empty;
            }

            var names = new string[Live.Count];

            for (var i = 0; i < Live.Count; i++) {
                names[i] = Live[i].Name;
            }

            return "\nLive worlds: " + string.Join(", ", names);
        }

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
