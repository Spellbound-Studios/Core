// Copyright (c) Spellbound Studios. All rights reserved.

namespace Spellbound.Core.Operations {
    public interface IResultRenderer {
        bool TryRender(object value, out string text);
    }
}
