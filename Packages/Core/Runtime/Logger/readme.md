# Spellbound Logging

A compile-time stripped, sink-based logging system for Unity. Log calls are removed from the binary entirely when disabled — zero IL, zero cost. When enabled, output routes to any combination of sinks: Unity Console, file, in-game console, or your own.

## Setup

1. Create the config asset: **Create > Spellbound > Log Config**.
2. Place it in any `Resources/` folder as `LogConfig`.
3. Select a log level, enable your sinks, and click **Apply**.

## Usage

```csharp
Log.Info("Player connected successfully.");
Log.Warn($"No scriptable object found - fallback to default");
Log.Error($"Failed to load chunk at {coord}");
Log.Debug($"Tick {tick}: processing {entityCount} entities");
```

Output is in the form of Source file, method name, and line number:

```
[2026-04-17 14:32:01.123] [Error] [ChunkManager.AddChunk:84] Failed to load chunk at 0, 1, 0.
```

## Log Levels

| Level | Behavior |
|-------|----------|
| **Verbose** | Compiles everything. No `Log.Verbose()` method exists — this is a config-only setting. |
| **Debug** | Compiles Debug, Info, Warning, Error. |
| **Info** | Compiles Info, Warning, Error. |
| **Warning** | Compiles Warning, Error. |
| **Error** | Error only. All other calls stripped. Error can never be stripped. |
| **None** | Equivalent to Error. |

## How Stripping Works

Each method on `Log` is decorated with `[Conditional("SPELLBOUND_LOG_X")]`. When the symbol is absent, the C# compiler removes the call — including argument evaluation and string interpolation — from the IL. The Apply button in the config inspector writes the appropriate symbols via `PlayerSettings.SetScriptingDefineSymbols`.

## Per-Sink Filtering

Each sink has its own filter level dropdown in the config. This is a runtime gate layered on top of the compile-time gate.

Example: global set to **Debug**, File sink filtered to **Debug**, Unity Console filtered to **Warning**, Game Console filtered to **Error**. All calls compile, but each sink only receives what you want it to.

## Built-In Sinks

- **Unity Console** — routes to `Debug.Log` / `LogWarning` / `LogError`.
- **File** — writes to `Application.persistentDataPath` on a background thread. File rotation keeps the last 3 sessions.
- **Game Console** — routes to `ConsoleLogger.PrintToConsole` / `PrintError`.

## Creating a Custom Sink

Implement `ILogSink` as a public, top-level type with a parameterless constructor. It will be discovered automatically via reflection and appear in the config inspector. Nested and internal sinks, and sinks whose constructor takes arguments, are never discovered, so they stay out of the inspector and out of user config assets.

```csharp
public class TelemetrySink : ILogSink {
    public string DisplayName => "Example";

    public void Initialize(LogConfig config) {
        // Your initialization
    }

    public void Emit(LogLevel level, string source, string message, string member, int line,
        UnityEngine.Object context) {
        // Your example code
    }
}
```

Sinks with external dependencies that require constructor arguments should skip the parameterless constructor. They won't appear in the config inspector — register them manually in your own bootstrap:

```csharp
Log.AddSink(new MyComplexSink(apiKey, endpoint), config, LogLevel.Warning);
```

## Temporary Sinks

`Log.RemoveSink` unregisters a sink by instance and returns whether it was registered. `Log.AddScopedSink` returns a disposable that removes it again, so a sink can be attached for the life of a `using` block and nothing leaks into the rest of the session.

```csharp
var sink = new RecordingLogSink(LogLevel.Error);

using (Log.AddScopedSink(sink, config, LogLevel.Verbose)) {
    RunTheThingThatShouldFail();
    Assert.AreEqual(1, sink.CountOf(LogLevel.Error));
    Assert.IsTrue(sink.Contains(LogLevel.Error, "chunk load failed"));
}
```

`RecordingLogSink` keeps entries in memory instead of writing them anywhere, which is what lets a test assert that code reported an error. Its constructor takes a minimum level, so it is not reflection-discoverable and never appears in the config inspector. `Entries`, `Count`, `CountOf`, `Contains`, and `Clear` are the reading surface, and it is safe to emit into from background threads.

Removal does not dispose the sink. Sinks that hold resources, like `FileSink`, are still the caller's to dispose.

To mute sinks you do not hold a reference to, such as the ones `LogBootstrap` registered from config, use `Log.SuspendSinks`. It detaches everything and reattaches on dispose, leaving any sink registered during the suspension in place.

```csharp
using (Log.SuspendSinks())
using (Log.AddScopedSink(sink, config, LogLevel.Verbose)) {
    RunTheThingThatShouldFail();
    Assert.AreEqual(1, sink.CountOf(LogLevel.Error));
}
```

`Log.ClearSinks` drops every registration without restoring. `LogBootstrap` calls it before registering, so entering play mode with domain reload disabled re-registers rather than stacking a second copy of every sink, and it calls it again on returning to edit mode so play-mode sinks do not outlive the session. Outside play mode `LogBootstrap` keeps one `UnityConsoleSink` registered (`LogBootstrap.EditModeSink`), so a line logged from editor code reaches the Unity console with the context object the caller gave.
## Tallying Sink

`TallyingLogSink` keeps one entry per distinct message with a count instead of writing anything. Its constructor takes the maximum number of distinct entries, so it is not reflection-discoverable; register it yourself and `Drain` it on your own schedule. Messages arriving once the cap is reached are counted in `DroppedCount`, which `Drain(out int droppedCount)` returns and resets. It is safe to emit into from background threads.

```csharp
var tally = new TallyingLogSink(256);
Log.AddSink(tally, config, LogLevel.Warning);

foreach (var entry in tally.Drain())
    Send(entry.Level, entry.Source, entry.Message, entry.Count);
```

## Capturing the Unity Log

Tick **Capture Unity Log** on the config and every `Debug.Log`, warning, error and exception, including those raised by Unity and third-party packages, flows through the sinks with the source `Unity` and the stack trace appended to the message. Messages below the global level are dropped. `UnityLogCapture.Start` and `Stop` do the same from code. Output that a sink itself sends to the Unity console is recognised and never captured a second time.
