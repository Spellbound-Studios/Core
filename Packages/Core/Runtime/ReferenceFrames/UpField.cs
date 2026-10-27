// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.ReferenceFrames {
    public readonly struct UpField {
        public readonly UpFieldForm Form;
        public readonly float X;
        public readonly float Y;
        public readonly float Z;
        public readonly UpGrid Grid;
        public readonly UpDirection Outside;

        private UpField(UpFieldForm form, float x, float y, float z, UpGrid grid, UpDirection outside) {
            Form = form;
            X = x;
            Y = y;
            Z = z;
            Grid = grid;
            Outside = outside;
        }

        public bool IsConstant => Form == UpFieldForm.Constant;

        public static UpField WorldUp => Constant(0f, 1f, 0f);

        public static UpField Constant(float x, float y, float z) {
            var length = Math.Sqrt((double)x * x + (double)y * y + (double)z * z);

            if (!(length > 0.0) || double.IsInfinity(length)) {
                throw new ArgumentException("A constant up needs a nonzero, finite direction.");
            }

            return new UpField(UpFieldForm.Constant, (float)(x / length), (float)(y / length), (float)(z / length),
                default, UpDirection.WorldUp);
        }

        public static UpField RadialFrom(float centreX, float centreY, float centreZ) {
            return new UpField(UpFieldForm.Radial, centreX, centreY, centreZ, default, UpDirection.WorldUp);
        }

        public static UpField Sampled(in UpGrid grid, UpDirection outside) {
            if (!grid.Directions.IsCreated) {
                throw new ArgumentException("A sampled up field needs a grid with its directions.", nameof(grid));
            }

            return new UpField(UpFieldForm.Sampled, 0f, 0f, 0f, grid, outside);
        }

        public void Evaluate(float x, float y, float z, out float upX, out float upY, out float upZ) {
            switch (Form) {
                case UpFieldForm.Constant:
                    upX = X;
                    upY = Y;
                    upZ = Z;
                    return;
                case UpFieldForm.Radial:
                    EvaluateRadial(x, y, z, out upX, out upY, out upZ);
                    return;
                case UpFieldForm.Sampled:
                    EvaluateSampled(x, y, z, out upX, out upY, out upZ);
                    return;
                default:
                    upX = 0f;
                    upY = 1f;
                    upZ = 0f;
                    return;
            }
        }

        private void EvaluateRadial(float x, float y, float z, out float upX, out float upY, out float upZ) {
            var offsetX = (double)x - X;
            var offsetY = (double)y - Y;
            var offsetZ = (double)z - Z;
            var length = Math.Sqrt(offsetX * offsetX + offsetY * offsetY + offsetZ * offsetZ);

            if (!(length > 0.0)) {
                upX = 0f;
                upY = 1f;
                upZ = 0f;
                return;
            }

            upX = (float)(offsetX / length);
            upY = (float)(offsetY / length);
            upZ = (float)(offsetZ / length);
        }

        private void EvaluateSampled(float x, float y, float z, out float upX, out float upY, out float upZ) {
            var direction = Grid.TryIndexOf(x, y, z, out var index) ? Grid.Directions[index] : Outside;
            upX = direction.X;
            upY = direction.Y;
            upZ = direction.Z;
        }
    }
}
