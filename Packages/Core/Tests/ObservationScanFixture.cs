// Copyright (c) Spellbound Studios. All rights reserved.

using Spellbound.Core.Observations;

namespace Spellbound.Core.Tests {
    public static class ObservationScanFixture {
        public static readonly Counter UntouchedUntilScanned = CoreKit.Observations.Counter(
            "observationtests.scan.untouched",
            "A key declared in a static field of a class no test references before Declared runs.");
    }
}
