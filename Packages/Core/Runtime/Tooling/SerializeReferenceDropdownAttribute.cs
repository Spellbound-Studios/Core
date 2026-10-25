// Copyright 2026 Spellbound Studio Inc.

using UnityEngine;

namespace Spellbound.Core.Tooling {
    /// <summary>
    /// Draws a SerializeReference field as a concrete-type dropdown with the chosen instance's fields
    /// below it. Apply alongside SerializeReference; on a list it applies to each element.
    /// </summary>
    public sealed class SerializeReferenceDropdownAttribute : PropertyAttribute { }
}
