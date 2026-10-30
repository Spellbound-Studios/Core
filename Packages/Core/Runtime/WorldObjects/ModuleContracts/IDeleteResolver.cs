// Copyright 2026 Spellbound Studio Inc.

using System.Collections.Generic;
using System.Threading.Tasks;
using Spellbound.Core.ObjectData;
using Spellbound.Core.ObjectHandling;
using Spellbound.Core.Objects;
using Spellbound.Core.Surfaces;

namespace Spellbound.Core.ModuleContracts {
    public interface IDeleteResolver {
        /// <summary>
        /// For ObjectPreset PresetModules
        /// Interface Contract for a Module to respond to changes with major consequences
        /// </summary>
        void ResolveDelete(Dictionary<InstanceDataKey, byte[]> dataSlots, TransformData transformData, ObjectParent parent = null, int instanceIndex = -1);
        
        Task<bool> ResolveDelete(IEventSurface eventSurface, IAwardReciever awardReciever);
    }
}