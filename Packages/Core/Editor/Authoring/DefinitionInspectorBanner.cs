// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using Spellbound.Core.Controls.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spellbound.Core.Authoring.Editor {
    [InitializeOnLoad]
    public static class DefinitionInspectorBanner {
        public const string InspectorWindowTypeName = "UnityEditor.InspectorWindow";
        public const string ElementName = "spellbound-definition-banner";
        public const string ClassName = "sb-definition-banner";
        public const string RunChecksText = "Run checks";
        public const string NotRunText = "Validation: not run yet";

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
            var expanded = banner.Q<ReportView>() is { Compact: false };
            banner.Clear();
            var header = new HeaderBar(NotRunText);
            header.Add(new Button(() => Run(asset)) { text = RunChecksText });
            banner.Add(header);

            if (!DefinitionReportCache.TryGet(path, out var run)) {
                ScheduleFirstRun(asset, path);
                return;
            }

            header.Title = "Validation at " + run.Finished.ToString("HH:mm:ss");
            var view = new ReportView { Compact = !expanded };
            view.Bind(run);
            banner.Add(view);
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
