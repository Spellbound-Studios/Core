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

A definition a designer or an agent authors is a ScriptableObject that implements `IAuthoredDefinition` and adds its findings to a `ValidationReport` (`Error`, `Warning`, `Information`, each with a code `<package>.<kebab-name>`). Core.Editor runs it: `core validate` over an asset, a folder, a package folder, the drafts folder or everything, `core preview`, `core drafts` and `core approve`. A package adds a check to a definition another package owns by implementing `IDefinitionCheck<T>` in its own editor assembly and marking the class `[RegisteredDefinitionCheck("<package>.<kebab-name>")]`; a `DefinitionPreviewer<T>` in that assembly draws its summary. A report is the status line, then `checks:` naming what ran as `<name> (<Package>)`, then one finding per line, code first.

<!-- spellbound:operations ea3ed1d7 -->
com.spellboundstudios.core 1.1.9
Operations of the group `core`, run as `<group> <operation> [arguments]`:
- `core about`: Describes this process: the Unity version, the render pipeline and the operation groups it holds. [reads, anyone]
- `core approve --target <scriptable-object> --to <text>`: Moves a draft out of the drafts folder into the project, keeping its GUID; refused while the draft has an error finding. [writes, approver]
  - `--target`: The draft, as asset:<path> or asset:<name>.
  - `--to`: The folder under Assets the approved asset moves to.
- `core context [--package <text>]`: Lists the installed Spellbound packages with their front doors and operations, or prints one package's agent context with its generated blocks refreshed. [reads, anyone]
  - `--package`: The operation group or package id to print in full; leave it out to list every package. Default: none.
- `core context-install --target <one of Skill, Agents, Claude>`: Writes the stub that tells an agent how to reach the installed packages: a skill file, or a block in the project's AGENTS.md or CLAUDE.md. [writes, developer]
  - `--target`: Skill writes .claude/skills/spellbound/SKILL.md; Agents and Claude write one delimited block into AGENTS.md or CLAUDE.md at the project root.
- `core drafts`: Lists the definitions waiting in the drafts folder with the state of their report. [reads, developer]
- `core help [--group <text>] [--all <true or false> = false]`: Lists the operation groups, or the operations of one group with their arguments. [reads, anyone]
  - `--group`: The group to list; leave it out to list every group. Default: none.
  - `--all`: True to list every operation of every group. Default: false.
- `core history [--offset <integer> = 0] [--limit <integer> = 50]`: Lists the operation lines this host has run, oldest first, with their status, caller and frame. [reads, developer]
  - `--offset`: How many entries to skip from the start. Default: 0.
  - `--limit`: How many entries to list. Default: 50.
- `core ledger`: Lists every installed Spellbound package's Ledger rows: the Unity types it builds on and what moved out of it. [reads, anyone]
- `core preview --target <scriptable-object>`: Describes an authored definition: its previewer's summary and pictures, and the lines every registered check contributes. [reads, anyone]
  - `--target`: The definition asset, as asset:<path> or asset:<name>.
- `core stats`: Lists each operation this host has run with its calls, failures and durations in milliseconds. [reads, developer]
- `core validate [--path <text>] [--drafts <true or false> = false] [--timings <true or false> = false]`: Validates authored definitions: one asset, a folder, a package folder, the drafts folder or everything that implements the contract, and writes the one report with its checks line. [reads, anyone]
  - `--path`: An asset path, a folder under Assets, or a package folder as Packages/<id> or Packages/<Name>; leave it out to validate everything. Default: none.
  - `--drafts`: True to validate the drafts folder named in OperationSettings. Default: false.
  - `--timings`: True to append a table with the milliseconds each validator and check took. Default: false.
<!-- /spellbound:operations -->

## Rules

The binding rules are the line text at https://github.com/Spellbound-Studios/.github/tree/main/line

