// Copyright (c) Spellbound Studios. All rights reserved.

using Spellbound.Core.Operations;

namespace Spellbound.Core.Tests.OperationFixture {
    [ResultRenderer]
    public sealed class FixtureBadgeRenderer : IResultRenderer {
        public bool TryRender(object value, out string text) {
            if (value is FixtureBadge badge) {
                text = "badge of " + badge.Metal;
                return true;
            }

            text = null;
            return false;
        }
    }
}
