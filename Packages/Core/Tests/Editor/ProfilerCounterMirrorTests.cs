// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using NUnit.Framework;
using Spellbound.Core.Observations;
using Unity.Profiling;

namespace Spellbound.Core.Tests {
    public class ProfilerCounterMirrorTests {
        private static readonly Counter Mirrored = CoreKit.Observations.Counter("profilermirrortests.counter.mirrored",
            "A counter the profiler mirror publishes as a profiler counter.");

        private static readonly Gauge Level = CoreKit.Observations.Gauge("profilermirrortests.gauge.level",
            "A gauge the profiler mirror publishes as a profiler counter.");

        [Test]
        public void AfterRefreshAProfilerRecorderReadsTheCountersValue() {
            var context = CoreKit.Observations.CreateContext(
                "profilermirror_" + Guid.NewGuid().ToString("N").Substring(0, 8));

            try {
                var mirror = CoreKit.Observations.MirrorToProfiler(context);
                Mirrored.Add(context, 3);
                mirror.Refresh();

                using (var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, Mirrored.Key.Name)) {
                    Mirrored.Add(context, 39);
                    mirror.Refresh();

                    Assert.IsTrue(recorder.Valid);
                    Assert.AreEqual(42d, recorder.CurrentValueAsDouble);
                }
            }
            finally {
                context.Dispose();
            }
        }

        [Test]
        public void AfterRefreshAProfilerRecorderReadsTheGaugesValue() {
            var context = CoreKit.Observations.CreateContext(
                "profilermirror_" + Guid.NewGuid().ToString("N").Substring(0, 8));

            try {
                var mirror = CoreKit.Observations.MirrorToProfiler(context);
                Level.Set(context, 1.5);
                mirror.Refresh();

                using (var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, Level.Key.Name)) {
                    Level.Set(context, 2.25);
                    mirror.Refresh();

                    Assert.AreEqual(2.25d, recorder.CurrentValueAsDouble);
                }
            }
            finally {
                context.Dispose();
            }
        }

        [Test]
        public void MirrorToProfilerRefusesANullContext() {
            Assert.Throws<ArgumentNullException>(() => CoreKit.Observations.MirrorToProfiler(null));
        }
    }
}
