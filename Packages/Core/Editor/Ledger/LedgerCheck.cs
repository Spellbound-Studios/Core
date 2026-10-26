// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using Spellbound.Core.Operations.Editor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Spellbound.Core.Ledger.Editor {
    public sealed class LedgerCheck {
        public const string ChangelogFileName = "CHANGELOG.md";
        public const string ChangelogIndent = "  ";

        private readonly string _editorVersion;
        private readonly Func<string, LedgerTypePresence> _typePresence;
        private readonly Func<string, string> _installedVersion;
        private readonly Func<string, string> _changelogText;

        public LedgerCheck(string editorVersion, Func<string, LedgerTypePresence> typePresence,
            Func<string, string> installedVersion, Func<string, string> changelogText) {
            _editorVersion = editorVersion ?? throw new ArgumentNullException(nameof(editorVersion));
            _typePresence = typePresence ?? throw new ArgumentNullException(nameof(typePresence));
            _installedVersion = installedVersion ?? throw new ArgumentNullException(nameof(installedVersion));
            _changelogText = changelogText ?? throw new ArgumentNullException(nameof(changelogText));
        }

        public static LedgerCheck ForThisEditor() {
            return new LedgerCheck(Application.unityVersion, TypePresenceOf, EditorInstalledPackages.VersionOf,
                ChangelogOf);
        }

        public LedgerCheckResult Run(IReadOnlyList<LedgerRow> rows, IReadOnlyList<string> problems, int sourceCount) {
            var results = new List<LedgerRowResult>();

            if (rows != null) {
                foreach (var row in rows) {
                    results.Add(Check(row));
                }
            }

            return new LedgerCheckResult(results, problems != null ? new List<string>(problems) : new List<string>(),
                sourceCount);
        }

        private LedgerRowResult Check(LedgerRow row) {
            var lines = new List<string>();
            var stale = false;
            var wrong = false;

            CheckType(row, lines, ref wrong);
            CheckEditor(row, lines, ref stale, ref wrong);

            foreach (var package in row.WatchPackages) {
                CheckPackage(row, package, lines, ref stale, ref wrong);
            }

            return new LedgerRowResult(row, stale, wrong, lines);
        }

        private void CheckType(LedgerRow row, List<string> lines, ref bool wrong) {
            if (row.OursType == null || row.Verdict == LedgerVerdict.Watch) {
                return;
            }

            var presence = _typePresence(row.OursType);

            if (presence == LedgerTypePresence.AssemblyNotLoaded) {
                lines.Add("type " + row.OursType + " is in an assembly not loaded here");
                return;
            }

            if (row.Verdict == LedgerVerdict.Moved && presence == LedgerTypePresence.Present) {
                lines.Add("type " + row.OursType + " still exists; a Moved row's type is deleted");
                wrong = true;
            }
            else if (row.Verdict != LedgerVerdict.Moved && presence == LedgerTypePresence.Absent) {
                lines.Add("type " + row.OursType + " no longer exists; re-judge or delete the row");
                wrong = true;
            }
        }

        private void CheckEditor(LedgerRow row, List<string> lines, ref bool stale, ref bool wrong) {
            var judgement = row.EditorJudgement;

            if (judgement == null) {
                lines.Add("no editor version in Judged at");
                wrong = true;
                return;
            }

            var current = LedgerStaleness.EditorMajorMinor(_editorVersion);
            var judged = judgement.Value.Version;
            var passed = LedgerStaleness.EditorVersionsBetween(judged, current);

            if (passed.Count == 0) {
                return;
            }

            lines.Add("editor " + string.Join(", ", passed) + " passed since " + judged + "; read "
                      + LedgerStaleness.DocumentationSearchAddress(current, row.WatchWords));
            stale = true;
        }

        private void CheckPackage(LedgerRow row, string package, List<string> lines, ref bool stale, ref bool wrong) {
            if (!row.TryJudgementFor(package, out var judgement)) {
                lines.Add("watch package " + package + " has no judged version in Judged at");
                wrong = true;
                return;
            }

            var installed = _installedVersion(package);

            if (installed == null) {
                lines.Add("watch package " + package + " is not installed here");
                return;
            }

            if (PackageVersionRange.Compare(installed, judgement.Version) <= 0) {
                return;
            }

            stale = true;
            lines.Add("watch package " + package + " moved " + judgement.Version + " to " + installed);
            var changelog = _changelogText(package);

            if (changelog == null) {
                lines.Add(ChangelogIndent + "no " + ChangelogFileName + " to read");
                return;
            }

            var found = ChangelogScan.LinesBetween(changelog, judgement.Version, installed, row.WatchWords);

            if (found.Count == 0) {
                lines.Add(ChangelogIndent + "no changelog line carries a watch word");
                return;
            }

            foreach (var line in found) {
                lines.Add(ChangelogIndent + line);
            }
        }

        private static LedgerTypePresence TypePresenceOf(string fullName) {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies) {
                try {
                    if (assembly.GetType(fullName, false) != null) {
                        return LedgerTypePresence.Present;
                    }
                }
                catch (ArgumentException) {
                    return LedgerTypePresence.Absent;
                }
            }

            var prefix = AssemblyPrefixOf(fullName);

            foreach (var assembly in assemblies) {
                var name = assembly.GetName().Name;

                if (string.Equals(name, prefix, StringComparison.Ordinal)
                    || name.StartsWith(prefix + ".", StringComparison.Ordinal)) {
                    return LedgerTypePresence.Absent;
                }
            }

            return LedgerTypePresence.AssemblyNotLoaded;
        }

        private static string AssemblyPrefixOf(string fullName) {
            var parts = fullName.Split('.');
            return parts.Length >= 2 ? parts[0] + "." + parts[1] : parts[0];
        }

        private static string ChangelogOf(string packageName) {
            var path = PackageInfo.FindForPackageName(packageName)?.resolvedPath;

            if (string.IsNullOrEmpty(path)) {
                return null;
            }

            var changelog = Path.Combine(path, ChangelogFileName);
            return File.Exists(changelog) ? File.ReadAllText(changelog) : null;
        }
    }
}
