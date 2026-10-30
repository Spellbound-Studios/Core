// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Spellbound.Core.Logging;
using Spellbound.Core.Operations;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Spellbound.Core.Tests {
    public class EditModeSinkTests {
        private const string Message = "the edit-mode sink carries this line to the console";
        private const string LogEntriesTypeName = "UnityEditor.LogEntries";
        private const string LogEntryTypeName = "UnityEditor.LogEntry";

        private readonly List<string> _console = new List<string>();
        private RecordingLogSink _recording;
        private OperationSettings _context;

        [SetUp]
        public void SetUp() {
            _console.Clear();
            Application.logMessageReceived += OnConsoleEntry;
            _recording = new RecordingLogSink(LogLevel.Verbose);
            Log.AddSink(_recording, null, LogLevel.Verbose);
            _context = ScriptableObject.CreateInstance<OperationSettings>();
        }

        [TearDown]
        public void TearDown() {
            Application.logMessageReceived -= OnConsoleEntry;
            Log.RemoveSink(_recording);
            UnityEngine.Object.DestroyImmediate(_context);
        }

        private void OnConsoleEntry(string condition, string stackTrace, LogType type) {
            _console.Add(type + ": " + condition);
        }

        [Test]
        public void TheConsoleSinkIsRegisteredOutsidePlayModeWhenTheDomainLoads() {
            Assert.IsFalse(Application.isPlaying);
            Assert.IsInstanceOf<UnityConsoleSink>(LogBootstrap.EditModeSink);
            Assert.IsTrue(Log.RemoveSink(LogBootstrap.EditModeSink), "the edit-mode sink was not registered");
            LogBootstrap.RegisterEditModeSink();
        }

        [Test]
        public void AnErrorLoggedOutsidePlayModeReachesTheConsoleWithItsContext() {
            LogAssert.Expect(LogType.Error, new Regex(Regex.Escape(Message)));

            Log.Error(Message, _context);

            var expected = "Error: [" + nameof(EditModeSinkTests) + "."
                           + nameof(AnErrorLoggedOutsidePlayModeReachesTheConsoleWithItsContext) + ":";
            Assert.IsTrue(_console.Exists(entry => entry.StartsWith(expected) && entry.EndsWith(Message)),
                string.Join("\n", _console));
            Assert.AreEqual(_context.GetEntityId(), ConsoleContextOf(Message));
            var recorded = _recording.Entries;
            Assert.AreEqual(1, recorded.Length);
            Assert.AreSame(_context, recorded[0].Context);
        }

        [Test]
        public void AnInformationLineReachesTheConsoleAsALogEntry() {
            Log.Emit(LogLevel.Info, Message);

            Assert.IsTrue(_console.Exists(entry => entry.StartsWith("Log: [") && entry.EndsWith(Message)),
                string.Join("\n", _console));
        }

        [Test]
        public void RegisteringTwiceKeepsOneEditModeSink() {
            var first = LogBootstrap.EditModeSink;
            LogBootstrap.RegisterEditModeSink();
            Assert.AreNotSame(first, LogBootstrap.EditModeSink);
            Assert.IsFalse(Log.RemoveSink(first));
            LogAssert.Expect(LogType.Error, new Regex(Regex.Escape(Message)));

            Log.Error(Message);

            Assert.AreEqual(1, _console.FindAll(entry => entry.EndsWith(Message)).Count);
        }

        private static object ConsoleContextOf(string message) {
            var editor = typeof(EditorWindow).Assembly;
            var logEntries = editor.GetType(LogEntriesTypeName);
            var logEntry = editor.GetType(LogEntryTypeName);
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            var start = logEntries?.GetMethod("StartGettingEntries", flags);
            var end = logEntries?.GetMethod("EndGettingEntries", flags);
            var getEntry = logEntries?.GetMethod("GetEntryInternal", flags);
            var messageField = logEntry?.GetField("message", flags);
            var contextField = logEntry?.GetField("entityId", flags);

            if (start == null || end == null || getEntry == null || messageField == null || contextField == null) {
                Assert.Inconclusive("the editor's console entries are not reachable through reflection");
            }

            var count = (int)start.Invoke(null, null);

            try {
                for (var row = count - 1; row >= 0; row--) {
                    var entry = Activator.CreateInstance(logEntry);
                    getEntry.Invoke(null, new[] { row, entry });
                    var text = (string)messageField.GetValue(entry);

                    if (text != null && text.Contains(message)) {
                        return contextField.GetValue(entry);
                    }
                }
            }
            finally {
                end.Invoke(null, null);
            }

            Assert.Fail("no console entry carries the message");
            return null;
        }
    }
}
