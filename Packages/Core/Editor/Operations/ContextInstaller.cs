// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.IO;
using Spellbound.Core.Operations;
using UnityEngine;

namespace Spellbound.Core.Operations.Editor {
    public static class ContextInstaller {
        public const string BlockName = "context";
        public const string AgentsFileName = "AGENTS.md";
        public const string ClaudeFileName = "CLAUDE.md";

        public static readonly string SkillRelativePath = Path.Combine(".claude", "skills", AgentContextStub.SkillName, "SKILL.md");

        public static string ProjectRoot() {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        public static string RelativePath(ContextInstallTarget target) {
            switch (target) {
                case ContextInstallTarget.Skill:
                    return SkillRelativePath;
                case ContextInstallTarget.Agents:
                    return AgentsFileName;
                case ContextInstallTarget.Claude:
                    return ClaudeFileName;
                default:
                    throw new ArgumentOutOfRangeException(nameof(target), target, null);
            }
        }

        public static string Install(ContextInstallTarget target, string projectRoot, out bool created) {
            var path = Path.Combine(projectRoot, RelativePath(target));
            created = !File.Exists(path);
            var directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directory)) {
                Directory.CreateDirectory(directory);
            }

            if (target == ContextInstallTarget.Skill) {
                File.WriteAllText(path, AgentContextStub.SkillFile());
                return path;
            }

            var existing = created ? string.Empty : File.ReadAllText(path);
            File.WriteAllText(path, GeneratedBlock.Replace(existing, BlockName, AgentContextStub.BlockBody()));
            return path;
        }
    }
}
