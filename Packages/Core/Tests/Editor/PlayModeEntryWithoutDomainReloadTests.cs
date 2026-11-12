// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Spellbound.Core.Console;
using Spellbound.Core.Observations;
using Spellbound.Core.Packing;
using Spellbound.Core.Tests.PlaySessionFixture;
using UnityEditor;
using UnityEngine.TestTools;

namespace Spellbound.Core.Tests {
    public class PlayModeEntryWithoutDomainReloadTests {
        private const string PlantedObservationKey = "core.tests.planted_on_first_entry";

        private bool _enterPlayModeOptionsEnabled;
        private EnterPlayModeOptions _enterPlayModeOptions;

        [SetUp]
        public void RememberPlayModeOptions() {
            _enterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            _enterPlayModeOptions = EditorSettings.enterPlayModeOptions;
        }

        [UnityTearDown]
        public IEnumerator LeavePlayModeAndRestoreOptions() {
            if (EditorApplication.isPlaying) {
                yield return new ExitPlayMode();
            }

            EditorSettings.enterPlayModeOptionsEnabled = _enterPlayModeOptionsEnabled;
            EditorSettings.enterPlayModeOptions = _enterPlayModeOptions;
        }

        [UnityTest]
        public IEnumerator EachEntryStartsFromResetRegistriesWithDomainReloadOff() {
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

            yield return new EnterPlayMode(false);
            var firstEntries = PlaySessionEntries.Count;
            var firstPackers = RegisteredPackers();
            var firstCommandRegistry = FilledCommandRegistry();
            var firstCommands = firstCommandRegistry.GetAllCommands().ToList();
            var firstDeclarations = CoreKit.Observations.Declared().Count;
            var planted = CoreKit.Observations.Counter(PlantedObservationKey, "Planted on the first entry only.");
            var plantedOnFirstEntry = IsDeclared(planted.Key);
            yield return new ExitPlayMode();

            yield return new EnterPlayMode(false);
            var secondEntries = PlaySessionEntries.Count;
            var secondPackers = RegisteredPackers();
            var plantedOnSecondEntry = IsDeclared(planted.Key);
            var secondCommandRegistry = FilledCommandRegistry();
            var secondCommands = secondCommandRegistry.GetAllCommands().ToList();
            var secondDeclarations = CoreKit.Observations.Declared().Count;
            yield return new ExitPlayMode();

            Assert.AreEqual(1, firstEntries, "The fixture static was not counted once on the first entry.");
            Assert.AreEqual(1, secondEntries, "The fixture static kept the first entry's count.");

            Assert.Greater(firstPackers.Count, 0, "No packer was registered on the first entry.");
            Assert.AreEqual(firstPackers.Count, secondPackers.Count, "The packer count changed between entries.");
            Assert.IsFalse(secondPackers.Any(packer => firstPackers.Any(first => ReferenceEquals(first, packer))),
                "The second entry keeps the first entry's packers.");

            Assert.AreNotSame(firstCommandRegistry, secondCommandRegistry,
                "The second entry keeps the first entry's command registry.");
            Assert.Greater(firstCommands.Count, 0, "No command was registered on the first entry.");
            Assert.AreEqual(firstCommands.Count, secondCommands.Count, "The command count changed between entries.");
            Assert.IsFalse(secondCommands.Any(command => firstCommands.Any(first => ReferenceEquals(first, command))),
                "The second entry keeps the first entry's commands.");

            Assert.Greater(firstDeclarations, 0, "No observation was declared on the first entry.");
            Assert.AreEqual(firstDeclarations, secondDeclarations, "The declaration count changed between entries.");
            Assert.IsTrue(plantedOnFirstEntry, "The planted observation was not declared on the first entry.");
            Assert.IsFalse(plantedOnSecondEntry, "The second entry keeps the first entry's planted observation.");
        }

        private static List<ISmartPacker> RegisteredPackers() {
            return SmartPackerRegistry.Registered.ToList();
        }

        private static CommandRegistry FilledCommandRegistry() {
            var registry = CommandRegistry.Instance;
            registry.AutoRegisterCommands();
            return registry;
        }

        private static bool IsDeclared(ObservationKey key) {
            return ObservationRegistry.Find(key) != null;
        }
    }
}
