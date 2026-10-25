// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;
using Spellbound.Core.Operations;

namespace Spellbound.Core.Operations.Editor {
    public static class CoreEditorOperations {
        [Operation("context", "Lists the installed Spellbound packages with their front doors and operations, or prints one package's agent context with its generated blocks refreshed.",
            OperationEffect.Reads, Audience = OperationAudiences.Anyone)]
        public static string Context(OperationContext context,
            [Argument("The operation group or package id to print in full; leave it out to list every package.")] string package = null) {
            var catalog = context.Host.Catalog;
            var packages = InstalledPackages.All();
            var doors = BridgeDoors.Find();

            if (!string.IsNullOrWhiteSpace(package)) {
                return OnePackage(catalog, packages, doors, package.Trim());
            }

            var builder = new StringBuilder();
            builder.Append(packages.Count).Append(" Spellbound packages installed");

            foreach (var installed in packages) {
                AppendSummary(builder, catalog, doors, installed, null);
            }

            foreach (var group in catalog.Groups) {
                var owner = PackageOf(group);

                if (owner == null) {
                    builder.Append('\n').Append("group ").Append(group.Name).Append(": ").Append(group.Operations.Count)
                        .Append(" operations in ").Append(string.Join(", ", group.Assemblies)).Append(", no package");
                }
                else if (!Contains(packages, owner)) {
                    AppendSummary(builder, catalog, doors, owner, " (a package folder under Assets, not installed through the package manager)");
                }
            }

            builder.Append("\nRun: core context --package <group>");
            return builder.ToString();
        }

        private static void AppendSummary(StringBuilder builder, OperationCatalog catalog, IReadOnlyList<BridgeDoor> doors,
            InstalledPackageDescription package, string note) {
            var group = GroupOf(catalog, package);
            var text = AgentContextFile.Read(package.Path);
            var frontDoor = AgentContextFile.FrontDoor(text);
            builder.Append('\n').Append(package.Line).Append(note).Append(": ");
            builder.Append(group != null ? "group " + group.Name + ", " + group.Operations.Count + " operations" : "no operations");
            builder.Append(", front door ").Append(frontDoor != null ? "`" + frontDoor + "`" : "not named");

            foreach (var door in DoorsOf(doors, package)) {
                builder.Append(", bridge door `").Append(door.TypeName).Append('`');
            }

            builder.Append(", context ").Append(text != null ? AgentContextFile.PathFor(package.Path) : "none");
        }

        private static bool Contains(IReadOnlyList<InstalledPackageDescription> packages, InstalledPackageDescription package) {
            foreach (var candidate in packages) {
                if (string.Equals(candidate.Name, package.Name, StringComparison.Ordinal)) {
                    return true;
                }
            }

            return false;
        }

        [Operation("context-install", "Writes the stub that tells an agent how to reach the installed packages: a skill file, or a block in the project's AGENTS.md or CLAUDE.md.",
            OperationEffect.Writes)]
        public static string ContextInstall([Argument("Skill writes .claude/skills/spellbound/SKILL.md; Agents and Claude write one delimited block into AGENTS.md or CLAUDE.md at the project root.")] ContextInstallTarget target) {
            var path = ContextInstaller.Install(target, ContextInstaller.ProjectRoot(), out var created);
            return (created ? "wrote " : "updated ") + path;
        }

        [Operation("ledger", "Lists every installed Spellbound package's Ledger rows: the Unity types it builds on and what moved out of it.",
            OperationEffect.Reads, Audience = OperationAudiences.Anyone)]
        public static OperationTable Ledger() {
            var table = new OperationTable("package", "row");

            foreach (var installed in InstalledPackages.All()) {
                var text = AgentContextFile.Read(installed.Path);

                if (text == null) {
                    table.AddRow(installed.Name, "no " + AgentContextFile.FileName);
                    continue;
                }

                var rows = AgentContextFile.LedgerRows(text);

                if (rows == null) {
                    table.AddRow(installed.Name, "no " + AgentContextFile.LedgerHeading + " heading");
                    continue;
                }

                if (rows.Count == 0) {
                    table.AddRow(installed.Name, "no rows");
                }

                foreach (var row in rows) {
                    table.AddRow(installed.Name, row);
                }
            }

            return table;
        }

