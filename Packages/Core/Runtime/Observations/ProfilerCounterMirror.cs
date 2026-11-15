// Copyright (c) Spellbound Studios. All rights reserved.

#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using Unity.Profiling.LowLevel.Unsafe;
#endif

namespace Spellbound.Core.Observations {
    public sealed class ProfilerCounterMirror {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private readonly ObservationContext _context;
        private readonly List<ObservationReading> _readings = new List<ObservationReading>();
        private readonly Dictionary<uint, IntPtr> _counterValues = new Dictionary<uint, IntPtr>();
#endif

        internal ProfilerCounterMirror(ObservationContext context) {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _context = context;
#endif
        }

        public void Refresh() {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_context.IsDisposed) {
                return;
            }

            _context.Read(_readings);

            for (var i = 0; i < _readings.Count; i++) {
                Publish(_readings[i]);
            }
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private unsafe void Publish(in ObservationReading reading) {
            if (!_counterValues.TryGetValue(reading.Key.Hash, out var counterValue)) {
                counterValue = (IntPtr)ProfilerUnsafeUtility.CreateCounterValue(out _, reading.Key.Name,
                    ProfilerCategory.Scripts, MarkerFlags.Default, (byte)ProfilerMarkerDataType.Double,
                    (byte)DataUnit(reading.Unit), sizeof(double), ProfilerCounterOptions.None);
                _counterValues.Add(reading.Key.Hash, counterValue);
            }

            var value = (double*)counterValue;

            if (*value == reading.Value) {
                return;
            }

            *value = reading.Value;
            ProfilerUnsafeUtility.FlushCounterValue(value);
        }

        private static ProfilerMarkerDataUnit DataUnit(ObservationUnit unit) {
            if (unit.Equals(ObservationUnit.Bytes)) {
                return ProfilerMarkerDataUnit.Bytes;
            }

            return unit.Equals(ObservationUnit.Count) ? ProfilerMarkerDataUnit.Count : ProfilerMarkerDataUnit.Undefined;
        }
#endif
    }
}
