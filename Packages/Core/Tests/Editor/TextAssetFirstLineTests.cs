// Copyright (c) Spellbound Studios. All rights reserved.

using System.IO;
using NUnit.Framework;
using Spellbound.Core.Logging;
using Spellbound.Core.Operations;
using UnityEditor;
using UnityEngine;

namespace Spellbound.Core.Tests {
    public class TextAssetFirstLineTests {
        private const string Folder = "Assets/SpellboundCoreTestsTextAsset";
        private const string AssetPath = Folder + "/ReSaved.asset";

        [SetUp]
        public void CreateFolder() {
            if (!AssetDatabase.IsValidFolder(Folder)) {
                AssetDatabase.CreateFolder("Assets", Folder.Substring("Assets/".Length));
            }
        }

        [TearDown]
        public void DeleteFolder() {
            AssetDatabase.DeleteAsset(Folder);
        }

        [Test]
        public void ReSavedTextAssetStartsWithYamlAndNoUnityVersionLine() {
            var asset = ScriptableObject.CreateInstance<OperationSettings>();
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            var fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", AssetPath));
            var lines = File.ReadAllLines(fullPath);
            Assert.GreaterOrEqual(lines.Length, 3);
            Log.Emit(LogLevel.Info, "TextAssetFirstLineTests: " + Application.unityVersion + " writes '" + lines[0]
                                    + "' then '" + lines[1] + "' then '" + lines[2]
                                    + "' as the first lines of a text asset");
            Assert.IsTrue(lines[0].StartsWith("%YAML"), "first line: " + lines[0]);

            for (var index = 0; index < 3; index++) {
                Assert.IsFalse(lines[index].StartsWith("%UNITY"), "line " + (index + 1) + ": " + lines[index]);
            }
        }
    }
}
