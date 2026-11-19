// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Spellbound.Core.Controls.Editor;
using Spellbound.Core.Logging;
using Spellbound.Core.Logging.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spellbound.Core.Tests {
    public class LogSettingsInspectorTests {
        private LogConfig _config;
        private UnityEditor.Editor _inspector;

        private string _storedApplied;

        [SetUp]
        public void SetUp() {
            _config = ScriptableObject.CreateInstance<LogConfig>();
            _storedApplied = UnityEditor.SessionState.GetString(LogSettingsInspector.AppliedSessionKey, string.Empty);
            UnityEditor.SessionState.EraseString(LogSettingsInspector.AppliedSessionKey);
        }

        [TearDown]
        public void TearDown() {
            if (_inspector != null) {
                UnityEngine.Object.DestroyImmediate(_inspector);
            }

            UnityEngine.Object.DestroyImmediate(_config);

            if (string.IsNullOrEmpty(_storedApplied)) {
                UnityEditor.SessionState.EraseString(LogSettingsInspector.AppliedSessionKey);
            }
            else {
                UnityEditor.SessionState.SetString(LogSettingsInspector.AppliedSessionKey, _storedApplied);
            }
        }

        [Test]
        public void TheLogConfigIsDrawnByTheLogSettingsInspector() {
            Draw();
            Assert.IsInstanceOf<LogSettingsInspector>(_inspector);
        }

        [Test]
        public void AHeaderThenTheFourCardsThenTheApplyButtonAndAnEmptyResultLine() {
            var root = Draw();
            Assert.IsTrue(root.ClassListContains(LogSettingsInspector.ClassName));
            Assert.IsInstanceOf<HeaderBar>(root[0]);
            Assert.AreEqual("Spellbound Log Configuration", ((HeaderBar)root[0]).Title);
            var cards = Cards(root);
            CollectionAssert.AreEqual(new[] { "Log Level", "File Output", "Unity Log", "Sinks" },
                cards.Select(card => card.Title).ToArray());
            CollectionAssert.AreEqual(
                new[] {
                    LogSettingsInspector.LevelCardName, LogSettingsInspector.FileCardName,
                    LogSettingsInspector.CaptureCardName, LogSettingsInspector.SinksCardName
                },
                cards.Select(card => card.name).ToArray());
            var apply = root.Q<Button>(className: LogSettingsInspector.ApplyClassName);
            Assert.AreEqual("Apply", apply.text);
            Assert.AreSame(apply, root[root.childCount - 2]);
            var applied = root[root.childCount - 1] as Label;
            Assert.IsNotNull(applied);
            Assert.IsTrue(applied.ClassListContains(LogSettingsInspector.AppliedClassName));
            Assert.AreEqual(string.Empty, applied.text);
        }

        [Test]
        public void TheLastAppliedLineOfTheSessionOutlivesTheRecompileApplyStarts() {
            const string applied = "Applied log level Warning for Standalone: SPELLBOUND_LOG_WARNING.";
            UnityEditor.SessionState.SetString(LogSettingsInspector.AppliedSessionKey, applied);
            var root = Draw();
            Assert.AreEqual(applied, root.Q<Label>(className: LogSettingsInspector.AppliedClassName).text);
        }

        [Test]
        public void EveryCardKeepsItsSubtitleAndBindsItsField() {
            _config.logFileName = "fixture.log";
            _config.captureUnityLog = true;
            var cards = Cards(Draw());
            Assert.AreEqual("Minimum severity compiled into all Spellbound packages.", SubtitleOf(cards[0]));
            Assert.AreEqual(nameof(LogConfig.globalLevel), cards[0].Q<EnumField>().bindingPath);
            Assert.AreEqual("Name of the log file written to the persistent data path.", SubtitleOf(cards[1]));
            var fileName = cards[1].Q<TextField>();
            Assert.AreEqual("Log File Name", fileName.label);
            Assert.AreEqual(nameof(LogConfig.logFileName), fileName.bindingPath);
            Assert.AreEqual("fixture.log", fileName.value);
            Assert.AreEqual("Routes Debug.Log, warnings, errors and exceptions through the sinks below.",
                SubtitleOf(cards[2]));
            var capture = cards[2].Q<Toggle>();
            Assert.AreEqual("Capture Unity Log", capture.label);
            Assert.AreEqual(nameof(LogConfig.captureUnityLog), capture.bindingPath);
            Assert.IsTrue(capture.value);
            Assert.AreEqual("Toggle where log output is routed.", SubtitleOf(cards[3]));
        }

        [TestCase(LogLevel.Verbose, "Compiles all log calls: Verbose, Debug, Info, Warning, and Error.")]
        [TestCase(LogLevel.Debug, "Compiles Debug, Info, Warning, and Error. Strips Verbose.")]
        [TestCase(LogLevel.Info, "Compiles Info, Warning, and Error. Strips Debug and below.")]
        [TestCase(LogLevel.Warning, "Compiles Warning and Error only. Strips Info and below.")]
        [TestCase(LogLevel.Error, "Error only. All other log calls are stripped from the build.")]
        [TestCase(LogLevel.None, "Strips all log calls except Error. Equivalent to Error.")]
        public void TheLevelCardDescribesTheChosenLevel(LogLevel level, string description) {
            _config.globalLevel = level;
            var root = Draw();
            Assert.AreEqual(description, root.Q<Label>(className: LogSettingsInspector.LevelDescriptionClassName).text);
            Assert.AreEqual(level, root.Q<EnumField>().value);
        }

        [Test]
        public void EachDiscoveredSinkGetsARowWithItsToggleAndItsFilter() {
            var discovered = LogSinkDiscovery.Discover();
            Assert.Greater(discovered.Count, 0);
            var rows = SinkRows(Draw());
            Assert.AreEqual(discovered.Count, rows.Count);
            Assert.AreEqual(discovered.Count, _config.sinks.Length);

            for (var index = 0; index < rows.Count; index++) {
                var toggle = rows[index].Q<Toggle>();
                Assert.AreEqual(discovered[index].DisplayName, toggle.label);
                Assert.AreEqual("sinks.Array.data[" + index + "].enabled", toggle.bindingPath);
                Assert.IsFalse(toggle.value);
                var filter = rows[index].Q<PopupField<LogLevel>>(className: LogSettingsInspector.SinkFilterClassName);
                Assert.AreEqual("Filter Level", filter.label);
            }
        }

        [TestCase(LogLevel.Verbose,
            new[] { LogLevel.Verbose, LogLevel.Debug, LogLevel.Info, LogLevel.Warning, LogLevel.Error })]
        [TestCase(LogLevel.Warning, new[] { LogLevel.Warning, LogLevel.Error })]
        [TestCase(LogLevel.Error, new[] { LogLevel.Error })]
        [TestCase(LogLevel.None, new[] { LogLevel.Error })]
        public void TheFilterChoicesLeaveOutLevelsBelowTheGlobalOneAndNone(LogLevel globalLevel, LogLevel[] expected) {
            _config.globalLevel = globalLevel;
            var filters = Filters(Draw());
            Assert.Greater(filters.Count, 0);

            foreach (var filter in filters) {
                CollectionAssert.AreEqual(expected, filter.choices);
                CollectionAssert.DoesNotContain(filter.choices, LogLevel.None);
            }
        }

        [Test]
        public void AStoredFilterBelowTheGlobalLevelShowsTheGlobalLevelAndIsNotRewritten() {
            _config.globalLevel = LogLevel.Warning;
            _config.sinks = new[] {
                new SinkEntry {
                    qualifiedTypeName = typeof(UnityConsoleSink).AssemblyQualifiedName, displayName = "Unity Console",
                    enabled = true, filterLevel = LogLevel.Debug
                }
            };

            var row = SinkRows(Draw())[0];
            Assert.AreEqual("Unity Console", row.Q<Toggle>().label);
            Assert.IsTrue(row.Q<Toggle>().value);
            Assert.AreEqual(LogLevel.Warning, row.Q<PopupField<LogLevel>>().value);
            Assert.AreEqual(LogLevel.Debug, _config.sinks[0].filterLevel);
        }

        [Test]
        public void ChoosingAGlobalLevelUpdatesTheDescriptionAndTheFilterChoices() {
            _config.globalLevel = LogLevel.Verbose;
            var root = Draw();
            var window = ControlTestWindow.Open(root);

            try {
                Assert.IsNotNull(root.panel, "the test window gave the inspector no panel");
                root.Q<EnumField>().value = LogLevel.Warning;
                Assert.AreEqual("Compiles Warning and Error only. Strips Info and below.",
                    root.Q<Label>(className: LogSettingsInspector.LevelDescriptionClassName).text);

                foreach (var filter in Filters(root)) {
                    CollectionAssert.AreEqual(new[] { LogLevel.Warning, LogLevel.Error }, filter.choices);
                    Assert.AreEqual(LogLevel.Warning, filter.value);
                }
            }
            finally {
                window.Close();
            }
        }

        private VisualElement Draw() {
            _inspector = UnityEditor.Editor.CreateEditor(_config);
            return _inspector.CreateInspectorGUI();
        }

        private static List<SectionCard> Cards(VisualElement root) {
            return root.Query<SectionCard>().ToList();
        }

        private static List<VisualElement> SinkRows(VisualElement root) {
            return root.Query<VisualElement>(className: LogSettingsInspector.SinkClassName).ToList();
        }

        private static List<PopupField<LogLevel>> Filters(VisualElement root) {
            return root.Query<PopupField<LogLevel>>(className: LogSettingsInspector.SinkFilterClassName).ToList();
        }

        private static string SubtitleOf(SectionCard card) {
            return card.Q<Label>(className: LogSettingsInspector.SubtitleClassName).text;
        }
    }
}
