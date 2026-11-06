// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using Spellbound.Core.Tests.AuthoringFixture;
using UnityEditor;
using UnityEngine;

namespace Spellbound.Core.Tests {
    public sealed class AuthoringTestFolder : IDisposable {
        public const string Root = "Assets/SpellboundCoreTestsAuthoring";

        public string Path { get; }

        public AuthoringTestFolder() {
            if (!AssetDatabase.IsValidFolder(Root)) {
                AssetDatabase.CreateFolder("Assets", Root.Substring("Assets/".Length));
            }

            var name = "Run" + Guid.NewGuid().ToString("N").Substring(0, 8);
            AssetDatabase.CreateFolder(Root, name);
            Path = Root + "/" + name;
        }

        public string Subfolder(string name) {
            AssetDatabase.CreateFolder(Path, name);
            return Path + "/" + name;
        }

        public FixtureDefinition Create(string name, string title, int threshold, string folder = null) {
            var asset = ScriptableObject.CreateInstance<FixtureDefinition>();
            asset.title = title;
            asset.threshold = threshold;
            AssetDatabase.CreateAsset(asset, (folder ?? Path) + "/" + name + ".asset");
            AssetDatabase.SaveAssets();
            return asset;
        }

        public void Dispose() {
            AssetDatabase.DeleteAsset(Path);

            if (AssetDatabase.IsValidFolder(Root) && AssetDatabase.FindAssets(string.Empty, new[] { Root }).Length == 0) {
                AssetDatabase.DeleteAsset(Root);
            }
        }
    }
}
