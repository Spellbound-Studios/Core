// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;

namespace Spellbound.Core.Logging.Editor {
    public static class LogLevelDefines {
        public const string Prefix = "SPELLBOUND_LOG_";
        public const string VerboseDefine = Prefix + "VERBOSE";
        public const string DebugDefine = Prefix + "DEBUG";
        public const string InfoDefine = Prefix + "INFO";
        public const string WarningDefine = Prefix + "WARNING";

        public static IReadOnlyList<string> For(LogLevel level) {
            switch (level) {
                case LogLevel.Verbose:
                    return new[] { VerboseDefine, DebugDefine, InfoDefine, WarningDefine };
                case LogLevel.Debug:
                    return new[] { DebugDefine, InfoDefine, WarningDefine };
                case LogLevel.Info:
                    return new[] { InfoDefine, WarningDefine };
                case LogLevel.Warning:
                    return new[] { WarningDefine };
                case LogLevel.Error:
                case LogLevel.None:
                    return Array.Empty<string>();
                default:
                    Log.Error("Unhandled log level: " + level);
                    return Array.Empty<string>();
            }
        }

        public static string[] Merge(IEnumerable<string> current, LogLevel level) {
            var merged = new List<string>();

            if (current != null) {
                foreach (var define in current) {
                    if (define != null && !define.StartsWith(Prefix, StringComparison.Ordinal)) {
                        merged.Add(define);
                    }
                }
            }

            merged.AddRange(For(level));
            return merged.ToArray();
        }

        public static IReadOnlyList<LogLevel> CompiledLevels(LogLevel globalLevel) {
            var levels = new List<LogLevel>();

            foreach (LogLevel level in Enum.GetValues(typeof(LogLevel))) {
                if (level != LogLevel.None && (level >= globalLevel || level == LogLevel.Error)) {
                    levels.Add(level);
                }
            }

            return levels;
        }
    }
}
