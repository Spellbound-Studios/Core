# Spellbound.Core

## Purpose

Gives every package one vocabulary: how to log, pack bytes, identify a thing, roll from a seeded stream, count steps of time, place a pose in a reference frame, measure a cost against a budget, declare an operation, and validate what a designer or an agent authors.

## Front door

`CoreKit`. `CoreKit.Operations` is the host that runs an operation line; `CoreKit.Packing.Describe(type)` writes the packed layout of a type.

Packing serves the save format and the wire. A type implements `IPacker` with `Pack(ref Span<byte>)` and `Unpack(ref ReadOnlySpan<byte>)` over the `Packer` reads and writes (little-endian primitives, strings and byte runs with a length prefix, vectors, lists and arrays, varints). `Packer.ToBytes(in value)` allocates the result; `CoreKit.Packing.TryPack(in value, destination, out written)` packs into a buffer the caller owns and answers false when it is too small; `CoreKit.Packing.PackPooled(in value)` rents one from the runtime's array pool and the `PooledPackedBytes` returns it on `Dispose`; `PackingLimits` bounds the stack buffer, the first rented size and the largest payload as an optional last argument. Bytes from the wire are read with the `TryRead` twins, which answer false and leave the span where it was, with `DecodeLimits` or a maximum for strings, byte runs and counts, or with `CoreKit.Packing.TryUnpack(bytes, out value)`, which refuses a short payload and trailing bytes. `BitWriter` and `BitReader` pack bits, varints and `Quantization` values into a span; `CoreKit.Packing.PackDelta(baseline, current, destination, out written)` and `TryUnpackDelta` carry the difference between two payloads, and who holds which baseline is Networking's.

```csharp
var run = CoreKit.Operations.Run("core help", OperationCaller.Development(), ObservationContext.Discard);
Debug.Log(run.Result.Text);
```

`CoreKit.Time.StartClock(source, observations)`, or `StartClock(rate, observations)` for the default game-time source, starts a `TickClock` that the driver advances every fixed step and that raises `Stepped` at the latest execution order; `CoreKit.Time.StopClock(clock)` ends it. The clock reports a held step under `core.clock.held` and a changed source rate under `core.clock.rate_changed`, into the context it was given.

A package declares what it observes once, as static fields: `CoreKit.Observations.Counter(key, description)`, `Occurrence(key, description)`, `Gauge(key, description, unit)` and `Cost(key, description, unit, defaultBudget, defaultQualityLevel)`, the key spelled `package.subject.name` and the description required. It reports against an `ObservationContext` a world instance owns, made by `CoreKit.Observations.CreateContext(name, settings)`: `counter.Add(context, reason)`, `occurrence.Record(context, tick, subject)`, `gauge.Set(context, value)`, `cost.Record(context, amount)` or `using (cost.Measure(context))`. Every door that reports takes the context as its last explicit argument and never looks one up; a caller that wants none passes `ObservationContext.Discard`. `context.Read(package, readings)` fills a caller-owned list of `ObservationReading` without allocating, `TryRead`, `ReadReasons`, `Version(key)` and `PackageVersion(package)` read one key or a package's change count, `SetBudget` and `SetQualityLevel` override a declaration's defaults, and `ObservationContexts.All` lists the live contexts for a collector. `CoreKit.Observations.Declared()` lists every declaration with nothing running, `Reason(name)` registers a reason name once, and `Compare(before, after, ratios)` gives the ratio per key. `OperationHost.Run(line, caller, observations)` records each operation group's duration under `<group>.operations` and its failures under `<group>.operation_failures`; `CallSiteLogSink` counts logged lines under `core.log.<level>` with the call site as the reason.

A package declares what an agent or a person can call with `[assembly: OperationGroup(name, job)]` on its assembly and `[Operation(name, description, effect)]` on a public static method, each parameter described by `[Argument(description)]`. The same declaration serves C#, a console, the Spellbound window and the Unity command line, where `spellbound_run` takes one operation line. `core context --package <group>` prints a package's own context with its operations. The generated text below is refreshed from the live catalog by `core context`.

