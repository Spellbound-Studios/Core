// Copyright (c) Spellbound Studios. All rights reserved.

using UnityEngine;

namespace Spellbound.Core.Simulation {
    internal sealed class TickClockDriver : MonoBehaviour {
        private const string ObjectName = "Spellbound Tick Clock";

        internal TickClock Clock { get; private set; }

        internal static TickClockDriver Attach(TickClock clock) {
            var host = new GameObject(ObjectName) {
                hideFlags = Application.isPlaying
                    ? HideFlags.HideInHierarchy
                    : HideFlags.HideInHierarchy | HideFlags.DontSave
            };

            if (Application.isPlaying) {
                DontDestroyOnLoad(host);
            }

            host.AddComponent<TickClockAdvanceStep>().Clock = clock;
            host.AddComponent<TickClockFinishStep>().Clock = clock;
            var driver = host.AddComponent<TickClockDriver>();
            driver.Clock = clock;
            return driver;
        }

        internal void Stop() {
            if (Application.isPlaying) {
                Destroy(gameObject);
            }
            else {
                DestroyImmediate(gameObject);
            }
        }
    }
}
