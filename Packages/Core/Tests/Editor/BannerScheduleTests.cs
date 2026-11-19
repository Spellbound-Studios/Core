// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using NUnit.Framework;
using Spellbound.Core.Authoring.Editor;
using UnityEditor;

namespace Spellbound.Core.Tests {
    public class BannerScheduleTests {
        private const string Path = "Assets/Definitions/Sword.asset";
        private const string OtherPath = "Assets/Definitions/Shield.asset";
        private const string Check = "fixturecheck.threshold-high (Core)";

        private static readonly DateTime Start = new DateTime(2026, 11, 5, 12, 0, 0);

        private BannerSchedule _schedule;

        [SetUp]
        public void SetUp() {
            _schedule = new BannerSchedule(400, 50);
        }

        [Test]
        public void NothingIsDueBeforeAnyChange() {
            Assert.IsNull(_schedule.Due(Start));
            Assert.IsNull(_schedule.Due(Start.AddHours(1)));
        }

        [Test]
        public void NothingIsDueBeforeTheDelayHasPassed() {
            _schedule.Changed(Path, Start);
            Assert.IsNull(_schedule.Due(Start));
            Assert.IsNull(_schedule.Due(Start.AddMilliseconds(399)));
        }

        [Test]
        public void ThePathIsDueOnceAfterStillness() {
            Assert.IsFalse(_schedule.IsWaiting);
            _schedule.Changed(Path, Start);
            Assert.IsTrue(_schedule.IsWaiting);
            Assert.AreEqual(Path, _schedule.Due(Start.AddMilliseconds(400)));
            Assert.IsFalse(_schedule.IsWaiting);
            Assert.IsNull(_schedule.Due(Start.AddMilliseconds(401)));
            Assert.IsNull(_schedule.Due(Start.AddSeconds(10)));
        }

        [Test]
        public void ASecondChangeRestartsTheWait() {
            _schedule.Changed(Path, Start);
            _schedule.Changed(Path, Start.AddMilliseconds(300));
            Assert.IsNull(_schedule.Due(Start.AddMilliseconds(500)));
            Assert.AreEqual(Path, _schedule.Due(Start.AddMilliseconds(700)));
        }

        [Test]
        public void TheLastChangeIsKeptAfterThePathFallsDue() {
            Assert.AreEqual(DateTime.MinValue, _schedule.ChangedAt(Path));
            _schedule.Changed(Path, Start);
            _schedule.Changed(Path, Start.AddMilliseconds(100));
            Assert.AreEqual(Path, _schedule.Due(Start.AddSeconds(1)));
            Assert.AreEqual(Start.AddMilliseconds(100), _schedule.ChangedAt(Path));
        }

        [Test]
        public void EachPathWaitsOnItsOwnChanges() {
            _schedule.Changed(Path, Start);
            _schedule.Changed(OtherPath, Start.AddMilliseconds(300));
            Assert.AreEqual(Path, _schedule.Due(Start.AddMilliseconds(450)));
            Assert.IsNull(_schedule.Due(Start.AddMilliseconds(450)));
            Assert.AreEqual(OtherPath, _schedule.Due(Start.AddMilliseconds(700)));
        }

        [Test]
        public void TheClockIsTheCallersSoNoTimePassesBetweenCalls() {
            var schedule = new BannerSchedule(0, 50);
            schedule.Changed(Path, Start);
            Assert.IsNull(schedule.Due(Start.AddMilliseconds(-1)));
            Assert.AreEqual(Path, schedule.Due(Start));
        }

        [Test]
        public void ADemotedCheckStaysDemoted() {
            Assert.IsFalse(_schedule.IsDemoted(Check));
            Assert.IsFalse(_schedule.TryGetDemotion(Check, out _));
            _schedule.Demote(Check, 73.5);
            Assert.IsTrue(_schedule.IsDemoted(Check));
            Assert.IsTrue(_schedule.TryGetDemotion(Check, out var milliseconds));
            Assert.AreEqual(73.5, milliseconds);
            _schedule.Changed(Path, Start);
            _schedule.Due(Start.AddSeconds(1));
            _schedule.Demote(Check, 80);
            Assert.IsTrue(_schedule.IsDemoted(Check));
            Assert.IsTrue(_schedule.TryGetDemotion(Check, out milliseconds));
            Assert.AreEqual(80, milliseconds);
            Assert.IsFalse(_schedule.IsDemoted("fixturecheck.other (Core)"));
        }

        [Test]
        public void ARunOverTheBudgetIsOverAndOneAtTheBudgetIsNot() {
            Assert.IsFalse(_schedule.IsOverBudget(50));
            Assert.IsTrue(_schedule.IsOverBudget(50.5));
        }

        [Test]
        public void NegativeSettingsAreTreatedAsZero() {
            var schedule = new BannerSchedule(-5, -1);
            Assert.AreEqual(0, schedule.StillnessDelayMilliseconds);
            Assert.AreEqual(0, schedule.CheckBudgetMilliseconds);
        }

        [Test]
        public void DemotionsWithASessionKeyOutliveTheSchedule() {
            const string key = "Spellbound.Core.Tests.BannerScheduleDemotions";
            SessionState.EraseString(key);

            try {
                new BannerSchedule(0, 50, key).Demote(Check, 72.25);
                var restored = new BannerSchedule(0, 50, key);
                Assert.IsTrue(restored.TryGetDemotion(Check, out var milliseconds));
                Assert.AreEqual(72.25, milliseconds);
                Assert.IsFalse(new BannerSchedule(0, 50).IsDemoted(Check));
            }
            finally {
                SessionState.EraseString(key);
            }
        }
    }
}
