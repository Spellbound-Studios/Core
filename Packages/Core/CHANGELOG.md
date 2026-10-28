## [Unreleased]

- Operations in `Spellbound.Core.Operations`: one way for every package to declare what a person or an agent can call. An assembly carries `[assembly: OperationGroup(name, job)]`, a public static method on a public static class carries `[Operation(name, description, effect)]` with an optional `Audience` and `Availability`, and each parameter may carry `[Argument(description)]`. The three marker attributes derive from `UnityEngine.Scripting.PreserveAttribute`, so a marked method, parser or renderer survives managed code stripping at every level with no extra attribute.
- `OperationCatalog` finds the groups and operations of every marked assembly at first use, merges a package's second assembly into its group, keeps the first of two operations with one name and the first of two groups with one name and a different job, and gives every operation a stable `Identifier` (FNV-1a of `group.name`). Instances register live operations with `Register` and leave with `Unregister`.
- `OperationHost` runs a line: `Run(line, caller)`, `Run(line, caller, placeholders)`, `RunAll(lines, caller)` and `Run(invocation, caller)`. It tokenises, fills `{placeholders}` after tokenising, resolves a group or a unique short name, binds named and positional arguments with defaults, checks availability, the caller's audience and the effect gate (`Destroys` needs `--confirm`; `--dry-run` answers `previewed:`), invokes, renders, cuts long results and records history, per-operation statistics and an optional trace through Core's logger.
- `OperationRun` derives from `CustomYieldInstruction`, completes now or when a returned `Task` completes, and exposes `Result`, `Results`, progress `Lines` and `AsTask()`. `OperationResult` carries a status (`succeeded`, `invalid`, `failed`, `refused`, `previewed`) as the first word of its text, a severity and the returned value. `OperationTable` renders aligned columns. `OperationException(message, hint)` is the refusal an operation raises.
- Argument parsers: integers, numbers, booleans, enums with legal values echoed, vectors, quaternions, colours, lists and arrays, `scene:/Path` for scene objects, `asset:` references in the editor, and any consumer type through `IArgumentParser<T>` or `IArgumentTypeParser` marked `[ArgumentParser]`. Result renderers through `IResultRenderer` marked `[ResultRenderer]`.
- `OperationSettings`, an optional asset under `Resources`: a trace toggle, disabled groups, the audiences the command line bridge holds, the maximum line length, result lines and history entries.
- Built-in operations in the group `core`: `help`, `about`, `history`, `stats`. `CoreKit.Operations` is the default host, reset on play mode entry.
- `Spellbound.Core.Pipeline`, the Unity command line bridge: one command, `spellbound_run`, that runs one operation line and returns its text, compiled only when `com.unity.pipeline` is installed inside a range the assembly definition names (0.8 and 0.9 today, one declaration file per range). A line that does not bind throws `ArgumentException`, a failed or refused call throws `InvalidOperationException`, so the CLI reports each as a failure with the status word first. The caller holds the audiences of `OperationSettings.bridgeAudiences`. Core.Editor writes one console line when Pipeline is installed outside every range.
- The agent context: `AGENTS.md` at the package root with the headings Purpose, Front door, Rules, Not for, Gaps and Ledger; `GeneratedBlock` for delimited, hashed text inside it; `core context` (the installed packages, or one package's file with its operation and bridge door blocks refreshed from the live catalog), `core context-install --target Skill|Agents|Claude` (the stub an agent reads first), `core ledger` (every package's Ledger rows), and `core about` naming the installed Spellbound packages with their versions in the editor through `InstalledPackages.Source`. `BridgeDoors` finds a bridge assembly's second door by its name.
- `scripts/bridge-fixture.sh` drives a throwaway project through the Unity CLI against a given editor and Pipeline version and asserts the bridge's answers and exit codes.
- The authoring contract in `Spellbound.Core.Authoring`: `IAuthoredDefinition.Validate(ValidationReport)`, `ValidationReport` and `ValidationFinding` (a code `<package>.<kebab-name>`, a severity, a subject path, a location, a message, a hint, named hints and a fix code; one line per finding, code first, errors before warnings before information).
- The check seam in `Spellbound.Core.Authoring.Editor`: `IDefinitionCheck<T>` with `Check` and `PreviewLines`, `RegisteredDefinitionCheckAttribute(name)` with `RunsWhileEditing`, and `DefinitionCheckRegistry`, which finds the classes through `TypeCache` (the marked-assembly scan is the fallback source), reads `T` from the interface, sorts by name and keeps the first of two checks with one name over one type.
- `DefinitionValidationRunner` validates one asset, a folder, a package folder (`Packages/<id>` or `Packages/<Name>`), the drafts folder or everything on the contract: the type's own `Validate` first, then every registered check that applies, each timed, a thrown validator or check reported as `core.validation-threw` or `core.check-threw`. `ValidationRun` carries the assets, the `checks:` entries, the merged report and the timings; `DefinitionReportCache` keeps each asset's last run.
- `DefinitionPreviewer<T>` and `DefinitionPreviewerRegistry`; `DefinitionDrafts` (`Create`, `List`, `TryApprove` through `AssetDatabase.MoveAsset`, so the GUID is kept) with `OperationSettings.draftsFolder` (default `Assets/Drafts`); `ProjectPaths` confines written paths to `Assets` and `Library/Spellbound` and read paths to those and `Packages`.
- Operations in the group `core`: `validate [--path] [--drafts] [--timings]` (`failed:` when an error was found, the `checks:` line second), `preview --target`, `drafts`, and `approve --target --to`, which needs the audience `approver` and is refused while the draft has an error finding.
- The inspector banner: a UI Toolkit element in the Inspector window showing the selected definition's cached report with a Run checks button; an asset with no report is validated on the next editor update, and a saved definition is revalidated after the save.
- `PackageIdentity` in `Spellbound.Core.Packages.Editor`, the type of the `Identity.asset` at a package's root: kind, layer, status, job, front door, what it must not own, hot path, deterministic output, its simulation assemblies, its machine-matched jobs and its skeleton version, with `JudgmentFields` naming the fields a code owner reviews. It implements the authoring contract (`core.identity-*` findings), so `core validate` and the inspector banner report it; `PackageIdentity.Find(folder)` loads a package's identity. Core ships its own `Identity.asset`.
- `PackageIdentityInspector`, the identity's inspector in UI Toolkit, grouped by what the package is, what it must not own, how it is checked and its skeleton, with every judgment field marked and the identity's own report beneath.
- The Spellbound window shell in `Spellbound.Core.Window.Editor`: `SpellboundWindow` at Window, Spellbound; a page is a class deriving from `WindowPage` and marked `[WindowPage(title, order)]`, found by `WindowPages` through `TypeCache` (public classes only) and drawn by `WindowShell` in plain UI Toolkit with `sb-window` classes. Core registers no page of its own yet.
- `LogSettingsInspector` replaces `LogConfigEditor`: the log settings drawn in UI Toolkit and bound to the asset, and Apply writing the level's `SPELLBOUND_LOG_*` defines for the selected build target and naming them under the button. `LogSinkDiscovery.Discover` lists the public top-level sinks with a parameterless constructor through `TypeCache` as `LogSinkDescription` values, and `AppendMissing` adds the ones a `LogConfig` lacks, disabled, once when the inspector opens. `LogLevelDefines` gives a level's defines (`For`), merges them into a target's defines keeping every other define (`Merge`), and lists the levels whose calls compile in (`CompiledLevels`), which are a sink's filter choices.
- The operations block `core context` writes into an `AGENTS.md` names its package by id alone, without the version, so a release that only moves the version in `package.json` leaves the block current.

