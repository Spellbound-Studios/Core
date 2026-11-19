// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using NUnit.Framework;
using Spellbound.Core.ReferenceFrames;
using UnityEngine;

namespace Spellbound.Core.Tests {
    public class ReferenceFramePoseTests {
        private sealed class MovingFrame : IReferenceFrame {
            public ReferenceFrameId Id { get; }
            public Pose WorldPose { get; set; }
            public Vector3 AngularVelocity => Vector3.zero;

            public MovingFrame(ulong id, Pose worldPose) {
                Id = new ReferenceFrameId(id);
                WorldPose = worldPose;
            }

            public Vector3 VelocityAt(Vector3 worldPoint) => Vector3.zero;
        }

        private static MovingFrame CreateFrame(ulong id) {
            return new MovingFrame(id, new Pose(new Vector3(5f, 2f, -3f), Quaternion.Euler(10f, 40f, 15f)));
        }

        [Test]
        public void FromWorldThenToWorldReturnsTheOriginalPose() {
            var frame = CreateFrame(7);
            var original = new Pose(new Vector3(1f, 2f, 3f), Quaternion.Euler(20f, 30f, 40f));
            var local = ReferenceFramePose.FromWorld(original, frame);
            var restored = local.ToWorld(frame);
            Assert.AreEqual(original.position.x, restored.position.x, 1e-4f);
            Assert.AreEqual(original.position.y, restored.position.y, 1e-4f);
            Assert.AreEqual(original.position.z, restored.position.z, 1e-4f);
            Assert.AreEqual(0f, Quaternion.Angle(original.rotation, restored.rotation), 1e-3f);
        }

        [Test]
        public void FromWorldRecordsTheFrameId() {
            var frame = CreateFrame(7);
            var local = ReferenceFramePose.FromWorld(new Pose(Vector3.zero, Quaternion.identity), frame);
            Assert.AreEqual(frame.Id, local.Frame);
        }

        [Test]
        public void MovingTheFrameMovesTheWorldPoseByTheSameAmount() {
            var frame = CreateFrame(7);
            var local = ReferenceFramePose.FromWorld(new Pose(new Vector3(1f, 2f, 3f), Quaternion.identity), frame);
            var before = local.ToWorld(frame);
            var framePose = frame.WorldPose;
            frame.WorldPose = new Pose(framePose.position + new Vector3(10f, 0f, 0f), framePose.rotation);
            var after = local.ToWorld(frame);
            Assert.AreEqual(10f, after.position.x - before.position.x, 1e-4f);
            Assert.AreEqual(0f, after.position.y - before.position.y, 1e-4f);
            Assert.AreEqual(0f, after.position.z - before.position.z, 1e-4f);
        }

        [Test]
        public void ToWorldWithAnotherFrameThrows() {
            var local = ReferenceFramePose.FromWorld(new Pose(Vector3.zero, Quaternion.identity), CreateFrame(7));
            Assert.Throws<ArgumentException>(() => local.ToWorld(CreateFrame(8)));
        }

        [Test]
        public void NullFrameThrows() {
            var pose = new Pose(Vector3.zero, Quaternion.identity);
            Assert.Throws<ArgumentNullException>(() => ReferenceFramePose.FromWorld(pose, null));
            Assert.Throws<ArgumentNullException>(() => ReferenceFramePose.InWorld(pose).ToWorld(null));
        }

        [Test]
        public void InWorldKeepsPositionAndRotation() {
            var pose = new Pose(new Vector3(1f, 2f, 3f), Quaternion.Euler(5f, 6f, 7f));
            var local = ReferenceFramePose.InWorld(pose);
            Assert.IsTrue(local.Frame.IsWorld);
            Assert.AreEqual(pose.position, local.Position);
            Assert.AreEqual(pose.rotation, local.Rotation);
        }
    }
}
