// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Spellbound.Core.Controls.Editor;
using Spellbound.Core.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spellbound.Core.Tests {
    public class EditorLookTests {
        private const string Key = "com.example.ui";
        private const string OtherKey = "com.example.other";

        private readonly List<UnityEngine.Object> _made = new List<UnityEngine.Object>();
        private readonly List<string> _added = new List<string>();
        private EditorLookSettings _settings;

        [SetUp]
        public void SetUp() {
            _settings = Make<EditorLookSettings>();
            EditorLook.Settings = () => _settings;
        }

        [TearDown]
        public void TearDown() {
            foreach (var key in _added) {
                EditorLook.Remove(key);
            }

            _added.Clear();
            EditorLook.Settings = null;

            foreach (var made in _made) {
                UnityEngine.Object.DestroyImmediate(made);
            }

            _made.Clear();
        }

        [Test]
        public void DressAddsTheThemeClassTheSkinClassAndTheLayoutSheetOnce() {
            var root = new VisualElement();
            EditorLook.Dress(root);
            EditorLook.Dress(root);
            Assert.IsTrue(root.ClassListContains(EditorLook.ThemeClassName));
            Assert.IsTrue(root.ClassListContains(EditorSkin.Current.ClassName));
            Assert.AreEqual(1, root.styleSheets.count);
            Assert.AreSame(ControlStyleSheet.Default, root.styleSheets[0]);
            Assert.IsTrue(EditorLook.IsDressed(root));
        }

        [Test]
        public void TheSheetsComeInOrderLayoutThenSourcesByOrderAndKeyThenTheProject() {
            var ui = Sheet();
            var late = Sheet();
            var early = Sheet();
            var project = Sheet();
            Register(new LookSource(Key, ui));
            Register(new LookSource("com.example.late", late, 10));
            Register(new LookSource("com.example.early", early, -10));
            _settings.styleSheets = new[] { project };
            var sheets = EditorLook.SheetsFor(EditorSkin.Dark);
            CollectionAssert.AreEqual(new[] { ControlStyleSheet.Default, early, ui, late, project }, sheets);
            var root = new VisualElement();
            EditorLook.Dress(root);
            CollectionAssert.AreEqual(sheets, Sheets(root));
        }

        [Test]
        public void ASourceIsReplacedAndRemovedByKey() {
            var first = Sheet();
            var second = Sheet();
            Register(new LookSource(Key, first));
            Register(new LookSource(Key, second));
            Assert.AreEqual(1, EditorLook.Sources.Count(source => source.Key == Key));
            CollectionAssert.Contains(EditorLook.SheetsFor(EditorSkin.Dark), second);
            CollectionAssert.DoesNotContain(EditorLook.SheetsFor(EditorSkin.Dark), first);
            Assert.IsTrue(EditorLook.Remove(Key));
            Assert.IsFalse(EditorLook.Remove(Key));
            CollectionAssert.DoesNotContain(EditorLook.SheetsFor(EditorSkin.Dark), second);
        }

        [Test]
        public void TheProjectSettingLeavesAPackageOut() {
            var ui = Sheet();
            var other = Sheet();
            Register(new LookSource(Key, ui));
            Register(new LookSource(OtherKey, other));
            _settings.excludedKeys = new[] { " " + Key + " " };
            var sheets = EditorLook.SheetsFor(EditorSkin.Dark);
            CollectionAssert.DoesNotContain(sheets, ui);
            CollectionAssert.Contains(sheets, other);
            Assert.IsTrue(_settings.Excludes(Key));
            Assert.IsFalse(_settings.Excludes(OtherKey));
            Assert.IsFalse(_settings.Excludes(null));
        }

        [Test]
        public void ASourceAnswersPerSkin() {
            var dark = Sheet();
            var light = Sheet();
            Register(new LookSource(Key, dark, light));
            CollectionAssert.Contains(EditorLook.SheetsFor(EditorSkin.Dark), dark);
            CollectionAssert.DoesNotContain(EditorLook.SheetsFor(EditorSkin.Dark), light);
            CollectionAssert.Contains(EditorLook.SheetsFor(EditorSkin.Light), light);
            CollectionAssert.DoesNotContain(EditorLook.SheetsFor(EditorSkin.Light), dark);
            var root = new VisualElement();
            EditorLook.Dress(root);
            var current = EditorSkin.Current.Equals(EditorSkin.Dark);
            Assert.IsTrue(root.styleSheets.Contains(current ? dark : light));
            Assert.IsFalse(root.styleSheets.Contains(current ? light : dark));
        }

        [Test]
        public void ADressedRootIsRedressedWithTheSheetsOfTheMomentAndStripped() {
            var first = Sheet();
            var second = Sheet();
            var root = new VisualElement();
            Register(new LookSource(Key, first));
            EditorLook.Dress(root);
            Register(new LookSource(Key, second));
            EditorLook.RedressAll();
            CollectionAssert.Contains(Sheets(root), second);
            CollectionAssert.DoesNotContain(Sheets(root), first);
            Assert.AreEqual(1, root.GetClasses().Count(name => name.StartsWith(EditorLook.ThemeClassName + "--")));
            EditorLook.Strip(root);
            Assert.IsFalse(root.ClassListContains(EditorLook.ThemeClassName));
            Assert.IsFalse(EditorLook.IsDressed(root));
            Assert.AreEqual(0, root.styleSheets.count);
        }

        [Test]
        public void ASecondDressingKeepsItsPlaceBeforeTheSheetsAConsumerAddedLater() {
            var ui = Sheet();
            var mine = Sheet();
            Register(new LookSource(Key, ui));
            var root = new VisualElement();
            EditorLook.Dress(root);
            root.styleSheets.Add(mine);
            EditorLook.Dress(root);
            CollectionAssert.AreEqual(new[] { ControlStyleSheet.Default, ui, mine }, Sheets(root));
            EditorLook.RedressAll();
            CollectionAssert.AreEqual(new[] { ControlStyleSheet.Default, ui, mine }, Sheets(root));
        }

        [Test]
        public void ASheetAndAThemeClassTheRootAlreadyHadAreLeftAlone() {
            var own = Sheet();
            var root = new VisualElement();
            root.AddToClassList(EditorLook.ThemeClassName);
            root.styleSheets.Add(own);
            Register(new LookSource(Key, own));
            EditorLook.Dress(root);
            Assert.AreEqual(2, root.styleSheets.count);
            EditorLook.Strip(root);
            Assert.IsTrue(root.styleSheets.Contains(own));
            Assert.IsTrue(root.ClassListContains(EditorLook.ThemeClassName));
            Assert.IsFalse(root.ClassListContains(EditorSkin.Current.ClassName));
        }

        [Test]
        public void AThrowingSourceIsLoggedAndSkippedAndTheRootIsStillDressed() {
            var other = Sheet();
            Register(new LookSource(Key, _ => throw new InvalidOperationException("no sheet today")));
            Register(new LookSource(OtherKey, other));
            var recording = new RecordingLogSink(LogLevel.Verbose);

            using (Log.SuspendSinks()) {
                Log.AddSink(recording, null, LogLevel.Verbose);

                try {
                    var root = new VisualElement();
                    EditorLook.Dress(root);
                    CollectionAssert.AreEqual(new[] { ControlStyleSheet.Default, other }, Sheets(root));
                    Assert.IsTrue(EditorLook.IsDressed(root));
                    Assert.AreEqual(1, recording.CountOf(LogLevel.Error));
                    EditorLook.Strip(root);
                    Assert.IsFalse(root.ClassListContains(EditorSkin.Current.ClassName));
                }
                finally {
                    Log.RemoveSink(recording);
                }
            }
        }

        [Test]
        public void AControlUnderADressedRootDropsItsOwnLayoutSheetOnAPanel() {
            var root = new VisualElement();
            EditorLook.Dress(root);
            var header = new HeaderBar("Inside");
            root.Add(header);
            var outside = new HeaderBar("Outside");
            Assert.IsTrue(header.styleSheets.Contains(ControlStyleSheet.Default));
            ControlStyleSheet.FollowAncestors(header, ControlStyleSheet.Default);
            ControlStyleSheet.FollowAncestors(outside, ControlStyleSheet.Default);
            Assert.IsFalse(header.styleSheets.Contains(ControlStyleSheet.Default));
            Assert.IsTrue(outside.styleSheets.Contains(ControlStyleSheet.Default));
            Assert.IsTrue(root.styleSheets.Contains(ControlStyleSheet.Default));
            var window = ControlTestWindow.Open(root);

            try {
                Assert.IsNotNull(header.panel, "the test window gave the control no panel");
                var late = new HeaderBar("Late");
                root.Add(late);
                Assert.IsFalse(late.styleSheets.Contains(ControlStyleSheet.Default));
            }
            finally {
                window.Close();
            }
        }

        [Test]
        public void LookSource_CustomImplementation() {
            var sheet = Sheet();
            var asked = new List<EditorSkin>();
            Register(new LookSource("com.example.custom", skin => {
                asked.Add(skin);
                return new[] { sheet };
            }));

            var root = new VisualElement();
            EditorLook.Dress(root);
            CollectionAssert.AreEqual(new[] { EditorSkin.Current }, asked);
            Assert.IsTrue(root.styleSheets.Contains(sheet));
        }

        [Test]
        public void Settings_CustomImplementation() {
            var project = Sheet();
            var custom = Make<EditorLookSettings>();
            custom.styleSheets = new[] { project };
            EditorLook.Settings = () => custom;
            CollectionAssert.Contains(EditorLook.SheetsFor(EditorSkin.Dark), project);
            EditorLook.Settings = null;
            CollectionAssert.DoesNotContain(EditorLook.SheetsFor(EditorSkin.Dark), project);
        }

        [Test]
        public void TheDefaultSettingsAreFoundOnceAndForgottenOnDemand() {
            EditorLook.Settings = null;
            var found = EditorLookSettings.Find();

            if (found != null) {
                Assert.Ignore("this project holds an EditorLookSettings asset at " + UnityEditor.AssetDatabase.GetAssetPath(found));
            }

            Assert.IsNull(EditorLook.Settings());
            EditorLook.ForgetSettings();
            Assert.IsNull(EditorLook.Settings());
            Assert.IsNotNull(EditorLook.SheetsFor(EditorSkin.Dark));
        }

        [Test]
        public void TheSkinNamesItsClass() {
            Assert.AreEqual("sb-theme--dark", EditorSkin.Dark.ClassName);
            Assert.AreEqual("sb-theme--light", EditorSkin.Light.ClassName);
            Assert.AreEqual(EditorSkin.Dark, new EditorSkin("dark"));
            Assert.AreEqual(EditorSkin.Dark, default(EditorSkin));
            Assert.AreNotEqual(EditorSkin.Dark, EditorSkin.Light);
            Assert.Throws<ArgumentException>(() => new EditorSkin(" "));
        }

        [Test]
        public void TheSharedClassNamesAreOneWordEach() {
            Assert.AreEqual("sb-button--primary", ButtonVariants.PrimaryClassName);
            Assert.AreEqual("sb-button--secondary", ButtonVariants.SecondaryClassName);
            Assert.AreEqual("sb-button--destructive", ButtonVariants.DestructiveClassName);
            Assert.AreEqual("sb-chip--passed", ChipTones.PassedClassName);
            Assert.AreEqual("sb-chip--stale", ChipTones.StaleClassName);
            Assert.AreEqual("sb-chip--in-sync", ChipTones.ClassNameFor("In-Sync"));
        }

        private void Register(LookSource source) {
            EditorLook.Register(source);

            if (!_added.Contains(source.Key)) {
                _added.Add(source.Key);
            }
        }

        private StyleSheet Sheet() {
            return Make<StyleSheet>();
        }

        private T Make<T>() where T : ScriptableObject {
            var made = ScriptableObject.CreateInstance<T>();
            _made.Add(made);
            return made;
        }

        private static List<StyleSheet> Sheets(VisualElement root) {
            var sheets = new List<StyleSheet>();

            for (var index = 0; index < root.styleSheets.count; index++) {
                sheets.Add(root.styleSheets[index]);
            }

            return sheets;
        }
    }
}
