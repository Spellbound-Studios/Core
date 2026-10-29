// Copyright (c) Spellbound Studios. All rights reserved.

using NUnit.Framework;
using Spellbound.Core.Simulation;
using UnityEngine;

namespace Spellbound.Core.Tests {
    public class TickClockDriverHostTests {
        private const string AdvanceStepTypeName = "TickClockAdvanceStep";

        [Test]
        public void TheDriverHostIsHiddenAndNeverSaved() {
            var clock = CoreKit.Time.StartClock(TickRate.PerSecond(20));

            try {
                var host = Host();
                Assert.IsNotNull(host);
                Assert.AreEqual(HideFlags.DontSave, host.hideFlags & HideFlags.DontSave);
                Assert.AreEqual(HideFlags.HideInHierarchy, host.hideFlags & HideFlags.HideInHierarchy);
            }
            finally {
                CoreKit.Time.StopClock(clock);
            }

            Assert.IsNull(Host());
        }

        private static GameObject Host() {
            foreach (var behaviour in Resources.FindObjectsOfTypeAll<MonoBehaviour>()) {
                if (behaviour != null && behaviour.GetType().Name == AdvanceStepTypeName) {
                    return behaviour.gameObject;
                }
            }

            return null;
        }
    }
}
