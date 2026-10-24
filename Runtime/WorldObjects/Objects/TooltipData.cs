// Copyright 2026 Spellbound Studio Inc.

using UnityEngine;

namespace Spellbound.Core.Objects {
    public struct TooltipData {
        public readonly string TooltipText;
        public Vector2 OffsetFromTarget;
        public readonly TooltipSource Source;
        public readonly bool RequiresCursorUnlocked;

        public TooltipData(
            string tooltipText,
            Vector2 offset,
            TooltipSource source,
            bool requiresCursorUnlocked
        ) {
            TooltipText = tooltipText;
            OffsetFromTarget = offset;
            Source = source;
            RequiresCursorUnlocked = requiresCursorUnlocked;
        }
    }
}