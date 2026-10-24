// Copyright 2026 Spellbound Studio Inc.

using System;
using Spellbound.Core.Packing;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Spellbound.Core {
    /// <summary>
    /// IPacker transform type. Allows wiring and persistence for transform components.
    /// </summary>
    public struct TransformData : IPacker {
        public Vector3 Position;
        public Vector3 Rotation;
        public float Scale;

        #region Constructors

        // Default Constructor
        public TransformData(Vector3 position, Vector3 rotation, float scale) {
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }

        // Constructor from ECS transform
        public TransformData(LocalTransform localTransform) {
            Position = localTransform.Position;
            Rotation = math.degrees(math.EulerXYZ(localTransform.Rotation));
            Scale = localTransform.Scale;
        }

        // Constructor from GameObject Transform
        public TransformData(Transform transform) {
            Position = transform.position;
            Rotation = math.degrees(math.EulerXYZ(transform.rotation));
            // TODO: Tinker it looks like we are silently dropping non-uniform scale please investigate
            Scale = transform.localScale.x;
        }

        #endregion

        #region HelperReadMethdods

        public readonly LocalTransform ToLocalTransform() =>
                new() {
                    Position = Position,
                    Rotation = quaternion.EulerXYZ(math.radians(Rotation)),
                    Scale = Scale
                };

        public Quaternion RotAsQuaternion() => quaternion.EulerXYZ(math.radians(Rotation));

        public Vector3 ScaleAsVector3() => new(Scale, Scale, Scale);

        #endregion

        #region Packing

        public void Pack(ref Span<byte> buffer) {
            Packer.WriteVector3(ref buffer, Position);
            Packer.WriteVector3(ref buffer, Rotation);
            Packer.WriteFloat(ref buffer, Scale);
        }

        public void Unpack(ref ReadOnlySpan<byte> buffer) {
            Position = Packer.ReadVector3(ref buffer);
            Rotation = Packer.ReadVector3(ref buffer);
            Scale = Packer.ReadFloat(ref buffer);
        }

        #endregion
    }
}