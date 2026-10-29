// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections.Generic;
using Spellbound.Core.Controls.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Spellbound.Core.Logging.Editor {
    [CustomEditor(typeof(LogConfig))]
    public sealed class LogSettingsInspector : UnityEditor.Editor {
        public const string ClassName = "sb-log-settings";
        public const string SubtitleClassName = ClassName + "__subtitle";
        public const string LevelDescriptionClassName = ClassName + "__level-description";
        public const string SinksClassName = ClassName + "__sinks";
        public const string SinkClassName = ClassName + "__sink";
        public const string SinkFilterClassName = ClassName + "__sink-filter";
        public const string ApplyClassName = ClassName + "__apply";
        public const string AppliedClassName = ClassName + "__applied";
        public const string AppliedSessionKey = "Spellbound.Core.LogSettingsInspector.Applied";
        public const string LevelCardName = "spellbound-log-level";
        public const string FileCardName = "spellbound-log-file";
        public const string CaptureCardName = "spellbound-log-capture";
        public const string SinksCardName = "spellbound-log-sinks";

        private const string HeaderLabel = "Spellbound Log Configuration";
        private const string GlobalLabel = "Log Level";
        private const string LevelSubtitle = "Minimum severity compiled into all Spellbound packages.";
        private const string FileLabel = "File Output";
        private const string FileSubtitle = "Name of the log file written to the persistent data path.";
        private const string FileNameLabel = "Log File Name";
        private const string CaptureLabel = "Unity Log";
        private const string CaptureSubtitle =
            "Routes Debug.Log, warnings, errors and exceptions through the sinks below.";
        private const string CaptureToggleLabel = "Capture Unity Log";
        private const string SinksLabel = "Sinks";
        private const string SinksSubtitle = "Toggle where log output is routed.";
        private const string SinkFilterLabel = "Filter Level";
        private const string ApplyLabel = "Apply";
        private const string VerboseDescription = "Compiles all log calls: Verbose, Debug, Info, Warning, and Error.";
        private const string DebugDescription = "Compiles Debug, Info, Warning, and Error. Strips Verbose.";
        private const string InfoDescription = "Compiles Info, Warning, and Error. Strips Debug and below.";
        private const string WarningDescription = "Compiles Warning and Error only. Strips Info and below.";
        private const string ErrorDescription = "Error only. All other log calls are stripped from the build.";
        private const string NoneDescription = "Strips all log calls except Error. Equivalent to Error.";
        private const float CardSpacing = 4f;
        private const float FilterIndent = 15f;
        private const float ApplySpacing = 8f;
        private const float ApplyHeight = 28f;

        private Label _applied;

        public override VisualElement CreateInspectorGUI() {
            if (LogSinkDiscovery.AppendMissing((LogConfig)target)) {
                serializedObject.Update();
            }

            var level = serializedObject.FindProperty(nameof(LogConfig.globalLevel));
            var sinks = serializedObject.FindProperty(nameof(LogConfig.sinks));
            var shownLevel = LevelOf(level);
            var description = Line(DescriptionOf(shownLevel), LevelDescriptionClassName);
            var rows = new VisualElement();
            rows.AddToClassList(SinksClassName);
            BuildSinkRows(rows, sinks, shownLevel);
            var levelField = new EnumField(shownLevel) { bindingPath = level.propertyPath };
            levelField.RegisterValueChangedCallback(change => {
                if (change.newValue is LogLevel chosen) {
                    ShowLevel(description, rows, chosen);
                }
            });

            var root = new VisualElement();
            root.AddToClassList(ClassName);
            root.Add(new HeaderBar(HeaderLabel));
            root.Add(Card(LevelCardName, GlobalLabel, Line(LevelSubtitle, SubtitleClassName), levelField,
                description));
            root.Add(Card(FileCardName, FileLabel, Line(FileSubtitle, SubtitleClassName), FileNameField()));
            root.Add(Card(CaptureCardName, CaptureLabel, Line(CaptureSubtitle, SubtitleClassName), CaptureToggle()));
            root.Add(Card(SinksCardName, SinksLabel, Line(SinksSubtitle, SubtitleClassName), rows));
            root.Add(ApplyButton());
            _applied = Line(SessionState.GetString(AppliedSessionKey, string.Empty), AppliedClassName);
            root.Add(_applied);
            root.TrackSerializedObjectValue(serializedObject, _ => {
                if (rows.childCount != sinks.arraySize) {
                    BuildSinkRows(rows, sinks, LevelOf(level));
                }

                ShowLevel(description, rows, LevelOf(level));
            });

            return root;
        }

        private TextField FileNameField() {
            var fileName = serializedObject.FindProperty(nameof(LogConfig.logFileName));
            var field = new TextField(FileNameLabel) { bindingPath = fileName.propertyPath };
            field.SetValueWithoutNotify(fileName.stringValue);
            return field;
        }

        private Toggle CaptureToggle() {
            var capture = serializedObject.FindProperty(nameof(LogConfig.captureUnityLog));
            var toggle = new Toggle(CaptureToggleLabel) { bindingPath = capture.propertyPath };
            toggle.SetValueWithoutNotify(capture.boolValue);
            return toggle;
        }

        private Button ApplyButton() {
            var button = new Button(Apply) { text = ApplyLabel };
            button.AddToClassList(ApplyClassName);
            button.style.marginTop = ApplySpacing;
            button.style.height = ApplyHeight;
            return button;
        }

        private void Apply() {
            var config = (LogConfig)target;

            if (config == null) {
                return;
            }

            EditorUtility.SetDirty(config);
            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            PlayerSettings.GetScriptingDefineSymbols(namedTarget, out var current);
            PlayerSettings.SetScriptingDefineSymbols(namedTarget, LogLevelDefines.Merge(current, config.globalLevel));
            var text = AppliedText(config.globalLevel, namedTarget.TargetName);
            SessionState.SetString(AppliedSessionKey, text);

            if (_applied != null) {
                _applied.text = text;
            }

            Log.Emit(LogLevel.Info, text);
        }

        private static string AppliedText(LogLevel level, string target) {
            var defines = LogLevelDefines.For(level);
            var written = defines.Count == 0 ? "no " + LogLevelDefines.Prefix + " define" : string.Join(", ", defines);
            return "Applied log level " + level + " for " + target + ": " + written + ".";
        }

        private static void BuildSinkRows(VisualElement rows, SerializedProperty sinks, LogLevel globalLevel) {
            rows.Clear();

            for (var index = 0; index < sinks.arraySize; index++) {
                rows.Add(SinkRow(sinks.GetArrayElementAtIndex(index), globalLevel));
            }
        }

        private static VisualElement SinkRow(SerializedProperty entry, LogLevel globalLevel) {
            var row = new VisualElement();
            row.AddToClassList(SinkClassName);
            var enabled = entry.FindPropertyRelative(nameof(SinkEntry.enabled));
            var toggle = new Toggle(entry.FindPropertyRelative(nameof(SinkEntry.displayName)).stringValue);
            toggle.SetValueWithoutNotify(enabled.boolValue);
            toggle.BindProperty(enabled);
            row.Add(toggle);
            var filterLevel = entry.FindPropertyRelative(nameof(SinkEntry.filterLevel));
            var filter = new PopupField<LogLevel>(SinkFilterLabel) { userData = filterLevel };
            filter.AddToClassList(SinkFilterClassName);
            filter.style.marginLeft = FilterIndent;
            ShowFilter(filter, globalLevel);
            filter.RegisterValueChangedCallback(change => {
                filterLevel.intValue = (int)change.newValue;
                filterLevel.serializedObject.ApplyModifiedProperties();
            });

            row.Add(filter);
            return row;
        }

        private static void ShowLevel(Label description, VisualElement rows, LogLevel globalLevel) {
            description.text = DescriptionOf(globalLevel);
            rows.Query<PopupField<LogLevel>>(className: SinkFilterClassName)
                .ForEach(filter => ShowFilter(filter, globalLevel));
        }

        private static void ShowFilter(PopupField<LogLevel> filter, LogLevel globalLevel) {
            var stored = LevelOf((SerializedProperty)filter.userData);
            var choices = new List<LogLevel>(LogLevelDefines.CompiledLevels(globalLevel));
            filter.choices = choices;
            filter.SetValueWithoutNotify(choices.Contains(stored) ? stored : choices[0]);
        }

        private static LogLevel LevelOf(SerializedProperty property) {
            return (LogLevel)property.intValue;
        }

        private static string DescriptionOf(LogLevel level) {
            switch (level) {
                case LogLevel.Verbose:
                    return VerboseDescription;
                case LogLevel.Debug:
                    return DebugDescription;
                case LogLevel.Info:
                    return InfoDescription;
                case LogLevel.Warning:
                    return WarningDescription;
                case LogLevel.Error:
                    return ErrorDescription;
                case LogLevel.None:
                    return NoneDescription;
                default:
                    return string.Empty;
            }
        }

        private static Label Line(string text, string className) {
            var label = new Label(text);
            label.AddToClassList(className);
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        private static SectionCard Card(string name, string title, params VisualElement[] children) {
            var card = new SectionCard(title) { name = name };
            card.style.marginTop = CardSpacing;

            foreach (var child in children) {
                card.Add(child);
            }

            return card;
        }
    }
}