        public static string Refresh(string text, OperationGroupDescription group, InstalledPackageDescription package, IReadOnlyList<BridgeDoor> doors) {
            var refreshed = text ?? string.Empty;

            if (group != null) {
                refreshed = GeneratedBlock.InsertUnderHeading(refreshed, AgentContextFile.FrontDoorHeading, AgentContextBlocks.OperationsBlock,
                    AgentContextBlocks.Operations(group, package?.Line));
            }

            if (doors != null && doors.Count > 0) {
                refreshed = GeneratedBlock.InsertUnderHeading(refreshed, AgentContextFile.FrontDoorHeading, AgentContextBlocks.DoorsBlock,
                    AgentContextBlocks.Doors(doors));
            }

            return refreshed;
        }

        private static string OnePackage(OperationCatalog catalog, IReadOnlyList<InstalledPackageDescription> packages, IReadOnlyList<BridgeDoor> doors,
            string wanted) {
            var group = catalog.Group(wanted);
            var package = group != null ? PackageOf(group) : InstalledPackages.Find(wanted) ?? FolderPackageNamed(catalog, wanted);

            if (group == null && package == null) {
                throw new OperationException("no group or package '" + wanted + "'", "Run: core context");
            }

            group ??= GroupOf(catalog, package);
            var packageDoors = package != null ? DoorsOf(doors, package) : new List<BridgeDoor>();

            if (package == null) {
                return "no package holds the group " + group.Name + "\n" + Refresh(string.Empty, group, null, packageDoors);
            }

            var path = AgentContextFile.PathFor(package.Path);
            var text = AgentContextFile.Read(package.Path);

            if (text == null) {
                return "no " + AgentContextFile.FileName + " at " + path + "\n" + Refresh(string.Empty, group, package, packageDoors);
            }

            return path + "\n" + Refresh(text, group, package, packageDoors).TrimEnd('\r', '\n');
        }

        private static InstalledPackageDescription FolderPackageNamed(OperationCatalog catalog, string wanted) {
            foreach (var group in catalog.Groups) {
                var owner = PackageOf(group);

                if (owner != null && (string.Equals(owner.Name, wanted, StringComparison.OrdinalIgnoreCase)
                                      || string.Equals(owner.DisplayName, wanted, StringComparison.OrdinalIgnoreCase))) {
                    return owner;
                }
            }

            return null;
        }

        private static OperationGroupDescription GroupOf(OperationCatalog catalog, InstalledPackageDescription package) {
            if (package == null) {
                return null;
            }

            foreach (var group in catalog.Groups) {
                var owner = PackageOf(group);

                if (owner != null && string.Equals(owner.Name, package.Name, StringComparison.Ordinal)) {
                    return group;
                }
            }

            return null;
        }

        private static InstalledPackageDescription PackageOf(OperationGroupDescription group) {
            foreach (var assemblyName in group.Assemblies) {
                var package = EditorInstalledPackages.ForAssemblyName(assemblyName);

                if (package != null) {
                    return package;
                }
            }

            return null;
        }

        private static List<BridgeDoor> DoorsOf(IReadOnlyList<BridgeDoor> doors, InstalledPackageDescription package) {
            var owned = new List<BridgeDoor>();

            foreach (var door in doors) {
                var owner = EditorInstalledPackages.ForAssemblyName(door.AssemblyName);

                if (owner != null && string.Equals(owner.Name, package.Name, StringComparison.Ordinal)) {
                    owned.Add(door);
                }
            }

            return owned;
        }
    }
}
