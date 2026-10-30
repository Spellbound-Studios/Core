// Copyright 2026 Spellbound Studio Inc.

using UnityEngine;

namespace Spellbound.Core.Logging {
    [CreateAssetMenu(menuName = "Spellbound/Log Config", fileName = "LogConfig")]
    public class LogConfig : ScriptableObject {
        public LogLevel globalLevel = LogLevel.Error;
        public string logFileName = "spellbound.log";
        public bool captureUnityLog;
        public SinkEntry[] sinks = System.Array.Empty<SinkEntry>();
    }
}