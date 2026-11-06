// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using Unity.Collections;

namespace Spellbound.Core.ReferenceFrames {
    public readonly struct UpGrid {
        public readonly NativeArray<UpDirection> Directions;
        public readonly float OriginX;
        public readonly float OriginY;
        public readonly float OriginZ;
        public readonly float CellSize;
        public readonly int CountX;
        public readonly int CountY;
        public readonly int CountZ;

        public UpGrid(NativeArray<UpDirection> directions, float originX, float originY, float originZ, float cellSize,
            int countX, int countY, int countZ) {
            if (countX < 1 || countY < 1 || countZ < 1) {
                throw new ArgumentException("A grid has at least one cell on each axis.");
            }

            if (!(cellSize > 0f)) {
                throw new ArgumentException("A grid's cell size is positive.", nameof(cellSize));
            }

            if (directions.Length != (long)countX * countY * countZ) {
                throw new ArgumentException("The grid holds " + directions.Length + " directions for "
                                            + countX + " by " + countY + " by " + countZ + " cells.",
                    nameof(directions));
            }

            Directions = directions;
            OriginX = originX;
            OriginY = originY;
            OriginZ = originZ;
            CellSize = cellSize;
            CountX = countX;
            CountY = countY;
            CountZ = countZ;
        }

        public int IndexOf(int x, int y, int z) {
            return x + CountX * (y + CountY * z);
        }

        public bool TryIndexOf(float worldX, float worldY, float worldZ, out int index) {
            var cellX = Math.Floor((worldX - OriginX) / (double)CellSize);
            var cellY = Math.Floor((worldY - OriginY) / (double)CellSize);
            var cellZ = Math.Floor((worldZ - OriginZ) / (double)CellSize);

            if (!(cellX >= 0 && cellX < CountX && cellY >= 0 && cellY < CountY && cellZ >= 0 && cellZ < CountZ)) {
                index = -1;
                return false;
            }

            index = IndexOf((int)cellX, (int)cellY, (int)cellZ);
            return true;
        }
    }
}
