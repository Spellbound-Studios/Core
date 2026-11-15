// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using Spellbound.Core.Observations;

namespace Spellbound.Core.Authoring.Editor {
    [RegisteredDefinitionCheck("core.observation-duplicate-key", RunsWhileEditing = true)]
    public sealed class ObservationDuplicateKeyCheck : IDefinitionCheck<ObservationSettings> {
        public const string DuplicateKeyCode = "core.observation-duplicate-key";
        public const string KeyUndeclaredCode = "core.observation-key-undeclared";
        public const string DeclaredTwiceCode = "core.observation-declared-twice";

        private readonly Func<IReadOnlyList<ObservationDeclaration>> _declarations;

        public ObservationDuplicateKeyCheck() : this(CoreKit.Observations.Declared) { }

        public ObservationDuplicateKeyCheck(Func<IReadOnlyList<ObservationDeclaration>> declarations) {
            _declarations = declarations ?? throw new ArgumentNullException(nameof(declarations));
        }

        public void Check(ObservationSettings definition, ValidationReport report) {
            if (definition == null) {
                throw new ArgumentNullException(nameof(definition));
            }

            if (report == null) {
                throw new ArgumentNullException(nameof(report));
            }

            var declared = _declarations() ?? Array.Empty<ObservationDeclaration>();
            var byName = DeclarationsByName(declared);
            var listsByKey = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            foreach (var (list, text) in NamedKeys(definition)) {
                if (!ObservationKey.TryParse(text, out var key, out _)) {
                    continue;
                }

                if (listsByKey.TryGetValue(key.Name, out var lists)) {
                    if (SetsTheOtherCostDefault(lists, list)) {
                        lists.Add(list);
                        continue;
                    }

                    var where = lists.Contains(list)
                        ? "twice in " + list
                        : "in " + lists[0] + " and again in " + list;
                    report.Error(DuplicateKeyCode, "The key '" + key + "' is named " + where + ".", list,
                        "Keep one entry for '" + key + "'.");
                    continue;
                }

                listsByKey.Add(key.Name, new List<string> { list });

                if (!byName.ContainsKey(key.Name)) {
                    report.Warning(KeyUndeclaredCode, "No package declares the key '" + key + "'.", list,
                        "Correct the spelling, or declare '" + key + "' through CoreKit.Observations.");
                }
            }

            ReportDeclaredTwice(declared, report);
        }

        public IReadOnlyList<string> PreviewLines(ObservationSettings definition) {
            if (definition == null) {
                return Array.Empty<string>();
            }

            var byName = DeclarationsByName(_declarations() ?? Array.Empty<ObservationDeclaration>());
            var listed = new HashSet<string>(StringComparer.Ordinal);
            var lines = new List<string>();

            foreach (var (_, text) in NamedKeys(definition)) {
                var name = text ?? string.Empty;

                if (!listed.Add(name)) {
                    continue;
                }

                if (!ObservationKey.TryParse(name, out _, out _)) {
                    lines.Add("'" + name + "': not a key");
                }
                else if (byName.TryGetValue(name, out var declaration)) {
                    lines.Add(name + ": " + KindWord(declaration));
                }
                else {
                    lines.Add(name + ": undeclared");
                }
            }

            return lines;
        }

        private static bool SetsTheOtherCostDefault(List<string> lists, string list) {
            return lists.Count == 1 && !string.Equals(lists[0], list, StringComparison.Ordinal)
                && IsCostDefaultList(lists[0]) && IsCostDefaultList(list);
        }

        private static bool IsCostDefaultList(string list) {
            return string.Equals(list, nameof(ObservationSettings.budgets), StringComparison.Ordinal)
                || string.Equals(list, nameof(ObservationSettings.qualityLevels), StringComparison.Ordinal);
        }

        private static void ReportDeclaredTwice(IReadOnlyList<ObservationDeclaration> declared,
            ValidationReport report) {
            var firstByHash = new Dictionary<uint, ObservationDeclaration>();

            foreach (var declaration in declared) {
                if (declaration == null) {
                    continue;
                }

                if (!firstByHash.TryGetValue(declaration.Hash, out var first)) {
                    firstByHash.Add(declaration.Hash, declaration);
                    continue;
                }

                if (string.Equals(first.Key.Package, declaration.Key.Package, StringComparison.Ordinal)) {
                    continue;
                }

                report.Error(DeclaredTwiceCode,
                    "The packages '" + first.Key.Package + "' and '" + declaration.Key.Package
                    + "' both declare the key hash " + declaration.Hash.ToString(CultureInfo.InvariantCulture)
                    + ", as '" + first.Key + "' and '" + declaration.Key + "'.",
                    null,
                    "Rename one of the two keys; a recorded occurrence carries only the hash.");
            }
        }

        private static Dictionary<string, ObservationDeclaration> DeclarationsByName(
            IReadOnlyList<ObservationDeclaration> declared) {
            var byName = new Dictionary<string, ObservationDeclaration>(StringComparer.Ordinal);

            foreach (var declaration in declared) {
                if (declaration != null && !byName.ContainsKey(declaration.Key.Name)) {
                    byName.Add(declaration.Key.Name, declaration);
                }
            }

            return byName;
        }

        private static List<(string List, string Key)> NamedKeys(ObservationSettings definition) {
            var keys = new List<(string List, string Key)>();

            if (definition.budgets != null) {
                foreach (var entry in definition.budgets) {
                    keys.Add((nameof(definition.budgets), entry.key));
                }
            }

            if (definition.qualityLevels != null) {
                foreach (var entry in definition.qualityLevels) {
                    keys.Add((nameof(definition.qualityLevels), entry.key));
                }
            }

            if (definition.rateCaps != null) {
                foreach (var entry in definition.rateCaps) {
                    keys.Add((nameof(definition.rateCaps), entry.key));
                }
            }

            if (definition.guardPolicies != null) {
                foreach (var entry in definition.guardPolicies) {
                    keys.Add((nameof(definition.guardPolicies), entry.key));
                }
            }

            return keys;
        }

        private static string KindWord(ObservationDeclaration declaration) {
            return declaration.IsGuard ? "guard" : declaration.Kind.ToString().ToLowerInvariant();
        }
    }
}
