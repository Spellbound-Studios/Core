// Copyright (c) Spellbound Studios. All rights reserved.

using Spellbound.Core.Logging;

namespace Spellbound.Core.Observations {
    public static class CoreObservations {
        public static readonly Counter ClockRateChanged = CoreKit.Observations.Counter("core.clock.rate_changed",
            "The tick source answered a rate other than the one the clock started with; the clock keeps its rate."
            + " The reason is the new rate.");

        public static readonly Counter ClockHeld = CoreKit.Observations.Counter("core.clock.held",
            "The tick source answered a tick below the clock's current tick, so the clock held where it was"
            + " for that step.");

        public static readonly Counter LogVerbose = CoreKit.Observations.Counter("core.log.verbose",
            "A line was logged at the Verbose level. The reason is the call site.");

        public static readonly Counter LogDebug = CoreKit.Observations.Counter("core.log.debug",
            "A line was logged at the Debug level. The reason is the call site.");

        public static readonly Counter LogInfo = CoreKit.Observations.Counter("core.log.info",
            "A line was logged at the Info level. The reason is the call site.");

        public static readonly Counter LogWarning = CoreKit.Observations.Counter("core.log.warning",
            "A line was logged at the Warning level. The reason is the call site.");

        public static readonly Counter LogError = CoreKit.Observations.Counter("core.log.error",
            "A line was logged at the Error level. The reason is the call site.");

        internal static Counter LogCounter(LogLevel level) {
            switch (level) {
                case LogLevel.Verbose:
                    return LogVerbose;
                case LogLevel.Debug:
                    return LogDebug;
                case LogLevel.Info:
                    return LogInfo;
                case LogLevel.Warning:
                    return LogWarning;
                case LogLevel.Error:
                    return LogError;
                default:
                    return null;
            }
        }
    }
}
