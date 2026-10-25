// Copyright 2026 Spellbound Studio Inc.

using NUnit.Framework;
using Spellbound.Core.ObjectData;
using Spellbound.Core.Packing;
using UnityEngine;

namespace Spellbound.Core.Tests {
    public class DynamicInstanceEntryTests {
        [Test]
        public void RoundTripsWithPopulatedDataSlots() {
            var entry = new DynamicInstanceEntry(
                0xABCD1234u,
                new TransformData(new Vector3(1f, 2f, 3f), new Vector3(10f, 20f, 30f), 2f),
                true);

            entry.DataSlots[new InstanceDataKey(111u, 0)] = new byte[] { 1, 2, 3 };
            entry.DataSlots[new InstanceDataKey(222u, 5)] = new byte[] { 9, 8, 7, 6 };

            var result = Packer.FromBytes<DynamicInstanceEntry>(Packer.ToBytes(entry));

            Assert.AreEqual(entry.PresetHash, result.PresetHash);
            Assert.AreEqual(entry.WasMovingAtSave, result.WasMovingAtSave);
            Assert.AreEqual(entry.Transform.Position, result.Transform.Position);
            Assert.AreEqual(entry.Transform.Rotation, result.Transform.Rotation);
            Assert.AreEqual(entry.Transform.Scale, result.Transform.Scale);

            Assert.AreEqual(2, result.DataSlots.Count);
            Assert.AreEqual(new byte[] { 1, 2, 3 }, result.DataSlots[new InstanceDataKey(111u, 0)]);
            Assert.AreEqual(new byte[] { 9, 8, 7, 6 }, result.DataSlots[new InstanceDataKey(222u, 5)]);
        }

        [Test]
        public void RoundTripsWithEmptyDataSlots() {
            var entry = new DynamicInstanceEntry(
                42u,
                new TransformData(Vector3.zero, Vector3.zero, 1f),
                false);

            var result = Packer.FromBytes<DynamicInstanceEntry>(Packer.ToBytes(entry));

            Assert.AreEqual(42u, result.PresetHash);
            Assert.IsFalse(result.WasMovingAtSave);
            Assert.IsEmpty(result.DataSlots);
        }
    }
}
