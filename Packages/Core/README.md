# README

[Documentation](https://spell-bound-studios.gitbook.io/spellbound-docs/)

# Introduction

---

## Console Overview

The Spellbound Console System provides a powerful command-line interface for Unity games. It supports three types of
commands:

1. **ICommand Classes** - Self-contained command implementations (e.g., `help`, `clear`, `list`)
2. **Utility Commands** - Static methods that can be called directly (e.g., `terraform_flatten`)
3. **Preset Commands** - Methods that operate on ObjectPresets with specific modules (e.g., `spawn sword`)

### Key Features

- ✅ Automatic command discovery via attributes
- ✅ Type-safe parameter parsing (int, float, bool, Vector3, Vector2, enums, etc.)
- ✅ Command aliases
- ✅ Command history navigation (up/down arrows)
- ✅ Extensible architecture
- ✅ Clean separation of concerns

---

## Quick Start

### 1. Add ConsoleController to Your Scene

1. Create a Canvas with a UI setup for the console
2. Add `ConsoleController` component
3. Assign required references:
    - `inputField` - TMP_InputField for user input
    - `outputText` - TextMeshProUGUI for output display
    - `scrollRect` - ScrollRect for scrolling
    - `contentContainer` - RectTransform for content sizing

### 2. Wire Up Input

```csharp
// In your input action asset or input manager:
consoleToggleAction.performed += consoleController.OnTogglePerformed;
consoleHistoryUpAction.performed += consoleController.OnHistoryUpPerformed;
consoleHistoryDownAction.performed += consoleController.OnHistoryDownPerformed;
```

### 3. Start Using Commands

Open the console and try:

```
> help
> list
> clear
```

---

## Command Types

### ICommand Classes

Best for: Standalone utility commands that don't need external context.

**Examples:** `help`, `clear`, `list`, custom help commands

**Characteristics:**

- Self-contained logic
- Support aliases
- Auto-registered via `[ConsoleCommandClass]` attribute
- Implement `ICommand` interface

### Utility Commands

Best for: Static helper methods, debug utilities, system commands.

**Examples:** Terrain manipulation, stats display, debug toggles

**Characteristics:**

- Must be static methods
- Decorated with `[ConsoleUtilityCommand]`
- No preset/target required
- Called directly: `commandname arg1 arg2`

### Preset Commands

Best for: Commands that operate on game objects/presets.

**Examples:** `spawn`, `delete`, `modify`

**Characteristics:**

- Can be static or instance methods
- Decorated with `[ConsolePresetCommand]`
- Require a preset target: `commandname targetname [args]`
- Only called if preset has required module type

---

## Creating Commands

### Creating an ICommand Class

```csharp
using Spellbound.Core.Console;

[ConsoleCommandClass("mycommand", "mc", "mycmd")]  // Name + aliases
public class MyCommand : ICommand {
    public string Name => "mycommand";
    public string Description => "Does something useful";
    public string Usage => "mycommand [arg]";

    public CommandResult Execute(string[] args) {
        if (args.Length == 0)
            return CommandResult.Fail("Missing argument");

        var result = DoSomething(args[0]);
        return CommandResult.Ok($"Success: {result}");
    }

    private string DoSomething(string arg) {
        return $"Processed {arg}";
    }
}
```

**Usage in console:**

```
> mycommand test
> mc test          (alias)
> mycmd test       (alias)
```

### Creating a Utility Command

```csharp
using Spellbound.Core.Console;
using UnityEngine;

public static class MyUtilities {
    
    [ConsoleUtilityCommand("setgravity", "Sets physics gravity")]
    public static void SetGravity(float x, float y, float z) {
        Physics.gravity = new Vector3(x, y, z);
        Debug.Log($"Gravity set to ({x}, {y}, {z})");
    }

    [ConsoleUtilityCommand("resetgravity", "Resets gravity to default")]
    public static void ResetGravity() {
        Physics.gravity = new Vector3(0, -9.81f, 0);
        Debug.Log("Gravity reset to default");
    }

    [ConsoleUtilityCommand("timescale", "Sets time scale")]
    public static void SetTimeScale(float scale = 1f) {
        Time.timeScale = Mathf.Clamp(scale, 0f, 10f);
        Debug.Log($"Time scale set to {Time.timeScale}");
    }
}
```

**Usage in console:**

```
> setgravity 0 -20 0
> resetgravity
> timescale 0.5
> timescale          (uses default: 1)
```

### Creating a Preset Command

```csharp
using Spellbound.Core.Console;
using UnityEngine;

public class GameObjectSpawner : MonoBehaviour {
    
    [ConsolePresetCommand("spawn", typeof(ConsoleModule))]
    public void SpawnObject(string presetUid, Vector3 position, Quaternion rotation, float scale) {
        var preset = presetUid.ResolvePreset();
        
        // Your spawn logic here
        var obj = Instantiate(preset.prefab, position, rotation);
        obj.transform.localScale = Vector3.one * scale;
        
        Debug.Log($"Spawned {preset.objectName} at {position}");
    }

    [ConsolePresetCommand("delete", typeof(ConsoleModule))]
    public void DeleteObject(string presetUid) {
        // Your delete logic here
        Debug.Log($"Deleted {presetUid}");
    }
}
```

**Setup:**

1. Your ObjectPreset must have a `ConsoleModule` attached
2. Set `autoRegister = true` on the ConsoleModule
3. Place the preset in a Resources folder

**Usage in console:**

```
> spawn sword
> spawn sword 5     (spawns 5 swords)
> delete sword
```

### Creating a Custom Help Command

```csharp
using System.Text;
using Spellbound.Core.Console;

[ConsoleCommandClass("terraform", "tf")]
public class TerraformHelpCommand : ICommand {
    public string Name => "terraform";
    public string Description => "List all terraform commands";
    public string Usage => "terraform";

    public CommandResult Execute(string[] args) {
        // Get all utility commands from a specific class
        var commands = AttributeCommandRegistry.GetUtilityCommandsByClass(typeof(TerrainSystem));

        var sb = new StringBuilder();
        sb.AppendLine("=== Terraform Commands ===");
        sb.AppendLine();

        foreach (var (commandName, description) in commands)
            sb.AppendLine($"{commandName,-25} {description}");

        return CommandResult.Ok(sb.ToString());
    }
}
```

---

## Architecture

### Core Components

```
CommandRegistry
├── Manages ICommand implementations
├── Routes commands to appropriate handlers
└── Provides ExecuteCommand() entry point

AttributeCommandRegistry
├── Discovers methods with [ConsoleUtilityCommand]
├── Discovers methods with [ConsolePresetCommand]
├── Handles method invocation and parameter parsing
└── Caches method instances

PresetResolver
├── Maps preset names to UIDs
├── Scans Resources for ObjectPresets with ConsoleModule
└── Provides TryResolvePresetUid()

ConsoleCommandRouter
├── Routes preset commands to handler methods
├── Checks module type requirements
├── Handles quantity multiplier
└── Provides execution context (position, rotation, etc.)

ConsoleController
├── UI management
├── Input handling
├── Command execution
└── Output display
```

### Command Flow

```
User Input: "spawn sword 5"
     ↓
ConsoleController.ExecuteCommand()
     ↓
CommandRegistry.ExecuteCommand()
     ↓
[Checks ICommand] → Not found
     ↓
[Checks Utility Command] → Not found
     ↓
[Checks Preset Command] → Found!
     ↓
ConsoleCommandRouter.RouteCommand()
     ↓
PresetResolver.TryResolvePresetUid("sword")
     ↓
AttributeCommandRegistry.TryGetPresetHandler("spawn", typeof(ConsoleModule))
     ↓
Invokes method 5 times
     ↓
Returns success
```

---

## API Reference

### CommandResult

Result object returned by all commands.

```csharp
// Success
return CommandResult.Ok();
return CommandResult.Ok("Success message");

// Failure
return CommandResult.Fail("Error message");
```

### ConsoleLogger

Static utility for printing to console from anywhere in your code.

```csharp
ConsoleLogger.PrintToConsole("Info message");
ConsoleLogger.PrintError("Error message");

if (ConsoleLogger.IsInitialized) {
    // Safe to use
}
```

### AttributeCommandRegistry

Query utility commands programmatically.

```csharp
// Get all utility commands from a class
var commands = AttributeCommandRegistry.GetUtilityCommandsByClass(typeof(MyClass));

// Get by class name
var commands = AttributeCommandRegistry.GetUtilityCommandsByClassName("MyClass");

// Check if utility command exists
bool exists = AttributeCommandRegistry.HasUtilityCommand("mycommand");

// Check if preset command exists
bool exists = AttributeCommandRegistry.HasPresetCommand("spawn");
```

### PresetResolver

Query registered presets.

```csharp
// Resolve preset name to UID
if (PresetResolver.TryResolvePresetUid("sword", out string uid)) {
    var preset = uid.ResolvePreset();
}

// Get all preset names
var names = PresetResolver.GetAllPresetNames();

// Get count
int count = PresetResolver.GetPresetCount();
```

---

## Advanced Usage

### Parameter Types Supported

Utility and preset commands automatically parse these types:

- `string`
- `int`
- `float`
- `double`
- `bool` (supports: true/false, 1/0)
- `byte`
- `Vector3` (consumes 3 args: x y z)
- `Vector2` (consumes 2 args: x y)
- `List<T>` (consumes all remaining args)
- Any `enum` type

**Example:**

```csharp
[ConsoleUtilityCommand("moveobject")]
public static void MoveObject(string name, Vector3 position, bool instant = false) {
    // Called as: moveobject player 10 5 0 true
    //   name = "player"
    //   position = Vector3(10, 5, 0)
    //   instant = true
}
```

### Default Parameters

Methods can use default parameter values:

```csharp
[ConsoleUtilityCommand("damage")]
public static void ApplyDamage(float amount, string damageType = "physical") {
    // damage 50         → amount=50, damageType="physical"
    // damage 50 fire    → amount=50, damageType="fire"
}
```

### Preset Command Context

Preset commands receive context from the system:

```csharp
[ConsolePresetCommand("spawn", typeof(ConsoleModule))]
public void SpawnObject(string presetUid, Vector3 position, Quaternion rotation, float scale) {
    // presetUid - automatically provided
    // position  - from crosshair raycast (configurable via ConsoleModule.spawnLocation)
    // rotation  - Quaternion.identity
    // scale     - 1.0f
}
```

### Error Handling

```csharp
public CommandResult Execute(string[] args) {
    try {
        DoSomethingRisky();
        return CommandResult.Ok("Success!");
    }
    catch (Exception ex) {
        return CommandResult.Fail($"Failed: {ex.Message}");
    }
}
```

### Console Visibility Events

```csharp
void Start() {
    var console = FindObjectOfType();
    console.OnVisibilityChanged += OnConsoleVisibilityChanged;
}

void OnConsoleVisibilityChanged(bool isVisible) {
    if (isVisible) {
        // Disable player input
        playerInput.Disable();
    } else {
        // Re-enable player input
        playerInput.Enable();
    }
}
```

### Manual Command Registration

```csharp
// Register a command instance manually
var myCommand = new MyCommand();
CommandRegistry.Instance.Register(myCommand, "alias1", "alias2");

// Unregister
CommandRegistry.Instance.Unregister("mycommand");
```

### Programmatic Command Execution

```csharp
// Execute from code
var result = CommandRegistry.Instance.ExecuteCommand("help");
if (result.Success) {
    Debug.Log(result.Message);
}
```

---

## Best Practices

### 1. Choose the Right Command Type

- **ICommand** for standalone utilities (help, clear, stats display)
- **Utility Commands** for static helper methods (debug toggles, system commands)
- **Preset Commands** for object manipulation (spawn, delete, modify)

### 2. Provide Good Descriptions

```csharp
// ✅ Good
[ConsoleUtilityCommand("setfov", "Sets camera field of view (30-120)")]

// ❌ Bad
[ConsoleUtilityCommand("setfov", "Sets FOV")]
```

### 3. Use Default Parameters for Optional Args

```csharp
// ✅ Good - allows: spawn sword  OR  spawn sword 5
public void Spawn(string presetUid, int quantity = 1)

// ❌ Bad - always requires quantity
public void Spawn(string presetUid, int quantity)
```

### 4. Validate Input

```csharp
[ConsoleUtilityCommand("setvolume")]
public static void SetVolume(float volume) {
    volume = Mathf.Clamp01(volume);  // ✅ Clamp to valid range
    AudioListener.volume = volume;
}
```

### 5. Group Related Commands

```csharp
// ✅ Good - all audio commands in one class
public static class AudioCommands {
    [ConsoleUtilityCommand("volume")] public static void SetVolume(float v) { }
    [ConsoleUtilityCommand("mute")] public static void Mute() { }
    [ConsoleUtilityCommand("unmute")] public static void Unmute() { }
}
```

### 6. Create Help Commands for Command Groups

```csharp
// ✅ Provide a way to discover related commands
[ConsoleCommandClass("audio")]
public class AudioHelpCommand : ICommand {
    public CommandResult Execute(string[] args) {
        var commands = AttributeCommandRegistry.GetUtilityCommandsByClass(typeof(AudioCommands));
        // Format and return
    }
}
```

---

## Troubleshooting

### "Unknown command" for utility commands

**Problem:** Utility command not found  
**Solution:** Ensure method is `static` and has `[ConsoleUtilityCommand]` attribute

### "No handler found" for preset commands

**Problem:** Preset command not routing  
**Solution:**

1. Check preset has `ConsoleModule` with `autoRegister = true`
2. Verify preset is in a Resources folder
3. Ensure method has `[ConsolePresetCommand]` with correct module type

### Commands not auto-registering

**Problem:** Commands don't show up  
**Solution:**

1. Ensure `CommandRegistry.Instance.AutoRegisterCommands()` is called (done in ConsoleController.Awake)
2. Check that AttributeCommandRegistry initializes before scene load
3. Verify namespace is `Spellbound.Core.Console`

### Parameter parsing errors

**Problem:** "Invalid arguments" errors  
**Solution:**

1. Check parameter types are supported
2. Ensure correct number of arguments
3. Use correct format for Vector3/Vector2 (space-separated)

---

## Support

For questions, issues, or feature requests, please contact Spellbound Studio or post in the Discord.

**Version:** 1.0  
**Unity Version:** 2021.3+  
**Dependencies:** TextMeshPro, Unity Input System

---

## License

Copyright 2025 Spellbound Studio Inc.