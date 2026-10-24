// Copyright 2026 Spellbound Studio Inc.

using Spellbound.Core.Logging;
using UnityEngine;

namespace Spellbound.Core.Console {
    internal sealed class GameConsoleOutput : IGameConsoleOutput {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register() => GameConsoleSink.Output = new GameConsoleOutput();

        public void PrintLine(string message) => ConsoleLogger.PrintToConsole(message);

        public void PrintError(string message) => ConsoleLogger.PrintError(message);
    }
}
