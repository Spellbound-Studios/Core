// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Reflection;
using NUnit.Framework;
using Spellbound.Core.Authoring;
using Spellbound.Core.Intents;
using Spellbound.Core.Observations;
using Spellbound.Core.Simulation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Spellbound.Core.Tests {
    public class IntentLedgerSettingsTests {
        private ObservationContext _context;
        private IntentLedgerSettings _settings;

        [SetUp]
        public void CreateSettings() {
            var suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            _context = CoreKit.Observations.CreateContext("ledgersettings_" + suffix);
            _settings = ScriptableObject.CreateInstance<IntentLedgerSettings>();
        }

        [TearDown]
        public void DestroySettings() {
            Object.DestroyImmediate(_settings);
            _context.Dispose();
        }

        [Test]
        public void TheSettingsAssetDefaultsAreThirtyTwoAndThreeHundredSeconds() {
            Assert.AreEqual(32, IntentLedgerSettings.DefaultWindowSize);
            Assert.AreEqual(300.0, IntentLedgerSettings.DefaultDepartureGraceSeconds);
            Assert.AreEqual(IntentLedgerSettings.DefaultWindowSize, _settings.windowSize);
            Assert.AreEqual(IntentLedgerSettings.DefaultDepartureGraceSeconds, _settings.departureGraceSeconds);

            var report = new ValidationReport("defaults");
            _settings.Validate(report);
            Assert.IsTrue(report.IsEmpty, report.ToText());
        }

        [Test]
        public void TheSettingsAssetIsUnderTheSpellboundMenu() {
            var menu = typeof(IntentLedgerSettings).GetCustomAttribute<CreateAssetMenuAttribute>();
            Assert.IsNotNull(menu);
            StringAssert.StartsWith("Spellbound/", menu.menuName);
            Assert.IsTrue(typeof(IAuthoredDefinition).IsAssignableFrom(typeof(IntentLedgerSettings)));
        }

        [Test]
        public void ACustomSettingsAssetShapesTheLedger() {
            _settings.windowSize = 4;
            _settings.departureGraceSeconds = 2;
            var ledger = CoreKit.Intents.CreateLedger(_settings, TickRate.PerSecond(10), _context);
            var receipt = new IntentReceipt(ObservationReason.None, 1);
            var actor = new IntentActor(5);

            ledger.Record(new IntentKey(actor, 10), receipt);
            Assert.AreEqual(4L * 12, ledger.Bytes);
            Assert.AreEqual(IntentLedgerCheck.Stale, ledger.Check(new IntentKey(actor, 6), out _));
            Assert.AreEqual(IntentLedgerCheck.Unseen, ledger.Check(new IntentKey(actor, 7), out _));

            ledger.Depart(actor, Tick.Zero);
            Assert.AreEqual(0, ledger.Expire(new Tick(19)));
            Assert.AreEqual(1, ledger.Expire(new Tick(20)));
        }

        [Test]
        public void TheLedgerKeepsTheGraceItWasCreatedWith() {
            _settings.departureGraceSeconds = 1;
            var ledger = CoreKit.Intents.CreateLedger(_settings, TickRate.PerSecond(10), _context);
            _settings.departureGraceSeconds = 100;
            var actor = new IntentActor(5);

            ledger.Record(new IntentKey(actor, 1), IntentReceipt.None);
            ledger.Depart(actor, Tick.Zero);
            Assert.AreEqual(1, ledger.Expire(new Tick(10)));
        }

        [Test]
        public void AWindowSizeUnderOneIsRefused() {
            _settings.windowSize = 0;
            var report = new ValidationReport("window");
            _settings.Validate(report);
            Assert.IsTrue(report.HasErrors);
            Assert.AreEqual(IntentLedgerSettings.WindowSizeUnderOneCode, report.Findings[0].Code);
        }

        [Test]
        public void ANegativeGraceIsRefused() {
            _settings.departureGraceSeconds = -1;
            var report = new ValidationReport("grace");
            _settings.Validate(report);
            Assert.IsTrue(report.HasErrors);
            Assert.AreEqual(IntentLedgerSettings.GraceNegativeCode, report.Findings[0].Code);
        }

        [Test]
        public void AGraceThatIsNotAFiniteNumberIsRefused() {
            foreach (var grace in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity }) {
                _settings.departureGraceSeconds = grace;
                var report = new ValidationReport("grace");
                _settings.Validate(report);
                Assert.AreEqual(1, report.Findings.Count, report.ToText());
                Assert.AreEqual(IntentLedgerSettings.GraceNotFiniteCode, report.Findings[0].Code);
            }
        }

        [Test]
        public void FindingsAreFullSentences() {
            _settings.windowSize = 0;
            _settings.departureGraceSeconds = -2;
            var report = new ValidationReport("sentences");
            _settings.Validate(report);
            Assert.AreEqual("The window size is 0, under 1.", report.Findings[0].Message);
            Assert.AreEqual("The departure grace is -2 seconds, below zero.", report.Findings[1].Message);
        }

        [Test]
        public void CreatingALedgerFromNoSettingsThrows() {
            Assert.Throws<ArgumentNullException>(() =>
                CoreKit.Intents.CreateLedger(null, TickRate.PerSecond(10), _context));
        }
    }
}