A definition a designer or an agent authors is a ScriptableObject that implements `IAuthoredDefinition` and adds its findings to a `ValidationReport` (`Error`, `Warning`, `Information`, each with a code `<package>.<kebab-name>`). Core.Editor runs it: `core validate` over an asset, a folder, a package folder, the drafts folder or everything, `core preview`, `core drafts` and `core approve`. A package adds a check to a definition another package owns by implementing `IDefinitionCheck<T>` in its own editor assembly and marking the class `[RegisteredDefinitionCheck("<package>.<kebab-name>")]`; a `DefinitionPreviewer<T>` in that assembly draws its summary. A report is the status line, then `checks:` naming what ran as `<name> (<Package>)`, then one finding per line, code first.

A package declares what it is once, in the `Identity.asset` at its root, a `PackageIdentity`: its kind, layer, status, one-sentence job and front door, what it must not own, and the switches its checks read. `PackageIdentity.JudgmentFields` names the fields a code owner reviews; `PackageIdentity.Find(folder)` loads it. The Spellbound window (`SpellboundWindow`, Window, Spellbound) draws the pages other packages register: a class deriving from `WindowPage` marked `[WindowPage(title, order)]` appears there with no change to Core; a page sets the window's header sentence and state and writes a footer line through `WindowPage.HeaderSentence`, `WindowPage.HeaderState` and `WindowPage.FooterText`.

An editor window or inspector takes the installed look with `EditorLook.Dress(root)`: the theme class, Core's layout sheet, every sheet a package registered through `EditorLook.Register(LookSource)` and the project's own from an `EditorLookSettings` asset. Core ships no theme; a `HeaderBar` shows one of the `HeaderStates`, and `ButtonVariants` and `ChipTones` name the shared classes a look styles. A look or project rule is scoped under `.sb-theme`, so it wins over Core's layout sheet.

<!-- spellbound:operations a693157a -->
com.spellboundstudios.core
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
- `core observation --key <text> [--world <text>]`: Prints one observation key: its declaration, its reading in one world and the counts by reason. [reads, anyone]
  - `--key`: The key, as package.subject.name.
  - `--world`: The world whose reading to show; leave it out when one world is live. Default: none.
- `core observations [--world <text>] [--prefix <text>]`: Lists the declared observation keys with their readings in one world: kind, unit, value, count, peak, budget and quality level. [reads, anyone]
  - `--world`: The world whose readings to show; leave it out when one world is live. Default: none.
  - `--prefix`: Only the keys that start with this text. Default: none.
- `core preview --target <scriptable-object>`: Describes an authored definition: its previewer's summary and pictures, and the lines every registered check contributes. [reads, anyone]
  - `--target`: The definition asset, as asset:<path> or asset:<name>.
- `core stats`: Lists each operation this host has run with its calls, failures and durations in milliseconds. [reads, developer]
- `core unity-ledger [--move-form <true or false> = false]`: Checks every ledger of Unity systems the installed packages and this project rest on: a row whose type contradicts its verdict is wrong, a row whose editor or watched package moved past its judgement is stale, with the changelog lines that carry a watch word. [reads, anyone]
  - `--move-form`: True to print the lines for the Move issue form instead of the report. Default: false.
- `core validate [--path <text>] [--drafts <true or false> = false] [--timings <true or false> = false]`: Validates authored definitions: one asset, a folder, a package folder, the drafts folder or everything that implements the contract, and writes the one report with its checks line. [reads, anyone]
  - `--path`: An asset path, a folder under Assets, or a package folder as Packages/<id> or Packages/<Name>; leave it out to validate everything. Default: none.
  - `--drafts`: True to validate the drafts folder named in OperationSettings. Default: false.
  - `--timings`: True to append a table with the milliseconds each validator and check took. Default: false.
<!-- /spellbound:operations -->

<!-- spellbound:depends 45527832 -->
Depends on:
- `com.unity.entities` 1.3.14
- `com.unity.entities.graphics` 1.3.14
- `com.unity.mathematics` 1.3.2
- `com.unity.burst` 1.8.19
- `com.unity.collections` 2.5.7
- `com.unity.inputsystem` 1.11.2
- `com.unity.ugui` 2.0.0
Bridges, each compiled only when its package is installed:
- `Spellbound.Core.Pipeline` when `com.unity.pipeline` is in [0.8.0-exp.1,0.9.0-exp.1)
- `Spellbound.Core.Pipeline` when `com.unity.pipeline` is in [0.9.0-exp.1,0.10.0-exp.1)
<!-- /spellbound:depends -->

