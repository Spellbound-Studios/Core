// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using Spellbound.Core.Operations;
using Spellbound.Core.Packing;
using Spellbound.Core.Simulation;
using UnityEngine;

namespace Spellbound.Core {
    public static class CoreKit {
        private static OperationHost _operations;

        public static OperationHost Operations => _operations ??= new OperationHost();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlaySession() {
            _operations = null;
        }

        public static class Packing {
            public static string Describe(Type type) => PackedLayoutDescription.Describe(type);
        }

        public static class Time {
            public static TickClock StartClock(ITickSource source) {
                var clock = new TickClock(source);
                clock.Driver = TickClockDriver.Attach(clock);
                return clock;
            }

            public static TickClock StartClock(TickRate rate) => StartClock(new GameTimeTickSource(rate));

            public static void StopClock(TickClock clock) {
                if (clock?.Driver == null) {
                    return;
                }

                clock.Driver.Stop();
                clock.Driver = null;
            }
        }
    }
}
