// Copyright 2026 Spellbound Studio Inc.

using System;
using UnityEngine;

namespace Spellbound.Core.Console {
    /// <summary>
    /// Static logging utility that prints directly to the console in build or in the editor.
    /// Useful for debugging and adding capability to your packages.
    /// </summary>
    public static class ConsoleLogger {
        public static event Action<string> LinePrinted;
        public static event Action<string> ErrorPrinted;
        public static event Action Cleared;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlaySession() {
            LinePrinted = null;
            ErrorPrinted = null;
            Cleared = null;
        }

        /// <summary>
        /// Print a message to the developer console.
        /// </summary>
        public static void PrintToConsole(string message) => LinePrinted?.Invoke(message);

        /// <summary>
        /// Print an error message to the developer console.
        /// </summary>
        public static void PrintError(string message) => ErrorPrinted?.Invoke(message);

        public static void Clear() => Cleared?.Invoke();

        /// <summary>
        /// Check if the console logger is initialized and ready to use.
        /// </summary>
        public static bool IsInitialized => LinePrinted != null;
    }
}