<!-- spellbound:pipeline 916d3b95 -->
Agents run these operation lines from the Unity command line when `com.unity.pipeline` is installed in [0.8.0-exp.1,0.9.0-exp.1) or [0.9.0-exp.1,0.10.0-exp.1): find the command with `unity command --query spellbound` and pass it one line.
<!-- /spellbound:pipeline -->

## Rules

The binding rules are the line text at https://github.com/Spellbound-Studios/.github/tree/main/line

- Log through `Spellbound.Core.Logging.Log`, never through `Debug.Log`.
- Outside play mode the logger writes to the Unity console through one sink; the sinks of the `LogConfig` register when play mode starts.
- A result's first word is its status: `succeeded`, `invalid`, `failed`, `refused` or `previewed`. A script branches on it; the prose after it is for people and agents.
- An operation that deletes data is refused without `--confirm`; `--dry-run` answers `previewed:` and changes nothing.
- An operation that could outlast a domain reload is a `<name>-start` and `<name>-status` pair; an operation never opens a modal dialog.
- Nothing per frame or per tick goes through an operation: the host reflects, parses and allocates per call.
- `OperationSettings` is optional. Every field has a default, so no asset is required; the field `bridgeAudiences` is the one setting that grants the audience `approver` to an agent on the Unity command line.
- `core approve` needs the audience `approver` and is refused while the draft has an error finding; a draft lives in `OperationSettings.draftsFolder` (default `Assets/Drafts`) and every path an authoring operation writes is under `Assets` or `Library/Spellbound`.
- An observation key is declared once, by one package, as an immutable static field of a class that holds only declarations (`<Package>Observations`), with a required description; a second declaration of a key with another kind or description throws at static initialisation. `Declared()` finds such classes in every assembly that references Core and runs their static constructors, so a class that mixes a declaration with other static state is not scanned. A context is read and written on the main thread; `CallSiteLogSink` drops a line emitted from another thread and counts the drop in `DroppedOffThread`.
- A reading's `Value`, `Total` and `Peak` follow the kind: a counter's value is its running total and its peak the largest single add; an occurrence's value is its count; a gauge's value is the last set and its peak the highest; a cost's value is the last amount, its total the sum and its peak the largest amount. `Count` is the number of reports in every kind.

## Not for

- Anything drawn or spawned, a world-object framework, a console view, a chat window: those are other packages' or the consumer's. The folders `Runtime/WorldObjects` and `Runtime/Console` are leaving Core.
- Connections, players or per-player bookkeeping. Core packs bytes; who receives them is Networking's.
- A value a game tunes. Core holds vocabulary and defaults, never a game's numbers.

## Gaps

- `core revise` and the authored-key scan (`core.unknown-key`) are not built yet; a draft is made new, and a renamed field is re-authored.
- The inspector banner shows a definition's last report and runs the full set on a save or from its button; it does not yet revalidate while a person edits.
- Validation timings stay on the run (`core validate --timings`); they are not yet reported as costs.
- The flight recorder, guards with a policy, the intent key and ledger, the native queue for jobs, the profiler mirror and the key validator's checks are not built yet; an occurrence only counts (its tick and subject are taken and not kept until the recorder exists), and a packed snapshot of a context's readings does not exist.
- A line captured from Unity's own log (`UnityLogCapture`) reaches `CallSiteLogSink` with the source `Unity`, the log type as its member and line 0, so every `Debug.Log` from anywhere counts under one call site per log type.
- The reference forms `world:3` and `player` are not bound yet; `scene:/Path` and `asset:` work.
- The Spellbound window holds no page of Core's own yet: the Line, Package, Operations and Drafts pages are not built.

## Ledger

