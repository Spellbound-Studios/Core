// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using UnityEngine;

namespace Spellbound.Core.ReferenceFrames {
    public struct ReferenceFramePose {
        public ReferenceFrameId Frame;
        public Vector3 Position;
        public Quaternion Rotation;

        public static ReferenceFramePose FromWorld(in Pose worldPose, IReferenceFrame frame) {
            if (frame == null) {
                throw new ArgumentNullException(nameof(frame));
            }

            var framePose = frame.WorldPose;
            var inverseRotation = Quaternion.Inverse(framePose.rotation);
            return new ReferenceFramePose {
                Frame = frame.Id,
                Position = inverseRotation * (worldPose.position - framePose.position),
                Rotation = inverseRotation * worldPose.rotation
            };
        }

        public static ReferenceFramePose InWorld(in Pose worldPose) {
            return new ReferenceFramePose {
                Frame = ReferenceFrameId.World,
                Position = worldPose.position,
                Rotation = worldPose.rotation
            };
        }

        public Pose ToWorld(IReferenceFrame frame) {
            if (frame == null) {
                throw new ArgumentNullException(nameof(frame));
            }

            if (frame.Id != Frame) {
                throw new ArgumentException(
                    "Frame " + frame.Id + " does not match the pose frame " + Frame + ".", nameof(frame));
            }

            var framePose = frame.WorldPose;
            return new Pose(framePose.position + framePose.rotation * Position, framePose.rotation * Rotation);
        }
    }
}
