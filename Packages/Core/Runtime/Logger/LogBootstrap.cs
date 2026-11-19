// Copyright 2026 Spellbound Studio Inc.

using System;
using UnityEngine;

namespace Spellbound.Core.Logging {
    public static class LogBootstrap {
        private const string ConfigResourcePath = "LogConfig";
        private const string BootstrapSource = "LogBootstrap";

        private const string NoConfigWarning =
                "No LogConfig found at Resources/LogConfig. Using default config " +
                "(Error level, Unity Console sink). Create one via Create > Spellbound > Log Config.";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize() {
            UnityLogCapture.Stop();
            Log.ClearSinks();

            var config = Resources.Load<LogConfig>(ConfigResourcePath);

            if (config == null) {
                config = CreateDefaultConfig();
                Debug.LogWarning($"[{BootstrapSource}] {NoConfigWarning}");
            }

            RegisterEnabledSinks(config);

            if (config.captureUnityLog)
                UnityLogCapture.Start(config);
        }

#if UNITY_EDITOR
        public static ILogSink EditModeSink { get; private set; }

        [UnityEditor.InitializeOnLoadMethod]
        private static void InitializeInEditor() {
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                RegisterEditModeSink();
        }

        private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state) {
            if (state != UnityEditor.PlayModeStateChange.EnteredEditMode)
                return;

            UnityLogCapture.Stop();
            Log.ClearSinks();
            RegisterEditModeSink();
        }

        public static void RegisterEditModeSink() {
            if (EditModeSink != null)
                Log.RemoveSink(EditModeSink);

            var config = Resources.Load<LogConfig>(ConfigResourcePath);
            EditModeSink = new UnityConsoleSink();
            Log.AddSink(EditModeSink, config, EditModeFilterLevel(config));
        }

        private static LogLevel EditModeFilterLevel(LogConfig config) {
            if (config == null || config.sinks == null)
                return LogLevel.Verbose;

            var consoleSinkTypeName = typeof(UnityConsoleSink).AssemblyQualifiedName;

            foreach (var entry in config.sinks) {
                if (string.Equals(entry.qualifiedTypeName, consoleSinkTypeName, StringComparison.Ordinal))
                    return entry.filterLevel;
            }

            return LogLevel.Verbose;
        }
#endif

        private static LogConfig CreateDefaultConfig() {
            var config = ScriptableObject.CreateInstance<LogConfig>();
            config.globalLevel = LogLevel.Error;
            config.logFileName = "spellbound.log";

            config.sinks = new[] {
                new SinkEntry {
                    qualifiedTypeName = typeof(UnityConsoleSink).AssemblyQualifiedName,
                    displayName = new UnityConsoleSink().DisplayName,
                    enabled = true
                }
            };

            return config;
        }

        private static void RegisterEnabledSinks(LogConfig config) {
            foreach (var entry in config.sinks) {
                if (!entry.enabled)
                    continue;

                var type = Type.GetType(entry.qualifiedTypeName);

                if (type == null) {
                    Debug.LogError(
                        $"[{BootstrapSource}] Could not resolve sink type '{entry.qualifiedTypeName}'. " +
                        "Was the assembly removed or renamed?"
                    );

                    continue;
                }

                try {
                    var sink = (ILogSink)Activator.CreateInstance(type);
                    Log.AddSink(sink, config, entry.filterLevel);
                }
                catch (Exception ex) {
                    Debug.LogError(
                        $"[{BootstrapSource}] Failed to instantiate sink '{entry.displayName}': {ex.Message}"
                    );
                }
            }
        }
    }
}