| Id | Ours | Ours (type) | Unity item | Verdict | Judged at | Reason | Evidence | Watch packages | Watch words |
|---|---|---|---|---|---|---|---|---|---|
| U-001 | Core object pool and GeoForge's own pools | ``Spellbound.Core.ObjectPooling.ObjectPool`1`` | UnityEngine.Pool.ObjectPool, ListPool | Moved | 6000.4 | Unity's pool takes create, get, release and destroy delegates with a default capacity and a maximum size, and its collection checks run only in the Editor. Ours adds only Prewarm, which a loop of Get and Release does, and a Discard hook, which actionOnDestroy covers. | Unity 6000.4 script reference, UnityEngine.Pool.ObjectPool<T> and its constructor | | ObjectPool; ListPool |
| U-002 | Core ECS drawer and entity prefab registry | `Spellbound.Core.ECS.InstantiationSystem` | BatchRendererGroup through Instancing | Moved | 6000.4; com.unity.render-pipelines.universal 17.4.0 | Decided 2026-10-23: data-driven instances have no GameObject to give the Resident Drawer, the manual sends them to BatchRendererGroup, and Entities Graphics needs Forward+ and does not support multiple Worlds; epic U3 measures the path. 6.7 beta probe 2026-10-25: unsure, GPUResidentDrawerMode InstancedDrawing is honoured but no Instancing code exists to draw. | Unity 6000.4 manual, draw call reduction methods (optimizing-draw-calls-choose-method); Entities Graphics 1.3 requirements-and-compatibility.md; com.unity.render-pipelines.core Runtime/GPUDriven/InstanceCullingBatcher.cs:520-525; Entities Graphics EntitiesGraphicsSystem.cs:1033 | com.unity.render-pipelines.universal | BatchRendererGroup; GPU Resident Drawer; GPU driven; DOTS instancing; unity_ObjectToWorld |
| U-003 | Object parent queries over entities | `Spellbound.Core.ObjectHandling.ObjectParent` | Entities, native containers | Moved | 6000.4 | Moved to native columns in WorldObjects: a column store and a Burst job need no Unity feature. Watch Unity's GameObjects are Entities work, which may one day give GameObjects chunk storage. 6.7 beta probe 2026-10-25: changed, EntityId from int is an error, and ObjectParent.cs:580 fails on the removed managed shared component call. | Unity forum, CoreCLR, Scripting, and ECS Status Update (2026-03-06) and the thread 2026 ECS for all (2026-08-05) | | EntityId; unified transform; GameObjects are Entities |
| U-006 | Resources.LoadAll registries in Core, Modifiers, Controller, UI and WorldForge | ``Spellbound.Core.Registries.ResourceRegistry`1`` | Preloaded Assets, Addressables | Moved | 6000.4 | Unity combines everything under Resources into one serialized file whose index costs memory even for objects never loaded, and points to Addressables instead; preloaded assets load at player start and stay alive until it ends. | Unity manual, The Resources folder (Manual/LoadingResourcesatRuntime.html); Unity 6000.4 script reference, PlayerSettings.GetPreloadedAssets | | Resources; Preloaded Assets; Addressables |
| U-007 | Core logger and sinks | `Spellbound.Core.Logging.Log` | Debug.unityLogger, the Logging package | Keeps | 6000.4 | The Logging package is deprecated from 6.3 and Unity says the default logging covers most uses; our sinks, tallies and levels sit on Debug. | Unity Logging package 1.5 manual, deprecated from 6.3 | | Debug.unityLogger; Logging package; deprecated |
| U-008 | Core observation model | `Spellbound.Core.Observations.ObservationContext` | ProfilerRecorder, ProfilerCounterValue, FrameTimingManager | Builds on | 6000.4 | Unity compiles Profiler counters out of non-development builds, so Core keeps its own counters in the context and will mirror them to Profiler counters in development builds only; FrameTimingManager works in release builds when Frame Timing Stats is on. The declarations and the per-world context are built; the mirror is not. | Unity 6000.4 manual, custom Profiler counters; Unity 6000.4 script reference, FrameTimingManager | | ProfilerRecorder; ProfilerCounterValue; FrameTimingManager |
| U-016 | Core sample import operation (Line rule 9) | | UnityEditor.PackageManager.UI.Sample.FindByPackage and Import | Builds on | 6000.4 | The Package Manager has a C# route to import a sample into Assets, and the operation wraps it. The operation is not built yet. | Unity 6000.4 script reference, PackageManager.UI.Sample | | Sample.FindByPackage; Sample.Import |
| U-017 | Spellbound window (Line rule 18) | | Package Manager | Keeps | 6000.4 | The Package Manager cannot follow a git URL from a package and cannot grey out a paid package. The window shell is built; its Package page is not. | Unity 6000.4 manual, Git dependencies (Manual/upm-git.html): a package names its own dependencies by registry version, never by git URL | | Package Manager; git URL; dependency |
| U-018 | Core log settings inspector | `Spellbound.Core.Logging.Editor.LogConfigEditor` | UnityEditor.Editor.CreateInspectorGUI, UI Toolkit | Moved | 6000.4 | Decided 2026-10-24: the inspector was drawn with the old editor system, Unity's reference says to implement CreateInspectorGUI for a UI Toolkit inspector, and the studio sells UI Toolkit only. Rewritten 2026-10-27 as LogSettingsInspector, which implements CreateInspectorGUI. | Unity 6000.3 script reference, UnityEditor.Editor.CreateInspectorGUI; LogSettingsInspector.cs | | CreateInspectorGUI; OnInspectorGUI; IMGUI |
| U-020 | Rule 13, no domain reload dependence | | Fast Enter Play Mode default, AutoStaticsCleanup, Project Auditor UAL analyzer | Builds on | 6000.4 | Reload is on by default in 6000.4, new 6.6 projects default to reload scene only, and the attribute arrived in 6.5; packages at the 6000.3 floor keep their explicit resets and adopt the attribute when the floor reaches 6.7. Unity 7's fast Play mode keeps statics alive, so the clock is the Unity 7 timeline and Core's key registry reset stays explicit. 6.7 beta probe 2026-10-25: unchanged, with warning UAC0005 on AppDomain.GetAssemblies, and the scan still runs with domain reload off. | Unity forum, Path to CoreCLR, 2026: Upgrade Guide (2026-03-26) and the CoreCLR updates of 2026-06-16 and 2026-10-05; New in Unity 6.5 (manual/whats-new/unity65); Unity forum, Unity 6.6 is now available (2026-09-01); Unity 6000.4 manual, configuring how Unity enters Play mode; Unity 7000.0.0a12 release notes | | AutoStaticsCleanup; domain reload; Enter Play Mode |
| U-021 | Runtime AppDomain assembly scans and Volumetrics' DomainUnload releases | `Spellbound.Core.Tooling.AssemblyScanning` | CoreCLR load contexts and the lifecycle attributes | Watch | 6000.4 | Unity advises CurrentAssemblies.GetLoadedAssemblies over AppDomain.GetAssemblies and says DomainUnload will no longer fire; domain reloads are gone in Unity 7. Then Moved: our one scan call switches behind versionDefines in epic U8, on the Unity 7 timeline. 6.7 beta probe 2026-10-25: unchanged, with warning UAC0005 on AssemblyScanning.cs:10, and SmartPackerRegistry registered its packers on both entries with domain reload off. | Unity forum, Path to CoreCLR, 2026: Upgrade Guide (2026-03-26) and the CoreCLR updates of 2026-06-16 and 2026-10-05; AssemblyScanning.cs:10; Volumetrics PipelineUtils.cs:190-197 | | Assemblies.CurrentAssemblies; GetLoadedAssemblies; DomainUnload |
| U-043 | Core packing | `Spellbound.Core.Packing.Packer` | com.unity.serialization | Keeps | 6000.4 | Unity deprecated the package in 6.6 and suggests System.Text.Json, Odin Serializer or MemoryPack, none of which is a bit writer with stable hashes and deltas. 6.7 beta probe 2026-10-25: unsure, the copy pulls com.unity.serialization through Entities only and Core's packing does not reference it. | Unity forum, CoreCLR, Scripting and Serialization update (2026-06-16) | | Serialization package; deprecated; bit writer |
| U-050 | Operations on the command line | `Spellbound.Core.Pipeline.SpellboundRunCommand` | Unity Pipeline package (experimental) | Builds on | 6000.4; com.unity.pipeline 0.8.0-exp.1 | In 0.8.0-exp.1 the declaration attributes moved to Unity.Pipeline.Attributes and CommandRegistry became public; results are unwrapped for Task only, and player discovery is a reflection fallback not tested under stripping. 6.7 beta probe 2026-10-25: changed in words, Pipeline runs on 6.7 and the Unity command line asks every project to run unity pipeline upgrade for the old command names. | Unity Pipeline 0.8.0-exp.1 as installed: CHANGELOG.md, Runtime/Common/CommandRegistry.cs:285-335, Editor/TypeCacheCommandDiscovery.cs | com.unity.pipeline | CliCommand; Awaitable; link.xml; CommandRegistry |
| U-051 | set_serialized_field for authored content | | Pipeline set_serialized_field | Builds on | 6000.4; com.unity.pipeline 0.8.0-exp.1 | It is unsupported for managed references, anything outside the listed types throws, and run_script covers them. | Unity Pipeline 0.8.0-exp.1 as installed: Documentation~/commands/scripts.md, Editor/Commands/Scripts/SerializedPropertyConverter.cs:24-96 | com.unity.pipeline | set_serialized_field; managed reference; run_script |
| U-058 | Operation run | `Spellbound.Core.Operations.OperationRun` | UnityEngine.CustomYieldInstruction | Builds on | 6000.4 | An operation run is a custom yield instruction, so a Play Mode test can yield it. | OperationRun.cs:9 | | CustomYieldInstruction; keepWaiting |
| U-059 | Operation marker attributes | `Spellbound.Core.Operations.OperationAttribute` | UnityEngine.Scripting.PreserveAttribute | Builds on | 6000.4 | The three marker attributes derive from it, so a marked method, parser or renderer survives managed code stripping. | OperationAttribute.cs:8 | | PreserveAttribute; managed code stripping |
| U-060 | Operation and log settings | `Spellbound.Core.Operations.OperationSettings` | UnityEngine.Resources | Builds on | 6000.4 | OperationSettings and LogConfig load from a Resources folder when one is present. | OperationSettings.cs:23 | | Resources.Load; Resources folder |
| U-061 | Installed package lookup | `Spellbound.Core.Operations.Editor.EditorInstalledPackages` | UnityEditor.PackageManager.PackageInfo | Builds on | 6000.4 | Core.Editor finds a package's folder and version through PackageInfo. | EditorInstalledPackages.cs:28; EditorInstalledPackages.cs:43 | | PackageInfo; GetAllRegisteredPackages; FindForAssembly |
| U-062 | Seeded stream | `Spellbound.Core.Simulation.SeededStream` | Unity.Mathematics.Random, UnityEngine.Random | Keeps | 6000.4; com.unity.mathematics 1.3.3 | Core on the engine alone cannot use Mathematics at the 6000.3 floor, and the stream names its algorithm, SplitMix64 seeding xoshiro256** draws. | New in Unity 6.5 (manual/whats-new/unity65); SeededStream.cs | com.unity.mathematics | Random; built-in module |
| U-063 | Tick clock and its driver | `Spellbound.Core.Simulation.TickClock` | MonoBehaviour.FixedUpdate, DefaultExecutionOrder | Builds on | 6000.4 | The driver advances the clock at the earliest execution order and raises Stepped at the latest. | Unity 6000.3 script reference, DefaultExecutionOrder, and the manual page on the order of event functions; TickClockDriver.cs | | FixedUpdate; DefaultExecutionOrder; Time.fixedDeltaTime |
| U-064 | Up field | `Spellbound.Core.ReferenceFrames.UpField` | Unity.Collections.NativeArray in the engine module | Builds on | 6000.4; com.unity.collections 6.4.0 | The up field is a blittable struct a Burst job evaluates, so Core needs only the engine's native array and no Collections package. | UpField.cs; UpFieldTests.cs | com.unity.collections | NativeArray; Collections; module |

- Moved `Objects`, `ObjectData`, `ECS` and `ModuleContracts` to the assembly `Spellbound.Core.WorldObjects`, waiting for the WorldObjects package.
- Moved the console folder to the assembly `Spellbound.Core.Console`, which leaves Core when the operation console replaces it.
