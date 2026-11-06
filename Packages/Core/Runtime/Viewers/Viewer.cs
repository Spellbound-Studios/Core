// Copyright (c) Spellbound Studios. All rights reserved.

using UnityEngine;

namespace Spellbound.Core.Viewers {
    public readonly struct Viewer {
        public readonly ViewerId Id;
        public readonly Vector3 Position;
        public readonly float CollisionRadius;
        public readonly float DetailRadius;

        public Viewer(ViewerId id, Vector3 position, float collisionRadius, float detailRadius) {
            Id = id;
            Position = position;
            CollisionRadius = collisionRadius;
            DetailRadius = detailRadius;
        }

        internal Viewer At(Vector3 position) => new Viewer(Id, position, CollisionRadius, DetailRadius);
    }
}
