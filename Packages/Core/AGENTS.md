# Spellbound.Core

## Purpose

Gives every package one vocabulary: how to log, pack bytes, identify a thing, roll from a seeded stream, count steps of time, place a pose in a reference frame, measure a cost against a budget, declare an operation, and validate what a designer or an agent authors.

## Front door

`CoreKit`. `CoreKit.Operations` is the host that runs an operation line; `CoreKit.Packing.Describe(type)` writes the packed layout of a type.

```csharp
var run = CoreKit.Operations.Run("core help", OperationCaller.Development());
Debug.Log(run.Result.Text);
```

A package declares what an agent or a person can call with `[assembly: OperationGroup(name, job)]` on its assembly and `[Operation(name, description, effect)]` on a public static method, each parameter described by `[Argument(description)]`. The same declaration serves C#, a console, the Spellbound window and the Unity command line, where `spellbound_run` takes one operation line. `core context --package <group>` prints a package's own context with its operations. The generated text below is refreshed from the live catalog by `core context`.

<!-- spellbound:operations 2a658ffe -->
com.spellboundstudios.core 1.1.9
Operations of the group `core`, run as `<group> <operation> [arguments]`:
- `core about`: Describes this process: the Unity version, the render pipeline and the operation groups it holds. [reads, anyone]
- `core context [--package <text>]`: Lists the installed Spellbound packages with their front doors and operations, or prints one package's agent context with its generated blocks refreshed. [reads, anyone]
  - `--package`: The operation group or package id to print in full; leave it out to list every package. Default: none.
- `core context-install --target <one of Skill, Agents, Claude>`: Writes the stub that tells an agent how to reach the installed packages: a skill file, or a block in the project's AGENTS.md or CLAUDE.md. [writes, developer]
  - `--target`: Skill writes .claude/skills/spellbound/SKILL.md; Agents and Claude write one delimited block into AGENTS.md or CLAUDE.md at the project root.
- `core help [--group <text>] [--all <true or false> = false]`: Lists the operation groups, or the operations of one group with their arguments. [reads, anyone]
  - `--group`: The group to list; leave it out to list every group. Default: none.
  - `--all`: True to list every operation of every group. Default: false.
- `core history [--offset <integer> = 0] [--limit <integer> = 50]`: Lists the operation lines this host has run, oldest first, with their status, caller and frame. [reads, developer]
  - `--offset`: How many entries to skip from the start. Default: 0.
  - `--limit`: How many entries to list. Default: 50.
- `core ledger`: Lists every installed Spellbound package's Ledger rows: the Unity types it builds on and what moved out of it. [reads, anyone]
- `core stats`: Lists each operation this host has run with its calls, failures and durations in milliseconds. [reads, developer]
<!-- /spellbound:operations -->

## Rules

The binding rules are the line text at https://github.com/Spellbound-Studios/.github/tree/main/line

- Log through `Spellbound.Core.Logging.Log`, never through `Debug.Log`.
- A result's first word is its status: `succeeded`, `invalid`, `failed`, `refused` or `previewed`. A script branches on it; the prose after it is for people and agents.
- An operation that deletes data is refused without `--confirm`; `--dry-run` answers `previewed:` and changes nothing.
- An operation that could outlast a domain reload is a `<name>-start` and `<name>-status` pair; an operation never opens a modal dialog.
- Nothing per frame or per tick goes through an operation: the host reflects, parses and allocates per call.
- `OperationSettings` is optional. Every field has a default, so no asset is required; the field `bridgeAudiences` is the one setting that grants the audience `approver` to an agent on the Unity command line.

## Not for

- Anything drawn or spawned, a world-object framework, a console view, a chat window: those are other packages' or the consumer's. The folders `Runtime/WorldObjects` and `Runtime/Console` are leaving Core.
- Connections, players or per-player bookkeeping. Core packs bytes; who receives them is Networking's.
- A value a game tunes. Core holds vocabulary and defaults, never a game's numbers.

## Gaps

- `core validate`, `core preview`, `core drafts`, `core revise` and `core approve` are not built yet; authored content is validated only by its own inspector until the authoring contract lands.
- `core observations` waits for the observation model; `core stats` reads the host's own tally of calls, failures and durations.
- The reference forms `world:3` and `player` are not bound yet; `scene:/Path` and `asset:` work.

## Ledger

- Builds on `UnityEngine.CustomYieldInstruction`: an operation run is one, so a Play Mode test can yield it.
- Builds on `UnityEngine.Scripting.PreserveAttribute`: the three marker attributes derive from it, so a marked method, parser or renderer survives managed code stripping.
- Builds on `UnityEngine.Resources`: `OperationSettings` and `LogConfig` load from a Resources folder when present.
- Builds on `UnityEditor.PackageManager.PackageInfo`: Core.Editor finds a package's folder and version through it.
- Moved `Objects`, `ObjectData`, `ECS` and `ModuleContracts` to the assembly `Spellbound.Core.WorldObjects`, waiting for the WorldObjects package.
- Moved the console folder to the assembly `Spellbound.Core.Console`, which leaves Core when the operation console replaces it.
