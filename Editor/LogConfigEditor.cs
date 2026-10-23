// Copyright 2026 Spellbound Studio Inc.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Spellbound.Core.Logging.Editor {
    [CustomEditor(typeof(LogConfig))]
    public class LogConfigEditor : UnityEditor.Editor {
        private const string DefinePrefix = "SPELLBOUND_LOG_";
        private const string DefineVerbose = "SPELLBOUND_LOG_VERBOSE";
        private const string DefineDebug = "SPELLBOUND_LOG_DEBUG";
        private const string DefineInfo = "SPELLBOUND_LOG_INFO";
        private const string DefineWarning = "SPELLBOUND_LOG_WARNING";

        private const string GlobalLabel = "Log Level";
        private const string FileLabel = "File Output";
        private const string FileNameLabel = "Log File Name";
        private const string UndoFileNameLabel = "Change Log File Name";
        private const string CaptureLabel = "Unity Log";
        private const string CaptureToggleLabel = "Capture Unity Log";
        private const string UndoCaptureLabel = "Toggle Unity Log Capture";
        private const string CaptureSubtitle =
                "Routes Debug.Log, warnings, errors and exceptions through the sinks below.";
        private const string HeaderLabel = "Spellbound Log Configuration";
        private const string SinksLabel = "Sinks";
        private const string UndoLevelLabel = "Change Global Log Level";
        private const string UndoSinkLabel = "Toggle Log Sink";
        private const string ApplyLabel = "Apply";
        private const string LevelSubtitle = "Minimum severity compiled into all Spellbound packages.";
        private const string FileSubtitle = "Name of the log file written to the persistent data path.";
        private const string SinksSubtitle = "Toggle where log output is routed.";
        private const string UndoSinkFilterLabel = "Change Sink Filter Level";
        private const string SinkFilterLabel = "  Filter Level";

        private const string LevelDescVerbose = "Compiles all log calls: Verbose, Debug, Info, Warning, and Error.";
        private const string LevelDescDebug = "Compiles Debug, Info, Warning, and Error. Strips Verbose.";
        private const string LevelDescInfo = "Compiles Info, Warning, and Error. Strips Debug and below.";
        private const string LevelDescWarning = "Compiles Warning and Error only. Strips Info and below.";
        private const string LevelDescError = "Error only. All other log calls are stripped from the build.";
        private const string LevelDescNone = "Strips all log calls except Error. Equivalent to Error.";

        private static GUIStyle _headerStyle;
        private static GUIStyle _subtitleStyle;
        private static GUIStyle _sectionStyle;

        private static void InitStyles() {
            if (_headerStyle != null)
                return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel) {
                fontSize = 14,
                padding = new RectOffset(0, 0, 4, 4)
            };

            _subtitleStyle = new GUIStyle(EditorStyles.miniLabel) {
                wordWrap = true,
                padding = new RectOffset(2, 0, 0, 4)
            };

            _sectionStyle = new GUIStyle("box") {
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(0, 0, 4, 4)
            };
        }

        public override void OnInspectorGUI() {
            InitStyles();
            var config = (LogConfig)target;

            EditorGUILayout.LabelField(HeaderLabel, _headerStyle);
            EditorGUILayout.Space(4);

            DrawLevelSection(config);
            EditorGUILayout.Space(4);
            DrawFileSection(config);
            EditorGUILayout.Space(4);
            DrawCaptureSection(config);
            EditorGUILayout.Space(4);
            DrawSinksSection(config);
            EditorGUILayout.Space(8);

            if (!GUILayout.Button(ApplyLabel, GUILayout.Height(28)))
                return;

            EditorUtility.SetDirty(config);
            ApplyDefines(config);
        }

        private static void DrawLevelSection(LogConfig config) {
            EditorGUILayout.BeginVertical(_sectionStyle);

            EditorGUILayout.LabelField(GlobalLabel, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(LevelSubtitle, _subtitleStyle);

            var newLevel = (LogLevel)EditorGUILayout.EnumPopup(config.globalLevel);

            if (newLevel != config.globalLevel) {
                Undo.RecordObject(config, UndoLevelLabel);
                config.globalLevel = newLevel;
            }

            EditorGUILayout.LabelField(GetLevelDescription(config.globalLevel), _subtitleStyle);

            EditorGUILayout.EndVertical();
        }

        private static string GetLevelDescription(LogLevel level) =>
                level switch {
                    LogLevel.Verbose => LevelDescVerbose,
                    LogLevel.Debug => LevelDescDebug,
                    LogLevel.Info => LevelDescInfo,
                    LogLevel.Warning => LevelDescWarning,
                    LogLevel.Error => LevelDescError,
                    LogLevel.None => LevelDescNone,
                    _ => ""
                };

        private static void DrawFileSection(LogConfig config) {
            EditorGUILayout.BeginVertical(_sectionStyle);

            EditorGUILayout.LabelField(FileLabel, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(FileSubtitle, _subtitleStyle);

            var newFileName = EditorGUILayout.TextField(FileNameLabel, config.logFileName);

            if (newFileName != config.logFileName) {
                Undo.RecordObject(config, UndoFileNameLabel);
                config.logFileName = newFileName;
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawCaptureSection(LogConfig config) {
            EditorGUILayout.BeginVertical(_sectionStyle);

            EditorGUILayout.LabelField(CaptureLabel, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(CaptureSubtitle, _subtitleStyle);

            var newCapture = EditorGUILayout.Toggle(CaptureToggleLabel, config.captureUnityLog);

            if (newCapture != config.captureUnityLog) {
                Undo.RecordObject(config, UndoCaptureLabel);
                config.captureUnityLog = newCapture;
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawSinksSection(LogConfig config) {
            EditorGUILayout.BeginVertical(_sectionStyle);

            EditorGUILayout.LabelField(SinksLabel, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(SinksSubtitle, _subtitleStyle);

            SyncSinkEntries(config, DiscoverSinks());

            for (var i = 0; i < config.sinks.Length; i++) {
                var entry = config.sinks[i];

                var newEnabled = EditorGUILayout.Toggle(entry.displayName, entry.enabled);

                if (newEnabled != entry.enabled) {
                    Undo.RecordObject(config, UndoSinkLabel);
                    config.sinks[i].enabled = newEnabled;
                }

                var newFilter = DrawFilteredLevelPopup(SinkFilterLabel, entry.filterLevel, config.globalLevel);

                if (newFilter != entry.filterLevel) {
                    Undo.RecordObject(config, UndoSinkFilterLabel);
                    config.sinks[i].filterLevel = newFilter;
                }

                if (i < config.sinks.Length - 1)
                    EditorGUILayout.Space(4);
            }

            EditorGUILayout.EndVertical();
        }

        private static List<DiscoveredSink> DiscoverSinks() {
            var sinks = new List<DiscoveredSink>();

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) {
                foreach (var type in asm.GetTypes()) {
                    if (!typeof(ILogSink).IsAssignableFrom(type))
                        continue;

                    if (type.IsAbstract || type.IsInterface)
                        continue;

                    if (!type.IsPublic)
                        continue;

                    if (type.GetConstructor(Type.EmptyTypes) == null)
                        continue;

                    var instance = (ILogSink)Activator.CreateInstance(type);

                    sinks.Add(new DiscoveredSink {
                        QualifiedTypeName = type.AssemblyQualifiedName,
                        DisplayName = instance.DisplayName
                    });
                }
            }

            sinks.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal));

            return sinks;
        }

        private static void SyncSinkEntries(LogConfig config, List<DiscoveredSink> discovered) {
            var existing = new HashSet<string>(config.sinks.Select(s => s.qualifiedTypeName));
            var updated = new List<SinkEntry>(config.sinks);

            foreach (var sink in discovered) {
                if (existing.Contains(sink.QualifiedTypeName))
                    continue;

                updated.Add(new SinkEntry {
                    qualifiedTypeName = sink.QualifiedTypeName,
                    displayName = sink.DisplayName,
                    enabled = false
                });
            }

            config.sinks = updated.ToArray();
        }

        private static void ApplyDefines(LogConfig config) {
            var defines = new HashSet<string>();
            AddDefinesForLevel(defines, config.globalLevel);

            var target = NamedBuildTarget.FromBuildTargetGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup
            );

            PlayerSettings.GetScriptingDefineSymbols(target, out var current);
            var preserved = current.Where(d => !d.StartsWith(DefinePrefix)).ToList();
            preserved.AddRange(defines);

            PlayerSettings.SetScriptingDefineSymbols(target, preserved.ToArray());
            Debug.Log($"[LogConfig] Applied log level: {config.globalLevel}");
        }

        // Each case adds defines for its level and everything above it.
        // Error has no [Conditional] attribute so it always compiles — no define needed.
        private static void AddDefinesForLevel(HashSet<string> defines, LogLevel level) {
            switch (level) {
                case LogLevel.Verbose:
                    defines.Add(DefineVerbose);
                    defines.Add(DefineDebug);
                    defines.Add(DefineInfo);
                    defines.Add(DefineWarning);

                    break;
                case LogLevel.Debug:
                    defines.Add(DefineDebug);
                    defines.Add(DefineInfo);
                    defines.Add(DefineWarning);

                    break;
                case LogLevel.Info:
                    defines.Add(DefineInfo);
                    defines.Add(DefineWarning);

                    break;
                case LogLevel.Warning:
                    defines.Add(DefineWarning);

                    break;
                case LogLevel.Error:
                case LogLevel.None:
                    break;
                default:
                    Debug.LogError($"[LogConfigEditor] Unhandled log level: {level}");

                    break;
            }
        }

        private static LogLevel DrawFilteredLevelPopup(string label, LogLevel current, LogLevel floor) {
            // Build arrays of only valid options (at or above the global level)
            var validLevels = new List<LogLevel>();
            var validNames = new List<string>();

            foreach (LogLevel level in Enum.GetValues(typeof(LogLevel))) {
                if (level < floor)
                    continue;

                if (level == LogLevel.None)
                    continue;

                validLevels.Add(level);
                validNames.Add(level.ToString());
            }

            // Find current selection index, clamp to floor if invalid
            var currentIndex = validLevels.IndexOf(current);

            if (currentIndex < 0)
                currentIndex = 0;

            var newIndex = EditorGUILayout.Popup(label, currentIndex, validNames.ToArray());

            return validLevels[newIndex];
        }

        private struct DiscoveredSink {
            public string QualifiedTypeName;
            public string DisplayName;
        }
    }
}
