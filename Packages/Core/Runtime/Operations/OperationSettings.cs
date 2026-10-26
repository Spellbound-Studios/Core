// Copyright (c) Spellbound Studios. All rights reserved.

using UnityEngine;

namespace Spellbound.Core.Operations {
    [CreateAssetMenu(menuName = "Spellbound/Operation Settings", fileName = ResourceName)]
    public class OperationSettings : ScriptableObject {
        public const string ResourceName = "OperationSettings";
        public const string DefaultDraftsFolder = "Assets/Drafts";

        public bool trace;
        public string[] disabledGroups = System.Array.Empty<string>();
        public string[] bridgeAudiences = { OperationAudiences.Developer };
        public int maximumLineLength = 4096;
        public int maximumResultLines = 200;
        public int maximumHistoryEntries = 1000;
        public string draftsFolder = DefaultDraftsFolder;

        public static OperationSettings Load() {
            var loaded = Resources.Load<OperationSettings>(ResourceName);

            if (loaded != null) {
                return loaded;
            }

            var defaults = CreateInstance<OperationSettings>();
            defaults.name = ResourceName;
            defaults.hideFlags = HideFlags.HideAndDontSave;
            return defaults;
        }

        public bool IsGroupDisabled(string group) {
            if (disabledGroups == null) {
                return false;
            }

            foreach (var disabled in disabledGroups) {
                if (string.Equals(disabled, group, System.StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
            }

            return false;
        }
    }
}
