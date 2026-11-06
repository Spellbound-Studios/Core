// Copyright (c) Spellbound Studios. All rights reserved.

using Spellbound.Core.Observations;

namespace Spellbound.Core.Tests {
    public static class ObservationMixedFixture {
        public static readonly Counter Mixed = CoreKit.Observations.Counter("observationtests.scan.mixed",
            "A key declared beside other static state, which the scan leaves alone.");

        public static int OtherState = 7;
    }
}
