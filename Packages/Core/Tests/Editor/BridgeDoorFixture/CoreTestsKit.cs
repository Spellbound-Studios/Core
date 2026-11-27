// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections.Generic;

namespace Spellbound.Core.Tests.BridgeDoorFixture {
    public static class CoreTestsKit {
        public static int Count { get; private set; }

        public static List<string> Names(string prefix, int count = 3) {
            var names = new List<string>();

            for (var i = 0; i < count; i++) {
                names.Add(prefix + i);
            }

            Count = names.Count;
            return names;
        }

        public static bool TryBuild(float size, out CoreTestsPart part) {
            part = new CoreTestsPart(size);
            return size > 0;
        }
    }
}
