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