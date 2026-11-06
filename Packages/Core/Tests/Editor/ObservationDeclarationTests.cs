// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Reflection;
using NUnit.Framework;
using Spellbound.Core.Observations;

namespace Spellbound.Core.Tests {
    public class ObservationDeclarationTests {
        [Test]
        public void EachKindDeclaresWithItsDefaults() {
            var counter = CoreKit.Observations.Counter("observationtests.kinds.counter", "A counter.");
            var occurrence = CoreKit.Observations.Occurrence("observationtests.kinds.occurrence", "An occurrence.");
            var gauge = CoreKit.Observations.Gauge("observationtests.kinds.gauge", "A gauge.", ObservationUnit.Bytes);
            var cost = CoreKit.Observations.Cost("observationtests.kinds.cost", "A cost.", ObservationUnit.Milliseconds,
                0.4, 2);

            Assert.AreEqual(ObservationKind.Counter, counter.Declaration.Kind);
            Assert.AreEqual(ObservationUnit.Count, counter.Declaration.Unit);
            Assert.IsFalse(counter.Declaration.HasDefaultBudget);
            Assert.AreEqual(ObservationKind.Occurrence, occurrence.Declaration.Kind);
            Assert.AreEqual(ObservationKind.Gauge, gauge.Declaration.Kind);
            Assert.AreEqual(ObservationUnit.Bytes, gauge.Declaration.Unit);
            Assert.AreEqual(ObservationKind.Cost, cost.Declaration.Kind);
            Assert.AreEqual(0.4, cost.Declaration.DefaultBudget);
            Assert.AreEqual(2, cost.Declaration.DefaultQualityLevel);
            Assert.IsTrue(cost.Declaration.HasDefaultBudget);
            Assert.AreEqual("observationtests.kinds.cost", cost.Key.Name);
        }

        [Test]
        public void TheShortCostFormIsMillisecondsWithNoBudget() {
            var cost = CoreKit.Observations.Cost("observationtests.kinds.short_cost", "A cost in milliseconds.");
            Assert.AreEqual(ObservationUnit.Milliseconds, cost.Declaration.Unit);
            Assert.IsFalse(cost.Declaration.HasDefaultBudget);
            Assert.AreEqual(0, cost.Declaration.DefaultQualityLevel);
        }

        [Test]
        public void ACustomUnitIsKept() {
            var gauge = CoreKit.Observations.Gauge("observationtests.kinds.custom_unit", "Rows awake.",
                ObservationUnit.Named("rows"));
            Assert.AreEqual("rows", gauge.Declaration.Unit.Name);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        public void TheDescriptionIsRequired(string description) {
            Assert.Throws<ArgumentException>(() =>
                CoreKit.Observations.Counter("observationtests.required.description", description));
        }

        [Test]
        public void AnInvalidKeyIsRefused() {
            Assert.Throws<ArgumentException>(() => CoreKit.Observations.Counter("NoDots", "A key with one segment."));
        }

        [Test]
        public void TheSameDeclarationTwiceReturnsTheFirst() {
            var first = CoreKit.Observations.Counter("observationtests.duplicate.same", "Declared twice the same way.");
            var second = CoreKit.Observations.Counter("observationtests.duplicate.same",
                "Declared twice the same way.");
            Assert.AreSame(first.Declaration, second.Declaration);
        }

        [Test]
        public void AnotherKindOrDescriptionOnOneKeyThrows() {
            CoreKit.Observations.Counter("observationtests.duplicate.kind", "A counter.");
            Assert.Throws<ArgumentException>(() =>
                CoreKit.Observations.Gauge("observationtests.duplicate.kind", "A counter.", ObservationUnit.Count));
            Assert.Throws<ArgumentException>(() =>
                CoreKit.Observations.Counter("observationtests.duplicate.kind", "Another description."));
        }

        [Test]
        public void AReasonRoundTrips() {
            var reason = CoreKit.Observations.Reason("embedded");
            Assert.AreEqual("embedded", reason.Name);
            Assert.AreNotEqual(0u, reason.Id);
            Assert.AreEqual("embedded", CoreKit.Observations.ReasonName(reason.Id));
            Assert.AreEqual(reason, CoreKit.Observations.Reason(" embedded "));
            Assert.IsTrue(ObservationReason.None.IsNone);
            Assert.Throws<ArgumentException>(() => CoreKit.Observations.Reason(" "));
        }

        [Test]
        public void DeclaredListsEveryDeclarationSortedByKey() {
            var counter = CoreKit.Observations.Counter("observationtests.declared.zz", "Last in order.");
            var gauge = CoreKit.Observations.Gauge("observationtests.declared.aa", "First in order.",
                ObservationUnit.Count);
            var declared = CoreKit.Observations.Declared();
            var indexOfGauge = -1;
            var indexOfCounter = -1;

            for (var i = 0; i < declared.Count; i++) {
                if (declared[i].Key == gauge.Key) {
                    indexOfGauge = i;
                }

                if (declared[i].Key == counter.Key) {
                    indexOfCounter = i;
                }
            }

            Assert.GreaterOrEqual(indexOfGauge, 0);
            Assert.Greater(indexOfCounter, indexOfGauge);
        }

        [Test]
        public void DeclaredFindsAStaticFieldNoCodeTouched() {
            var found = false;

            foreach (var declaration in CoreKit.Observations.Declared()) {
                if (declaration.Key.Name == "observationtests.scan.untouched") {
                    found = true;
                }
            }

            Assert.IsTrue(found,
                "the scan did not find the field of ObservationScanFixture, a class no test references");
        }

        [Test]
        public void AClassThatMixesADeclarationWithOtherStaticStateIsNotScanned() {
            foreach (var declaration in CoreKit.Observations.Declared()) {
                Assert.AreNotEqual("observationtests.scan.mixed", declaration.Key.Name);
            }
        }

        [Test]
        public void ADeclarationRegistersItselfAgainAfterTheReset() {
            var counter = CoreKit.Observations.Counter("observationtests.reset.counter",
                "Survives the play session reset through its field.");

            try {
                ResetRegistry();
                Assert.IsFalse(IsDeclaredWithoutScan(counter.Key));

                using var observations = CoreKit.Observations.CreateContext("reset_world");
                counter.Add(observations);
                Assert.IsTrue(IsDeclaredWithoutScan(counter.Key));
                Assert.IsTrue(observations.TryRead(counter.Key, out var reading));
                Assert.AreEqual(1, reading.Value);
            }
            finally {
                CoreKit.Observations.Declared();
            }
        }

        private static bool IsDeclaredWithoutScan(ObservationKey key) {
            var registry = typeof(CoreKit).Assembly.GetType("Spellbound.Core.Observations.ObservationRegistry");
            var find = registry.GetMethod("Find", BindingFlags.Static | BindingFlags.NonPublic);
            return find.Invoke(null, new object[] { key }) != null;
        }

        private static void ResetRegistry() {
            var registry = typeof(CoreKit).Assembly.GetType("Spellbound.Core.Observations.ObservationRegistry");
            var reset = registry.GetMethod("ResetForPlaySession", BindingFlags.Static | BindingFlags.NonPublic);
            reset.Invoke(null, null);
        }
    }
}
