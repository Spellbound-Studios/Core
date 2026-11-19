// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using UnityEditor;

namespace Spellbound.Core.Controls.Editor {
    public sealed class EditorLookSettingsWatcher : AssetPostprocessor {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths) {
            if (!Touches(importedAssets) && !Touches(deletedAssets) && !Touches(movedAssets)) {
                return;
            }

            EditorLook.ForgetSettings();
            EditorLook.RedressAll();
        }

        private static bool Touches(string[] paths) {
            if (paths == null) {
                return false;
            }

            foreach (var path in paths) {
                if (path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)
                    && (AssetDatabase.LoadAssetAtPath<EditorLookSettings>(path) != null
                        || AssetDatabase.GetMainAssetTypeAtPath(path) == null)) {
                    return true;
                }
            }

            return false;
        }
    }
}
