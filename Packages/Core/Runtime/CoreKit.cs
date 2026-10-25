// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using Spellbound.Core.Operations;
using Spellbound.Core.Packing;
using UnityEngine;

namespace Spellbound.Core {
    public static class CoreKit {
        private static OperationHost _operations;

        public static OperationHost Operations => _operations ??= new OperationHost();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlaySession() {
            _operations = null;
        }

        public static class Packing {
            public static string Describe(Type type) => PackedLayoutDescription.Describe(type);
        }
    }
}
