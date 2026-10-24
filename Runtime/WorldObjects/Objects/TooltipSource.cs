// Copyright 2026 Spellbound Studio Inc.

namespace Spellbound.Core.Objects {
    /// <summary>
    /// Categorises what kind of thing is being tooltipped so multiple tooltip views can subscribe to the
    /// same event bus and self-filter. Values name the domain (world object, inventory item, talent), not
    /// the rendering technology — every value here is "UI" in the literal sense; the discriminator is
    /// which SYSTEM owns the source.
    /// </summary>
    public enum TooltipSource {
        None,
        World,
        Inventory,
        Talent
    }
}