## [1.1.9] - 10/18/2026

- `UnityLogCapture` routes `Debug.Log`, warnings, errors and exceptions through the Spellbound sinks, enabled per `LogConfig` with **Capture Unity Log**. Console output produced by a sink is never captured back.
- `TallyingLogSink` keeps one entry per distinct message with a count and a cap on distinct entries; `Drain` returns `LogMessageTally` records and resets.

## [1.1.8] - 9/7/2026

- `WeightedPool<T>` renamed to `WeightedTableAsset<T>`. It is the asset form of a `WeightedTable<T>`, and the old name collided with `ObjectPool`.

## [1.1.7] - 9/7/2026

- `WeightedTable<T>` in `Spellbound.Core.Sampling`: a serializable weighted list with a nothing slot. One roll through `PickIndex` or `TryPick`, driven by any random source in `[0, TotalWeight)`, resolves by binary search over cumulative weights built once per table. `Sample` draws several picks with or without replacement. `Define` builds a table in code.
- `WeightedPool<T>`: a ScriptableObject holding one `WeightedTable<T>` for tables shared between assets.

## [1.1.6] - 8/2/2026

- `Packer` reads and writes `sbyte` through `WriteSByte` and `ReadSByte`, sharing the single-byte two's complement layout of `WriteByte`.
- `Packer` test coverage extended to every read and write pair, the bitwise variants, the smart packer helpers, and `BuildPayload`.
- `ObjectParent.GetNextInstanceIndex` advances a per-chunk cursor instead of rescanning from the seed count on every call, so placement cost stays flat as a chunk fills. Freed indices below the cursor are not reused within a chunk session.
- `ObjectParent.StaticEntityDistanceQuery` skips evaluation until the POV has moved 4m or the chunk's static entity count has changed, and returns whether it evaluated. Surface promotion and demotion can lag movement by up to 4m, well inside the default 50/70 interaction band.

## [1.1.5] - 8/1/2026

- `Log.ClearSinks` and `Log.SuspendSinks` mute sinks the caller does not hold a reference to. `LogBootstrap` now clears before registering and on returning to edit mode, so sinks no longer stack when domain reload is disabled.

## [1.1.4] - 8/1/2026

- Log sinks can now be unregistered with `Log.RemoveSink` or scoped to a `using` block with `Log.AddScopedSink`.
- `EntityPrefabRegistryAuthoring` takes a `PresetBakeManifest`, letting the prefab registry rebake when presets are added, removed, or edited.
- `TransformData.RotAsQuaternion` rebuilds rotation in XYZ euler order, matching how the constructors capture it. Composed rotations no longer come back skewed.
- `RecordingLogSink` captures log entries in memory for assertions, and sink discovery now ignores non-public types so test sinks no longer appear in the Log Config inspector.
- `ResourceRegistry<TEntry>` discovers registry entries under a Resources folder and adds the lazy load, name index, and per-entry validation that every registry was hand-rolling. `PresetRegistry` now sits on it; a failed load clears the registry and reports again on the next access instead of leaving it half populated.

## [2.0.0] - 4/18/2025

### Second Release

#### Major overhaul of all sections belonging to Core. The main focus for this 2.0.0 release was to manage integration across all new libraries and the necessary abstractions needed for each of them. Core remains a UnityEngine-only dependency and should be installed alongside any and all Spellbound Packages.

- Console Added
- Logger Added
- Modules Added
- Object Handling Added
- Objects Added
- Packing Added
- Tooling Added
- Editor Scripts and Tooling Added

## [1.0.0] - 4/11/2025

### First Release

- Package to git
- Package import into other Unity projects
