// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using Spellbound.Core.Operations;
using UnityEngine;

namespace Spellbound.Core.Ledger.Editor {
    public static class LedgerSources {
        public const string ProjectLedgerPattern = "*.md";

        public static IReadOnlyList<LedgerRow> All(OperationSettings settings, ICollection<string> problems) {
            var rows = new List<LedgerRow>();

            foreach (var package in InstalledPackages.All()) {
                var text = AgentContextFile.Read(package.Path);
                var section = LedgerSectionInPlace(text);

                if (section != null) {
                    rows.AddRange(LedgerTable.Parse(section, AgentContextFile.PathFor(package.Path), problems));
                }
            }

            foreach (var file in ProjectFiles(settings)) {
                rows.AddRange(LedgerTable.Parse(File.ReadAllText(file), file, problems));
            }

            return rows;
        }

        public static IReadOnlyList<string> SourceNames(OperationSettings settings) {
            var names = new List<string>();

            foreach (var package in InstalledPackages.All()) {
                if (LedgerSectionInPlace(AgentContextFile.Read(package.Path)) != null) {
                    names.Add(AgentContextFile.PathFor(package.Path));
                }
            }

            names.AddRange(ProjectFiles(settings));
            return names;
        }

        public static string ProjectFolder(OperationSettings settings) {
            var folder = settings != null && !string.IsNullOrWhiteSpace(settings.ledgerFolder)
                ? settings.ledgerFolder
                : OperationSettings.DefaultLedgerFolder;
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", folder));
        }

        private static List<string> ProjectFiles(OperationSettings settings) {
            var files = new List<string>();
            var folder = ProjectFolder(settings);

            if (!Directory.Exists(folder)) {
                return files;
            }

            files.AddRange(Directory.GetFiles(folder, ProjectLedgerPattern, SearchOption.TopDirectoryOnly));
            files.Sort((left, right) => string.CompareOrdinal(Path.GetFileName(left), Path.GetFileName(right)));
            return files;
        }

        private static string LedgerSectionInPlace(string text) {
            if (string.IsNullOrEmpty(text)) {
                return null;
            }

            var lines = text.Replace("\r\n", "\n").Split('\n');
            var wanted = "## " + AgentContextFile.LedgerHeading;
            var found = false;
            var collecting = false;

            for (var i = 0; i < lines.Length; i++) {
                if (collecting && lines[i].StartsWith("## ", StringComparison.Ordinal)) {
                    collecting = false;
                }

                if (!collecting && !found && string.Equals(lines[i].TrimEnd(), wanted, StringComparison.Ordinal)) {
                    found = true;
                    collecting = true;
                    lines[i] = string.Empty;
                    continue;
                }

                if (!collecting) {
                    lines[i] = string.Empty;
                }
            }

            return found ? string.Join("\n", lines) : null;
        }
    }
}
