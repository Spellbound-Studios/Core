// Copyright (c) Spellbound Studios. All rights reserved.

using UnityEngine;

namespace Spellbound.Core.Authoring.Editor {
    public class Draft {
        public ScriptableObject Asset { get; }
        public string Path { get; }
        public ValidationRun Run { get; }
        public bool IsRevision { get; }
        public string OriginalPath { get; }

        public Draft(ScriptableObject asset, string path, ValidationRun run, bool isRevision = false,
            string originalPath = null) {
            Asset = asset;
            Path = path;
            Run = run;
            IsRevision = isRevision;
            OriginalPath = originalPath;
        }
    }

    public sealed class Draft<T> : Draft where T : ScriptableObject {
        public T Definition => (T)Asset;

        public Draft(T asset, string path, ValidationRun run) : base(asset, path, run) { }
    }
}
