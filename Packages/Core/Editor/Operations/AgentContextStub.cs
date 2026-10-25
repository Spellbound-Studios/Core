// Copyright (c) Spellbound Studios. All rights reserved.

namespace Spellbound.Core.Operations.Editor {
    public static class AgentContextStub {
        public const string SkillName = "spellbound";
        public const string Title = "Spellbound packages in this project";

        public const string SkillFrontmatter =
            "---\n" +
            "name: " + SkillName + "\n" +
            "description: Use when a Unity project has Spellbound packages installed and the task touches them: find out what they offer, author or change their content, test it. Not for editing the packages.\n" +
            "---\n";

        public const string Body =
            "Everything the installed packages can do is an operation. Run one with the Unity CLI:\n" +
            "\n" +
            "    unity command --query spellbound\n" +
            "\n" +
            "The command it finds takes one operation line. If the search finds nothing, read the console with: unity command console --level warning\n" +
            "Core says there why the Spellbound command is off.\n" +
            "\n" +
            "Operation lines (give each to that command):\n" +
            "\n" +
            "1. core help lists the installed groups and what each is for.\n" +
            "2. core context --package <group> prints that package's rules, front door and operations for the installed version. Read it before using the package.\n" +
            "3. Make content as assets in the drafts folder, then run core validate --drafts and core preview. To change content a person already approved, run core revise on it and edit the revision. When core validate reports core.unknown-key, copy the stored value it shows into the new field first and save nothing else on that asset before it. create_asset and set_serialized_field write plain fields only; a definition that holds a list of references to different types is made with run_script or the package front door.\n" +
            "4. Never edit files inside a package. If a package cannot do the task, say what is missing and stop.\n" +
            "5. You cannot approve drafts. Report the validation, the preview and the test, and ask a person.\n" +
            "6. To read a running build, ask a person first: the build's server is off by default, needs a development player for a standalone target, and answers this machine only.";

        public static string SkillFile() {
            return SkillFrontmatter + "\n# " + Title + "\n\n" + Body + "\n";
        }

        public static string BlockBody() {
            return "## " + Title + "\n\n" + Body;
        }
    }
}
