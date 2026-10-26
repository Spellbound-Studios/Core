// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spellbound.Core.Authoring.Editor {
    [InitializeOnLoad]
    public static class DefinitionInspectorBanner {
        public const string InspectorWindowTypeName = "UnityEditor.InspectorWindow";
        public const string ElementName = "spellbound-definition-banner";
        public const string ClassName = "spellbound-definition-banner";
        public const string HeaderClassName = ClassName + "__header";
        public const string TitleClassName = ClassName + "__title";
        public const string ButtonClassName = ClassName + "__run";
        public const string FindingClassName = ClassName + "__finding";
        public const string ErrorClassName = FindingClassName + "--error";
        public const string WarningClassName = FindingClassName + "--warning";
        public const string InformationClassName = FindingClassName + "--information";
        public const string RunChecksText = "Run checks";

        private static readonly HashSet<string> Pending = new HashSet<string>(StringComparer.Ordinal);

        static DefinitionInspectorBanner() {
            Selection.selectionChanged += Refresh;
            DefinitionReportCache.Changed += _ => Refresh();
            EditorApplication.delayCall += Refresh;
        }

        public static void Refresh() {
            var asset = Selection.activeObject as ScriptableObject;
            var path = asset != null ? AssetDatabase.GetAssetPath(asset) : null;
            var isDefinition = DefinitionAssets.IsDefinition(asset) && !string.IsNullOrEmpty(path);

            foreach (var window in InspectorWindows()) {
                var root = window.rootVisualElement;

                if (root == null) {
                    continue;
                }

                var banner = root.Q<VisualElement>(ElementName);

                if (!isDefinition) {
                    banner?.RemoveFromHierarchy();
                    continue;
                }

                if (banner == null) {
                    banner = new VisualElement { name = ElementName };
                    banner.AddToClassList(ClassName);
                    Host(root).Insert(0, banner);
                }

                Fill(banner, asset, path);
            }
        }

        public static VisualElement Build(ScriptableObject asset, string path) {
            var banner = new VisualElement { name = ElementName };
            banner.AddToClassList(ClassName);
            Fill(banner, asset, path);
            return banner;
        }

        private static void Fill(VisualElement banner, ScriptableObject asset, string path) {
            banner.Clear();
            var header = new VisualElement();
            header.AddToClassList(HeaderClassName);
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;
            var title = new Label();
            title.AddToClassList(TitleClassName);
            header.Add(title);
            var button = new Button(() => Run(asset)) { text = RunChecksText };
            button.AddToClassList(ButtonClassName);
            header.Add(button);
            banner.Add(header);

            if (!DefinitionReportCache.TryGet(path, out var run)) {
                title.text = "Validation: not run yet";
                ScheduleFirstRun(asset, path);
                return;
            }

            title.text = "Validation at " + run.Finished.ToString("HH:mm:ss") + ": " + run.Report.Summary();

            foreach (var finding in run.Report.Ordered()) {
                var line = new Label(FindingText(finding));
                line.AddToClassList(FindingClassName);
                line.AddToClassList(ClassFor(finding.Severity));
                line.style.whiteSpace = WhiteSpace.Normal;
                line.tooltip = finding.ToText();
                banner.Add(line);
            }

            var checks = new Label(run.ChecksLine());
            checks.style.whiteSpace = WhiteSpace.Normal;
            banner.Add(checks);
        }

        private static string FindingText(ValidationFinding finding) {
            var text = ValidationFinding.SeverityWord(finding.Severity) + " " + finding.Code;

            if (finding.Location != null) {
                text += " at " + finding.Location;
            }

            text += ": " + finding.Message;

            if (finding.Hint != null) {
                text += " (" + finding.Hint + ")";
            }

            return text;
        }

        private static string ClassFor(FindingSeverity severity) {
            switch (severity) {
                case FindingSeverity.Error:
                    return ErrorClassName;
                case FindingSeverity.Warning:
                    return WarningClassName;
                default:
                    return InformationClassName;
            }
        }

        private static void ScheduleFirstRun(ScriptableObject asset, string path) {
            if (!Pending.Add(path)) {
                return;
            }

            EditorApplication.delayCall += () => {
                Pending.Remove(path);

                if (asset != null) {
                    Run(asset);
                }
            };
        }

        private static void Run(ScriptableObject asset) {
            if (asset == null) {
                return;
            }

            new DefinitionValidationRunner().ValidateAsset(asset);
        }

        private static VisualElement Host(VisualElement root) {
            return root.Q<VisualElement>(className: "unity-inspector-main-container") ?? root;
        }

        public static IReadOnlyList<EditorWindow> InspectorWindows() {
            var windows = new List<EditorWindow>();

            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>()) {
                if (window != null
                    && string.Equals(window.GetType().FullName, InspectorWindowTypeName, StringComparison.Ordinal)) {
                    windows.Add(window);
                }
            }

            return windows;
        }
    }
}
