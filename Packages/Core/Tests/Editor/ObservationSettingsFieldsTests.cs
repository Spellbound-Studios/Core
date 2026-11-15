// Copyright (c) Spellbound Studios. All rights reserved.

using NUnit.Framework;
using Spellbound.Core.Observations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Spellbound.Core.Tests {
    public class ObservationSettingsFieldsTests {
        private ObservationSettings _settings;
        private GuardPolicy _policy;

        [SetUp]
        public void CreateAssets() {
            _settings = ScriptableObject.CreateInstance<ObservationSettings>();
            _policy = ScriptableObject.CreateInstance<GuardPolicy>();
        }

        [TearDown]
        public void DestroyAssets() {
            Object.DestroyImmediate(_settings);
            Object.DestroyImmediate(_policy);
        }

        [Test]
        public void ANewAssetHoldsThirtyTwoThousandSevenHundredSixtyEightRecorderCells() {
            Assert.AreEqual(32768, ObservationSettings.DefaultRecorderCells);
            Assert.AreEqual(ObservationSettings.DefaultRecorderCells, _settings.recorderCells);
        }

        [Test]
        public void ANewAssetHasNoRateCapsAndNoGuardPolicies() {
            Assert.IsNotNull(_settings.rateCaps);
            Assert.IsEmpty(_settings.rateCaps);
            Assert.IsNotNull(_settings.guardPolicies);
            Assert.IsEmpty(_settings.guardPolicies);
        }

        [Test]
        public void ARateCapNamesAKeyAndRecordsPerSecond() {
            _settings.rateCaps.Add(new ObservationRateCap {
                key = "settingstests.rate.capped",
                recordsPerSecond = 120
            });

            Assert.AreEqual("settingstests.rate.capped", _settings.rateCaps[0].key);
            Assert.AreEqual(120.0, _settings.rateCaps[0].recordsPerSecond);
        }

        [Test]
        public void AGuardPolicyOverrideNamesAKeyAndAPolicy() {
            _settings.guardPolicies.Add(new ObservationGuardPolicyOverride {
                key = "settingstests.guard.policed",
                policy = _policy
            });

            Assert.AreEqual("settingstests.guard.policed", _settings.guardPolicies[0].key);
            Assert.AreSame(_policy, _settings.guardPolicies[0].policy);
        }
    }
}
