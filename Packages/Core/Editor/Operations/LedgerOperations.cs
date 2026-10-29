// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections.Generic;
using Spellbound.Core.UnityLedger;

namespace Spellbound.Core.Operations.Editor {
    public static class LedgerOperations {
        private const string UnityLedgerDescription =
            "Checks every ledger of Unity systems the installed packages and this project rest on: a row whose type "
            + "contradicts its verdict is wrong, a row whose editor or watched package moved past its judgement is "
            + "stale, with the changelog lines that carry a watch word.";

        [Operation("unity-ledger", UnityLedgerDescription, OperationEffect.Reads, Audience = OperationAudiences.Anyone)]
        public static OperationResult UnityLedger(OperationContext context,
            [Argument("True to print the lines for the Move issue form instead of the report.")]
            bool moveForm = false) {
            var settings = context.Host.Settings;
            var problems = new List<string>();
            var rows = LedgerSources.All(settings, problems);
            var sourceCount = LedgerSources.SourceNames(settings).Count;
            var result = LedgerCheck.ForThisEditor().Run(rows, problems, sourceCount);
            var text = result.ToText();

            if (moveForm) {
                var lines = result.MoveFormLines();
                text = lines.Length > 0 ? lines : result.Summary();
            }

            return result.Passed ? OperationResult.Success(text, result) : OperationResult.Failed(text);
        }
    }
}
