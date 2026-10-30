// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Packing {
    public readonly struct PackingLimits {
        public const int LargestStackBufferBytes = 64 * 1024;

        private const int DefaultStackBufferBytes = 4096;
        private const int DefaultFirstRentBytes = 8192;
        private const int DefaultMaximumPayloadBytes = 64 * 1024 * 1024;

        private readonly int _stackBufferBytes;
        private readonly int _firstRentBytes;
        private readonly int _maximumPayloadBytes;

        public static PackingLimits Default =>
            new(DefaultStackBufferBytes, DefaultFirstRentBytes, DefaultMaximumPayloadBytes);

        public int StackBufferBytes => _stackBufferBytes == 0 ? DefaultStackBufferBytes : _stackBufferBytes;

        public int FirstRentBytes => _firstRentBytes == 0 ? DefaultFirstRentBytes : _firstRentBytes;

        public int MaximumPayloadBytes =>
            _maximumPayloadBytes == 0 ? DefaultMaximumPayloadBytes : _maximumPayloadBytes;

        public PackingLimits(int stackBufferBytes, int firstRentBytes, int maximumPayloadBytes) {
            if (stackBufferBytes <= 0 || stackBufferBytes > LargestStackBufferBytes) {
                throw new ArgumentOutOfRangeException(nameof(stackBufferBytes),
                    "The stack buffer takes 1 to " + LargestStackBufferBytes + " bytes.");
            }

            if (firstRentBytes <= 0) {
                throw new ArgumentOutOfRangeException(nameof(firstRentBytes), "The first rent needs at least one byte.");
            }

            if (maximumPayloadBytes < stackBufferBytes || maximumPayloadBytes < firstRentBytes) {
                throw new ArgumentOutOfRangeException(nameof(maximumPayloadBytes),
                    "The largest payload cannot be smaller than the stack buffer or the first rent.");
            }

            _stackBufferBytes = stackBufferBytes;
            _firstRentBytes = firstRentBytes;
            _maximumPayloadBytes = maximumPayloadBytes;
        }
    }
}
