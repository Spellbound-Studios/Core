// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using NUnit.Framework;
using Spellbound.Core.Observations;
using Spellbound.Core.Simulation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Spellbound.Core.Tests {
    public class ObservationContextRateTests {
        private string _name;

        [SetUp]
        public void NameTheContext() {
            _name = "contextrate_" + Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        [Test]
        public void AContextCreatedWithARateReadsItBack() {
            using (var context = CoreKit.Observations.CreateContext(_name, null, TickRate.PerSecond(20))) {
                Assert.AreEqual(20.0, context.Rate.TicksPerSecond);
            }
        }

        [Test]
        public void TheNameOnlyOverloadRunsAtFiftyTicksASecond() {
            using (var context = CoreKit.Observations.CreateContext(_name)) {
                Assert.AreEqual(50.0, context.Rate.TicksPerSecond);
            }
        }

        [Test]
        public void TheSettingsOverloadRunsAtFiftyTicksASecond() {
            var settings = ScriptableObject.CreateInstance<ObservationSettings>();

            try {
                using (var context = CoreKit.Observations.CreateContext(_name, settings)) {
                    Assert.AreEqual(50.0, context.Rate.TicksPerSecond);
                }
            }
            finally {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void TheDiscardContextRunsAtFiftyTicksASecond() {
            Assert.AreEqual(50.0, ObservationContext.Discard.Rate.TicksPerSecond);
        }

        [Test]
        public void TheRateOverloadStillRefusesABlankName() {
            Assert.Throws<ArgumentException>(() =>
                CoreKit.Observations.CreateContext("  ", null, TickRate.PerSecond(20)));
        }
    }
}
