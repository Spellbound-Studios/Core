// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using Spellbound.Core.Packing;

namespace Spellbound.Core {
    public static class CoreKit {
        public static class Packing {
            public static string Describe(Type type) => PackedLayoutDescription.Describe(type);
        }
    }
}
