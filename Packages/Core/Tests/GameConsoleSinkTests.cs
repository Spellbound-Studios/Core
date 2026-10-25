// Copyright 2026 Spellbound Studio Inc.

using System.Collections.Generic;
using NUnit.Framework;
using Spellbound.Core.Logging;

namespace Spellbound.Core.Tests {
    public class GameConsoleSinkTests {
        private sealed class RecordingOutput : IGameConsoleOutput {
            public readonly List<string> Lines = new();
            public readonly List<string> Errors = new();

            public void PrintLine(string message) => Lines.Add(message);

            public void PrintError(string message) => Errors.Add(message);
        }

        private IGameConsoleOutput _previousOutput;

        [SetUp]
        public void SetUp() => _previousOutput = GameConsoleSink.Output;

        [TearDown]
        public void TearDown() => GameConsoleSink.Output = _previousOutput;

        [Test]
        public void Emit_CustomImplementation_ReceivesFormattedLine() {
            var output = new RecordingOutput();
            GameConsoleSink.Output = output;

            new GameConsoleSink().Emit(LogLevel.Info, "Source", "hello", "Member", 7);

            Assert.AreEqual(new[] { "[Source.Member:7] hello" }, output.Lines);
            Assert.IsEmpty(output.Errors);
        }

        [Test]
        public void Emit_ErrorLevel_RoutesToPrintError() {
            var output = new RecordingOutput();
            GameConsoleSink.Output = output;

            new GameConsoleSink().Emit(LogLevel.Error, "Source", "boom", "Member", 3);

            Assert.AreEqual(new[] { "[Source.Member:3] boom" }, output.Errors);
            Assert.IsEmpty(output.Lines);
        }

        [Test]
        public void Emit_WithoutOutput_DoesNothing() {
            GameConsoleSink.Output = null;

            Assert.DoesNotThrow(() => new GameConsoleSink().Emit(LogLevel.Error, "Source", "boom", "Member", 1));
        }
    }
}
