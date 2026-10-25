// Copyright 2026 Spellbound Studio Inc.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Spellbound.Core.Registries;
using UnityEngine;
using UnityEngine.TestTools;

namespace Spellbound.Core.Tests {
    public class ResourceRegistryTests {
        private class Entry : ScriptableObject, IRegistryEntry {
            public uint Hash { get; private set; }
            public string EntryName { get; private set; }

            public static Entry Create(uint hash, string entryName = null, string assetName = null) {
                var entry = CreateInstance<Entry>();
                entry.Hash = hash;
                entry.EntryName = entryName;
                entry.name = assetName ?? entryName ?? $"Entry{hash}";

                return entry;
            }
        }

        private readonly List<Entry> _created = new();

        private Entry Make(uint hash, string entryName = null, string assetName = null) {
            var entry = Entry.Create(hash, entryName, assetName);
            _created.Add(entry);

            return entry;
        }

        private static Func<string, Entry[]> LoaderFor(params Entry[] entries) => _ => entries;

        [TearDown]
        public void TearDown() {
            LogAssert.ignoreFailingMessages = false;

            foreach (var entry in _created)
                UnityEngine.Object.DestroyImmediate(entry);

            _created.Clear();
        }

        [Test]
        public void LoadsLazilyOnFirstAccess() {
            var loads = 0;
            var registry = new ResourceRegistry<Entry>("Things", loader: _ => {
                loads++;

                return new[] { Make(1u), Make(2u) };
            });

            Assert.AreEqual(0, loads);
            Assert.AreEqual(2, registry.Count);
            Assert.AreEqual(1, loads);

            _ = registry.All;

            Assert.AreEqual(1, loads);
        }

        [Test]
        public void LoaderReceivesResourceFolder() {
            string requestedFolder = null;
            var registry = new ResourceRegistry<Entry>("Things", loader: folder => {
                requestedFolder = folder;

                return Array.Empty<Entry>();
            });

            registry.EnsureLoaded();

            Assert.AreEqual("Things", requestedFolder);
        }

        [Test]
        public void ResolvesByHash() {
            var entry = Make(42u);
            var registry = new ResourceRegistry<Entry>("Things", loader: LoaderFor(entry));

            Assert.AreSame(entry, registry.Get(42u));
            Assert.IsTrue(registry.TryGet(42u, out var resolved));
            Assert.AreSame(entry, resolved);
            Assert.IsTrue(registry.Contains(42u));
        }

        [Test]
        public void MissingHashResolvesToNull() {
            var registry = new ResourceRegistry<Entry>("Things", loader: LoaderFor(Make(1u)));

            Assert.IsNull(registry.Get(99u));
            Assert.IsFalse(registry.TryGet(99u, out _));
            Assert.IsFalse(registry.Contains(99u));
        }

        [Test]
        public void ResolvesByName() {
            var entry = Make(7u, "Vitality");
            var registry = new ResourceRegistry<Entry>("Things", e => e.EntryName, loader: LoaderFor(entry));

            Assert.AreSame(entry, registry.Get("Vitality"));
            Assert.AreEqual(7u, registry.GetHash("Vitality"));
            Assert.IsTrue(registry.TryGetHash("Vitality", out var hash));
            Assert.AreEqual(7u, hash);
            Assert.IsTrue(registry.IsRegistered("Vitality"));
            Assert.AreEqual("Vitality", registry.GetName(7u));
        }

        [Test]
        public void UnknownNameFailsWithoutThrowingOnTryPath() {
            var registry = new ResourceRegistry<Entry>("Things", e => e.EntryName, loader: LoaderFor(Make(7u, "Vitality")));

            Assert.IsNull(registry.Get("Strength"));
            Assert.IsFalse(registry.IsRegistered("Strength"));
            Assert.IsFalse(registry.TryGetHash("Strength", out var hash));
            Assert.AreEqual(0u, hash);
            Assert.Throws<KeyNotFoundException>(() => registry.GetHash("Strength"));
        }

        [Test]
        public void NameLookupWithoutSelectorThrows() {
            var registry = new ResourceRegistry<Entry>("Things", loader: LoaderFor(Make(7u, "Vitality")));

            Assert.Throws<InvalidOperationException>(() => registry.Get("Vitality"));
            Assert.Throws<InvalidOperationException>(() => registry.GetName(7u));
        }

