// Copyright 2026 Spellbound Studio Inc.

using NUnit.Framework;
using Spellbound.Core.Console;

namespace Spellbound.Core.Tests {
    public class ConsoleLoggerTests {
        [Test]
        public void PrintToConsoleRaisesLinePrinted() {
            string received = null;
            void Handler(string message) => received = message;

            ConsoleLogger.LinePrinted += Handler;

            try {
                ConsoleLogger.PrintToConsole("hello");
                Assert.AreEqual("hello", received);
            }
            finally {
                ConsoleLogger.LinePrinted -= Handler;
            }
        }

        [Test]
        public void PrintErrorRaisesErrorPrinted() {
            string received = null;
            void Handler(string message) => received = message;

            ConsoleLogger.ErrorPrinted += Handler;

            try {
                ConsoleLogger.PrintError("boom");
                Assert.AreEqual("boom", received);
            }
            finally {
                ConsoleLogger.ErrorPrinted -= Handler;
            }
        }

        [Test]
        public void ClearRaisesCleared() {
            var cleared = false;
            void Handler() => cleared = true;

            ConsoleLogger.Cleared += Handler;

            try {
                ConsoleLogger.Clear();
                Assert.IsTrue(cleared);
            }
            finally {
                ConsoleLogger.Cleared -= Handler;
            }
        }

        [Test]
        public void IsInitializedTracksLineSubscribers() {
            void Handler(string message) { }

            Assert.IsFalse(ConsoleLogger.IsInitialized);

            ConsoleLogger.LinePrinted += Handler;

            try {
                Assert.IsTrue(ConsoleLogger.IsInitialized);
            }
            finally {
                ConsoleLogger.LinePrinted -= Handler;
            }

            Assert.IsFalse(ConsoleLogger.IsInitialized);
        }
    }
}
