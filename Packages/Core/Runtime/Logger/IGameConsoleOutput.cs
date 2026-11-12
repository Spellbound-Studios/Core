// Copyright 2026 Spellbound Studio Inc.

namespace Spellbound.Core.Logging {
    public interface IGameConsoleOutput {
        void PrintLine(string message);
        void PrintError(string message);
    }
}