- Log through `Spellbound.Core.Logging.Log`, never through `Debug.Log`.
- A result's first word is its status: `succeeded`, `invalid`, `failed`, `refused` or `previewed`. A script branches on it; the prose after it is for people and agents.
- An operation that deletes data is refused without `--confirm`; `--dry-run` answers `previewed:` and changes nothing.
- An operation that could outlast a domain reload is a `<name>-start` and `<name>-status` pair; an operation never opens a modal dialog.
- Nothing per frame or per tick goes through an operation: the host reflects, parses and allocates per call.
- `OperationSettings` is optional. Every field has a default, so no asset is required; the field `bridgeAudiences` is the one setting that grants the audience `approver` to an agent on the Unity command line.
- `core approve` needs the audience `approver` and is refused while the draft has an error finding; a draft lives in `OperationSettings.draftsFolder` (default `Assets/Drafts`) and every path an authoring operation writes is under `Assets` or `Library/Spellbound`.

## Not for

- Anything drawn or spawned, a world-object framework, a console view, a chat window: those are other packages' or the consumer's. The folders `Runtime/WorldObjects` and `Runtime/Console` are leaving Core.
- Connections, players or per-player bookkeeping. Core packs bytes; who receives them is Networking's.
- A value a game tunes. Core holds vocabulary and defaults, never a game's numbers.

## Gaps

- `core revise` and the authored-key scan (`core.unknown-key`) are not built yet; a draft is made new, and a renamed field is re-authored.
- The inspector banner shows a definition's last report and runs the full set on a save or from its button; it does not yet revalidate while a person edits.
- Validation timings stay on the run (`core validate --timings`) until the observation model exists.
- `core observations` waits for the observation model; `core stats` reads the host's own tally of calls, failures and durations.
- The reference forms `world:3` and `player` are not bound yet; `scene:/Path` and `asset:` work.

## Ledger

