// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Packing {
    public readonly struct DecodeLimits {
        private const int DefaultMaximumStringBytes = 64 * 1024;
        private const int DefaultMaximumByteRun = 1024 * 1024;
        private const int DefaultMaximumCount = 65536;

        private readonly int _maximumStringBytes;
        private readonly int _maximumByteRun;
        private readonly int _maximumCount;
        private readonly bool _set;

        public static DecodeLimits Default =>
            new(DefaultMaximumStringBytes, DefaultMaximumByteRun, DefaultMaximumCount);

        public int MaximumStringBytes => _set ? _maximumStringBytes : DefaultMaximumStringBytes;

        public int MaximumByteRun => _set ? _maximumByteRun : DefaultMaximumByteRun;

        public int MaximumCount => _set ? _maximumCount : DefaultMaximumCount;

        public DecodeLimits(int maximumStringBytes, int maximumByteRun, int maximumCount) {
            if (maximumStringBytes < 0) {
                throw new ArgumentOutOfRangeException(nameof(maximumStringBytes));
            }

            if (maximumByteRun < 0) {
                throw new ArgumentOutOfRangeException(nameof(maximumByteRun));
            }

            if (maximumCount < 0) {
                throw new ArgumentOutOfRangeException(nameof(maximumCount));
            }

            _maximumStringBytes = maximumStringBytes;
            _maximumByteRun = maximumByteRun;
            _maximumCount = maximumCount;
            _set = true;
        }
    }
}
