// Copyright (c) Spellbound Studios. All rights reserved.

using NUnit.Framework;
using Spellbound.Core.ReferenceFrames;
using UnityEngine;

namespace Spellbound.Core.Tests {
    public class ReferenceFrameDirectoryTests {
        private sealed class StubFrame : IReferenceFrame {
            public ReferenceFrameId Id { get; }
            public Pose WorldPose => new Pose(Vector3.zero, Quaternion.identity);
            public Vector3 AngularVelocity => Vector3.zero;

            public StubFrame(ulong id) {
                Id = new ReferenceFrameId(id);
            }

            public Vector3 VelocityAt(Vector3 worldPoint) => Vector3.zero;
        }

        [Test]
        public void WorldIdAnswersTheWorldFrame() {
            var directory = new ReferenceFrameDirectory();
            Assert.IsTrue(directory.TryGet(ReferenceFrameId.World, out var frame));
            Assert.AreSame(ReferenceFrameDirectory.World, frame);
            Assert.IsTrue(directory.Contains(ReferenceFrameId.World));
        }

        [Test]
        public void RegisteredFrameIsReturnedAndCounted() {
            var directory = new ReferenceFrameDirectory();
            var registered = new StubFrame(5);
            Assert.IsTrue(directory.TryRegister(registered));
            Assert.IsTrue(directory.TryGet(registered.Id, out var found));
            Assert.AreSame(registered, found);
            Assert.AreEqual(1, directory.Count);
            Assert.IsTrue(directory.Contains(registered.Id));
        }

        [Test]
        public void DuplicateIdIsRejected() {
            var directory = new ReferenceFrameDirectory();
            Assert.IsTrue(directory.TryRegister(new StubFrame(5)));
            Assert.IsFalse(directory.TryRegister(new StubFrame(5)));
            Assert.AreEqual(1, directory.Count);
        }

        [Test]
        public void WorldIdFrameIsRejected() {
            var directory = new ReferenceFrameDirectory();
            Assert.IsFalse(directory.TryRegister(new StubFrame(0)));
            Assert.AreEqual(0, directory.Count);
        }

        [Test]
        public void NullFrameIsRejected() {
            Assert.IsFalse(new ReferenceFrameDirectory().TryRegister(null));
        }

        [Test]
        public void UnregisterRemovesTheFrame() {
            var directory = new ReferenceFrameDirectory();
            var registered = new StubFrame(5);
            directory.TryRegister(registered);
            Assert.IsTrue(directory.Unregister(registered.Id));
            Assert.IsFalse(directory.TryGet(registered.Id, out var found));
            Assert.IsNull(found);
        }

        [Test]
        public void UnregisterOfWorldOrUnknownIsFalse() {
            var directory = new ReferenceFrameDirectory();
            Assert.IsFalse(directory.Unregister(ReferenceFrameId.World));
            Assert.IsFalse(directory.Unregister(new ReferenceFrameId(99)));
        }
    }
}
