// Copyright 2026 Spellbound Studio Inc.

namespace Spellbound.Core.Logging {
    [System.Serializable]
    public struct SinkEntry {
        public string qualifiedTypeName;
        public string displayName;
        public bool enabled;
        public LogLevel filterLevel;
    }
}