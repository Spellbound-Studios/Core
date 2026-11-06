# Spellbound.Core

Core is the foundation every Spellbound package builds on. It gives a game and the packages it installs one vocabulary: logging through `Log` and its sinks, packing values into bytes for a save or the wire, stable hashes and instance ids, rolls from a `SeededStream`, steps of time from a `TickClock`, poses in a reference frame, the viewers a world wakes things around, the intent ledger a world keeps, observations measured against a budget, operations that a person, a console, an agent or the Unity command line can call, and the validation of what a designer or an agent authors. Core draws nothing and spawns nothing; a package that does builds on it.

## The assembly layout

- `Spellbound.Core`, in `Runtime`: the vocabulary above, with `CoreKit` as its front door. It references the engine alone.
- `Spellbound.Core.Console`, in `Runtime/Console`: the in-game console and its command registry, over TextMeshPro, the Input System and uGUI.
- `Spellbound.Core.Pipeline`, in `Runtime/Pipeline`: the `spellbound_run` command of the Unity command line. It compiles only when a supported `com.unity.pipeline` is installed.
- `Spellbound.Core.Editor`, in `Editor`: editor only. It runs the validation of authored definitions, draws the Spellbound window and its pages, and adds the editor operations of the group `core`.
- `Spellbound.Core.Testing`, in `Testing`: the determinism and golden hash helpers other packages' tests use.
- `Spellbound.Core.Tests`, in `Tests/Editor`: the edit-mode tests, with the fixture assemblies they load beside them; the fixtures' operation groups are unlisted, so `core help` shows them only with `--all`. `Spellbound.Core.PlayModeTests`, in `Tests/Runtime`, runs in play mode.

## Starting with it

Every door is reached through `CoreKit`. A first operation line lists what the installed packages offer:

```csharp
var run = CoreKit.Operations.Run("core help", OperationCaller.Development(), ObservationContext.Discard);
Debug.Log(run.Result.Text);
```

The same line runs from the Unity command line as `unity command spellbound_run --line "core help"`; Pipeline 0.9 and 0.10 spell the command `unity command spellbound run`, and `unity command --query spellbound` finds it. A package declares its own operations with `[assembly: OperationGroup(name, job)]` and `[Operation(name, description, effect)]`, its observations once as static fields through `CoreKit.Observations`, and what it is in the `Identity.asset` at its root.

A type that goes into a save or onto the wire implements `IPacker`; `CoreKit.Packing.TryPack(in value, destination, out written)` packs it into a buffer the caller owns without allocating, and `CoreKit.Packing.TryUnpack(bytes, out value)` reads it back and refuses a short payload or trailing bytes.

## Samples

`Benchmarks` times the packer writing and reading a million ints, floats, vectors and quaternions. Import it from the Package Manager and drop its `PackingBenchmark` prefab into a scene.
