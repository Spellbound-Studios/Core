// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;

namespace Spellbound.Core.Authoring.Editor {
    public static class DefinitionReportCache {
        private static readonly Dictionary<string, ValidationRun> Runs =
            new Dictionary<string, ValidationRun>(StringComparer.Ordinal);

        public static event Action<string> Changed;

        public static bool TryGet(string assetPath, out ValidationRun run) {
            if (string.IsNullOrEmpty(assetPath)) {
                run = null;
                return false;
            }

            return Runs.TryGetValue(assetPath, out run);
        }

        public static void Store(string assetPath, ValidationRun run) {
            if (string.IsNullOrEmpty(assetPath) || run == null) {
                return;
            }

            Runs[assetPath] = run;
            Changed?.Invoke(assetPath);
        }

        public static void Forget(string assetPath) {
            if (!string.IsNullOrEmpty(assetPath) && Runs.Remove(assetPath)) {
                Changed?.Invoke(assetPath);
            }
        }

        public static void Clear() {
            Runs.Clear();
            Changed?.Invoke(null);
        }
    }
}
