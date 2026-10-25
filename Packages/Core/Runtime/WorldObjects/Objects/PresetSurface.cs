// Copyright 2026 Spellbound Studio Inc.

using System;
using System.Collections.Generic;
using Spellbound.Core.Modules;
using UnityEngine;

namespace Spellbound.Core.Objects {
    /// <summary>
    /// A surface defines the specifics of an object preset. It contains PresetModules that help shape and define how
    /// this surface behaves.
    /// </summary>
    [Serializable]
    public class PresetSurface {
        public string surfaceName;

        [SerializeReference] public List<PresetModule> presetModules;
    }
}