        [Test]
        public void UnstampedEntryIsSkipped() {
            LogAssert.ignoreFailingMessages = true;
            var stamped = Make(5u);
            var registry = new ResourceRegistry<Entry>("Things", loader: LoaderFor(Make(0u), stamped));

            Assert.AreEqual(1, registry.Count);
            Assert.AreSame(stamped, registry.Get(5u));
        }

        [Test]
        public void NullEntryIsSkipped() {
            var registry = new ResourceRegistry<Entry>("Things", loader: _ => new[] { null, Make(5u) });

            Assert.AreEqual(1, registry.Count);
        }

        [Test]
        public void HashCollisionThrows() {
            var registry = new ResourceRegistry<Entry>(
                "Things", loader: LoaderFor(Make(3u, assetName: "First"), Make(3u, assetName: "Second")));

            var exception = Assert.Throws<InvalidOperationException>(() => registry.EnsureLoaded());

            StringAssert.Contains("First", exception.Message);
            StringAssert.Contains("Second", exception.Message);
        }

        [Test]
        public void DuplicateNameThrows() {
            var registry = new ResourceRegistry<Entry>(
                "Things",
                e => e.EntryName,
                loader: LoaderFor(Make(1u, "Vitality", "A"), Make(2u, "Vitality", "B")));

            var exception = Assert.Throws<InvalidOperationException>(() => registry.EnsureLoaded());

            StringAssert.Contains("Vitality", exception.Message);
        }

        [Test]
        public void EmptyNameThrows() {
            var registry = new ResourceRegistry<Entry>(
                "Things", e => e.EntryName, loader: LoaderFor(Make(1u, assetName: "Unnamed")));

            Assert.Throws<InvalidOperationException>(() => registry.EnsureLoaded());
        }

        [Test]
        public void ValidatorRunsForEveryEntry() {
            var validated = new List<uint>();
            var registry = new ResourceRegistry<Entry>(
                "Things", validateEntry: e => validated.Add(e.Hash), loader: LoaderFor(Make(1u), Make(2u)));

            registry.EnsureLoaded();

            CollectionAssert.AreEqual(new[] { 1u, 2u }, validated);
        }

        [Test]
        public void ValidatorFailureIsRetriedOnNextAccess() {
            var attempts = 0;
            var registry = new ResourceRegistry<Entry>(
                "Things",
                validateEntry: entry => {
                    if (entry.Hash == 2u)
                        throw new InvalidOperationException("bad entry");
                },
                loader: _ => {
                    attempts++;

                    return attempts == 1 ? new[] { Make(1u), Make(2u) } : new[] { Make(1u) };
                });

            Assert.Throws<InvalidOperationException>(() => registry.EnsureLoaded());
            Assert.AreEqual(1, registry.Count);
            Assert.AreEqual(2, attempts);
        }

        [Test]
        public void ResetClearsAndReloadsOnNextAccess() {
            var loads = 0;
            var registry = new ResourceRegistry<Entry>("Things", loader: _ => {
                loads++;

                return new[] { Make(1u) };
            });

            registry.EnsureLoaded();
            registry.Reset();

            Assert.AreEqual(1, loads);
            Assert.AreEqual(1, registry.Count);
            Assert.AreEqual(2, loads);
        }

        [Test]
        public void ReloadPicksUpNewEntries() {
            var loads = 0;
            var registry = new ResourceRegistry<Entry>("Things", e => e.EntryName, loader: _ => {
                loads++;

                return loads == 1
                        ? new[] { Make(1u, "Vitality") }
                        : new[] { Make(1u, "Vitality"), Make(2u, "Strength") };
            });

            Assert.AreEqual(1, registry.Count);

            registry.Reload();

            Assert.AreEqual(2, registry.Count);
            Assert.AreEqual(2u, registry.GetHash("Strength"));
        }

        [Test]
        public void EmptyFolderIsNotAnError() {
            var registry = new ResourceRegistry<Entry>("Things", loader: _ => Array.Empty<Entry>());

            Assert.AreEqual(0, registry.Count);
            Assert.IsEmpty(registry.All);
        }

        [Test]
        public void ImplementsRegistryContractForPacking() {
            var entry = Make(11u);
            IRegistry<Entry> registry = new ResourceRegistry<Entry>("Things", loader: LoaderFor(entry));

            Assert.AreEqual(1, registry.Count);
            Assert.AreSame(entry, registry.Get(11u));
        }
    }
}
