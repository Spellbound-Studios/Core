// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using UnityEngine.Scripting;

namespace Spellbound.Core.Operations {
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ResultRendererAttribute : PreserveAttribute { }
}