| Id | Ours | Ours (type) | Unity item | Verdict | Judged at | Reason | Evidence | Watch packages | Watch words |
|---|---|---|---|---|---|---|---|---|---|
| U-001 | Core object pool and GeoForge's own pools | ``Spellbound.Core.ObjectPooling.ObjectPool`1`` | UnityEngine.Pool.ObjectPool, ListPool | Moved | 6000.4 | Unity's pool takes create, get, release and destroy delegates with a default capacity and a maximum size, and its collection checks run only in the Editor. Ours adds only Prewarm, which a loop of Get and Release does, and a Discard hook, which actionOnDestroy covers. | S11; docs 6000.4 Pool.ObjectPool_1 | | ObjectPool; ListPool |
| U-002 | Core ECS drawer and entity prefab registry | `Spellbound.Core.ECS.InstantiationSystem` | BatchRendererGroup through Instancing | Moved | 6000.4; com.unity.render-pipelines.universal 17.4.0 | Decided 2026-10-23: data-driven instances have no GameObject to give the Resident Drawer, the manual sends them to BatchRendererGroup, and Entities Graphics needs Forward+ and does not support multiple Worlds; epic U3 measures the path. 6.7 beta probe 2026-10-25: unsure, GPUResidentDrawerMode InstancedDrawing is honoured but no Instancing code exists to draw. | S2; S6; S46; InstanceCullingBatcher.cs:520-525; EntitiesGraphicsSystem.cs:1033; docs 6000.4 optimizing-draw-calls-choose-method | com.unity.render-pipelines.universal | BatchRendererGroup; GPU Resident Drawer; GPU driven; DOTS instancing; unity_ObjectToWorld |
| U-003 | Object parent queries over entities | `Spellbound.Core.ObjectHandling.ObjectParent` | Entities, native containers | Moved | 6000.4 | Moved to native columns in WorldObjects: a column store and a Burst job need no Unity feature. Watch Unity's GameObjects are Entities work, which may one day give GameObjects chunk storage. 6.7 beta probe 2026-10-25: changed, EntityId from int is an error, and ObjectParent.cs:580 fails on the removed managed shared component call. | S9; forum 2026-08-05 | | EntityId; unified transform; GameObjects are Entities |
| U-006 | Resources.LoadAll registries in Core, Modifiers, Controller, UI and WorldForge | ``Spellbound.Core.Registries.ResourceRegistry`1`` | Preloaded Assets, Addressables | Moved | 6000.4 | Unity combines everything under Resources into one serialized file whose index costs memory even for objects never loaded, and points to Addressables instead; preloaded assets load at player start and stay alive until it ends. | S12; S13 | | Resources; Preloaded Assets; Addressables |
| U-007 | Core logger and sinks | `Spellbound.Core.Logging.Log` | Debug.unityLogger, the Logging package | Keeps | 6000.4 | The Logging package is deprecated from 6.3 and Unity says the default logging covers most uses; our sinks, tallies and levels sit on Debug. | S27 | | Debug.unityLogger; Logging package; deprecated |
| U-008 | Core observation model | | ProfilerRecorder, ProfilerCounterValue, FrameTimingManager | Builds on | 6000.4 | Unity compiles Profiler counters out of non-development builds, so Core keeps its own counters and mirrors them to Profiler counters in development builds only; FrameTimingManager works in release builds when Frame Timing Stats is on. Not built yet, area 3 builds it. | S14; S15 | | ProfilerRecorder; ProfilerCounterValue; FrameTimingManager |
| U-016 | Core sample import operation (Line rule 8) | | UnityEditor.PackageManager.UI.Sample.FindByPackage and Import | Builds on | 6000.4 | The Package Manager has a C# route to import a sample into Assets, and the operation wraps it. The operation is not built yet. | S23 | | Sample.FindByPackage; Sample.Import |
| U-017 | Spellbound window (Line rule 16) | | Package Manager | Keeps | 6000.4 | The Package Manager cannot follow a git URL from a package and cannot grey out a paid package. The window is not built yet. | Line decision 2 | | Package Manager; git URL; dependency |
| U-018 | Core log settings inspector | `Spellbound.Core.Logging.Editor.LogConfigEditor` | UnityEditor.Editor.CreateInspectorGUI, UI Toolkit | Moved | 6000.4 | Decided 2026-10-24: the inspector is drawn with the old editor system, Unity's reference says to implement CreateInspectorGUI for a UI Toolkit inspector, and the studio sells UI Toolkit only. Area 11's epic U7b rewrites it; until it lands the check reports this row wrong, as it does for U-002. | S49; Corrections.md rule 3 | | CreateInspectorGUI; OnInspectorGUI; IMGUI |
| U-020 | Rule 12, no domain reload dependence | | Fast Enter Play Mode default, AutoStaticsCleanup, Project Auditor UAL analyzer | Builds on | 6000.4 | Reload is on by default in 6000.4, new 6.6 projects default to reload scene only, and the attribute arrived in 6.5; packages at the 6000.3 floor keep their explicit resets and adopt the attribute when the floor reaches 6.7. Unity 7's fast Play mode keeps statics alive, so the clock is the Unity 7 timeline and Core's key registry reset stays explicit. 6.7 beta probe 2026-10-25: unchanged, with warning UAC0005 on AppDomain.GetAssemblies, and the scan still runs with domain reload off. | S29; S32; S34; S45; S52 p1; S52 p9; S52 p14; S52 p15 | | AutoStaticsCleanup; domain reload; Enter Play Mode |
| U-021 | Runtime AppDomain assembly scans and Volumetrics' DomainUnload releases | `Spellbound.Core.Tooling.AssemblyScanning` | CoreCLR load contexts and the lifecycle attributes | Watch | 6000.4 | Unity advises CurrentAssemblies.GetLoadedAssemblies over AppDomain.GetAssemblies and says DomainUnload will no longer fire; domain reloads are gone in Unity 7. Then Moved: our one scan call switches behind versionDefines in epic U8, on the Unity 7 timeline. 6.7 beta probe 2026-10-25: unchanged, with warning UAC0005 on AssemblyScanning.cs:10, and SmartPackerRegistry registered its packers on both entries with domain reload off. | S9; S29; S30; S31; AssemblyScanning.cs:10; PipelineUtils.cs:190-197 | | Assemblies.CurrentAssemblies; GetLoadedAssemblies; DomainUnload |
| U-043 | Core packing | `Spellbound.Core.Packing.Packer` | com.unity.serialization | Keeps | 6000.4 | Unity deprecated the package in 6.6 and suggests System.Text.Json, Odin Serializer or MemoryPack, none of which is a bit writer with stable hashes and deltas. 6.7 beta probe 2026-10-25: unsure, the copy pulls com.unity.serialization through Entities only and Core's packing does not reference it. | S31 | | Serialization package; deprecated; bit writer |
| U-050 | Operations on the command line | `Spellbound.Core.Pipeline.SpellboundRunCommand` | Unity Pipeline package (experimental) | Builds on | 6000.4; com.unity.pipeline 0.8.0-exp.1 | In 0.8.0-exp.1 the declaration attributes moved to Unity.Pipeline.Attributes and CommandRegistry became public; results are unwrapped for Task only, and player discovery is a reflection fallback not tested under stripping. 6.7 beta probe 2026-10-25: changed in words, Pipeline runs on 6.7 and the Unity command line asks every project to run unity pipeline upgrade for the old command names. | S41 | com.unity.pipeline | CliCommand; Awaitable; link.xml; CommandRegistry |
| U-051 | set_serialized_field for authored content | | Pipeline set_serialized_field | Builds on | 6000.4; com.unity.pipeline 0.8.0-exp.1 | It is unsupported for managed references, anything outside the listed types throws, and run_script covers them. | S41 | com.unity.pipeline | set_serialized_field; managed reference; run_script |
| U-058 | Operation run | `Spellbound.Core.Operations.OperationRun` | UnityEngine.CustomYieldInstruction | Builds on | 6000.4 | An operation run is a custom yield instruction, so a Play Mode test can yield it. | OperationRun.cs:9 | | CustomYieldInstruction; keepWaiting |
| U-059 | Operation marker attributes | `Spellbound.Core.Operations.OperationAttribute` | UnityEngine.Scripting.PreserveAttribute | Builds on | 6000.4 | The three marker attributes derive from it, so a marked method, parser or renderer survives managed code stripping. | OperationAttribute.cs:8 | | PreserveAttribute; managed code stripping |
| U-060 | Operation and log settings | `Spellbound.Core.Operations.OperationSettings` | UnityEngine.Resources | Builds on | 6000.4 | OperationSettings and LogConfig load from a Resources folder when one is present. | OperationSettings.cs:22 | | Resources.Load; Resources folder |
| U-061 | Installed package lookup | `Spellbound.Core.Operations.Editor.EditorInstalledPackages` | UnityEditor.PackageManager.PackageInfo | Builds on | 6000.4 | Core.Editor finds a package's folder and version through PackageInfo. | EditorInstalledPackages.cs:27; EditorInstalledPackages.cs:42 | | PackageInfo; GetAllRegisteredPackages; FindForAssembly |
| U-062 | Seeded stream | `Spellbound.Core.Simulation.SeededStream` | Unity.Mathematics.Random, UnityEngine.Random | Keeps | 6000.4; com.unity.mathematics 1.3.3 | Core on the engine alone cannot use Mathematics at the 6000.3 floor, and the stream names its algorithm, SplitMix64 seeding xoshiro256** draws. | S32; R0-4 | com.unity.mathematics | Random; built-in module |
| U-063 | Tick clock and its driver | `Spellbound.Core.Simulation.TickClock` | MonoBehaviour.FixedUpdate, DefaultExecutionOrder | Builds on | 6000.4 | The driver advances the clock at the earliest execution order and raises Stepped at the latest. | S50; R0-3 | | FixedUpdate; DefaultExecutionOrder; Time.fixedDeltaTime |
| U-064 | Up field | `Spellbound.Core.ReferenceFrames.UpField` | Unity.Collections.NativeArray in the engine module | Builds on | 6000.4; com.unity.collections 6.4.0 | The up field is a blittable struct a Burst job evaluates, so Core needs only the engine's native array and no Collections package. | R0-6 | com.unity.collections | NativeArray; Collections; module |

- Moved `Objects`, `ObjectData`, `ECS` and `ModuleContracts` to the assembly `Spellbound.Core.WorldObjects`, waiting for the WorldObjects package.
- Moved the console folder to the assembly `Spellbound.Core.Console`, which leaves Core when the operation console replaces it.
