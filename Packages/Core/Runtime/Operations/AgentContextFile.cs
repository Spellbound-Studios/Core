// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;

namespace Spellbound.Core.Operations {
    public static class AgentContextFile {
        public const string FileName = "AGENTS.md";
        public const string PurposeHeading = "Purpose";
        public const string FrontDoorHeading = "Front door";
        public const string RulesHeading = "Rules";
        public const string NotForHeading = "Not for";
        public const string GapsHeading = "Gaps";
        public const string LedgerHeading = "Ledger";

        public static readonly IReadOnlyList<string> Headings = new[] {
            PurposeHeading, FrontDoorHeading, RulesHeading, NotForHeading, GapsHeading, LedgerHeading
        };

        public static string PathFor(string packagePath) {
            return string.IsNullOrEmpty(packagePath) ? null : Path.Combine(packagePath, FileName);
        }

        public static string Read(string packagePath) {
            var path = PathFor(packagePath);
            return path != null && File.Exists(path) ? File.ReadAllText(path) : null;
        }

        public static string Section(string text, string heading) {
            if (string.IsNullOrEmpty(text)) {
                return null;
            }

            var lines = text.Replace("\r\n", "\n").Split('\n');
            var wanted = "## " + heading;
            var collecting = false;
            var body = new List<string>();

            foreach (var line in lines) {
                if (collecting && line.StartsWith("## ", StringComparison.Ordinal)) {
                    break;
                }

                if (collecting) {
                    body.Add(line);
                    continue;
                }

                if (string.Equals(line.TrimEnd(), wanted, StringComparison.Ordinal)) {
                    collecting = true;
                }
            }

            return collecting ? string.Join("\n", body).Trim('\n') : null;
        }

        public static IReadOnlyList<string> MissingHeadings(string text) {
            var missing = new List<string>();

            foreach (var heading in Headings) {
                if (Section(text, heading) == null) {
                    missing.Add(heading);
                }
            }

            return missing;
        }

        public static string FrontDoor(string text) {
            var section = Section(text, FrontDoorHeading);

            if (section == null) {
                return null;
            }

            var start = section.IndexOf('`');

            if (start < 0) {
                return null;
            }

            var end = section.IndexOf('`', start + 1);
            return end < 0 ? null : section.Substring(start + 1, end - start - 1);
        }

    }
}
