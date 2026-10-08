# Coding Style & Patterns

This document describes the conventions found throughout this codebase. Follow them when adding or modifying code.

---

## Naming

| Kind | Convention | Example |
|------|------------|---------|
| Classes (forms, helpers) | PascalCase | `ModLoaderForm`, `AutosplitterHelper` |
| Game impl classes | lowercase | `sly2`, `sly3`, `sly1` |
| Address classes | PascalCase | `Sly2Addresses`, `Sly3Addresses` |
| Public methods | PascalCase | `SavePosition`, `LoadPosition` |
| Event handlers | `controlName_EventName` | `inputDisplayButton_Click`, `Sly2Practice_FormClosed` |
| Public fields | camelCase | `inputCheck`, `pid`, `coords` |
| Private fields | camelCase, no underscore prefix | `mapIndex`, `reloading` |
| Local variables | camelCase | `coordsAddress`, `bytes` |
| Constants | PascalCase | `PrefersAlwaysOnTopKey`, `mmfAddressBytes` |
| Enum values | camelCase | `l2`, `triangle`, `select` |
| Config file keys | camelCase string literals | `"loadPosCombo"`, `"savePosCombo"` |

Do **not** use `_` prefixes on private fields.

---

## Access modifiers

- `public` for anything a form or external class needs to call directly
- `private` for implementation details, helper methods, conversion utilities
- `static` heavily for utility classes (`func`, `Inputs`, `ConfigureCombos` static fields)
- `protected virtual` for base-class hooks with empty default bodies; override in game classes
- `public abstract` for methods every game class must implement (`CheckInputs`, `SavePosition`, `LoadPosition`)

Address/offset values: wrap raw values in a private nested `AddressValues` class; expose them through expression-bodied public properties on the outer class.

---

## Types and idioms

**Prefer explicit types over `var`:**
```csharp
string position = ...;
int pid = ...;
byte[] bytes = ...;
```

**String interpolation everywhere:**
```csharp
$"SluMAN v{Assembly.GetExecutingAssembly().GetName().Version} connected"
```

**Expression-bodied properties for address forwarding:**
```csharp
public uint inputOffset => values.inputOffset;
```

**LINQ method chaining (not query syntax):**
```csharp
bytes.Reverse().ToArray()
BitConverter.GetBytes(value).Take(size).Reverse().ToArray()
```

**Bit operations for controller masks:**
```csharp
if ((mask & (int)SlyButtons.cross) != 0) { ... }
```

**Ternary for simple config writes:**
```csharp
func.ChangeFileLines("config.txt", alwaysOnTopCheckBox.Checked ? "true" : "false", "alwaysOnTop");
```

**Pattern matching for API type checks:**
```csharp
if (api is Ratchetron r) { ... }
```

**Do not use:** async/await, null-conditional operators (`?.`), MVVM, generics (except collections), query-syntax LINQ.

---

## Exception handling

Use bare `catch` blocks to suppress errors that are expected and non-fatal:
```csharp
catch
{
    // Keep the ones that successfully loaded.
}
```

Show a `MessageBox` for user-facing failures:
```csharp
catch (Exception ex)
{
    MessageBox.Show($"Failed to save position: {ex.Message}", "Error",
        MessageBoxButtons.OK, MessageBoxIcon.Error);
}
catch
{
    MessageBox.Show("Please enter a valid number", "Error",
        MessageBoxButtons.OK, MessageBoxIcon.Error);
}
```

Utility methods that read config or parse values return a safe default on failure — do not re-throw or log.

---

## Byte order

PS3 is big-endian; the host is little-endian. Always `.Reverse().ToArray()` before passing to `BitConverter`:
```csharp
uint value = BitConverter.ToUInt32(bytes.Reverse().ToArray(), 0);
byte[] out = BitConverter.GetBytes(floatVal).Reverse().ToArray();
```

---

## Memory subscriptions

Set up subscriptions in the game class or form constructor. Return and store the subscription ID so it can be released on close.

```csharp
int subID = api.SubMemory(pid, addr.inputOffset, 4, (value) =>
{
    int mask = BitConverter.ToInt32(value.Reverse().ToArray(), 0);
    Inputs.RawInputs = ConvertSlyButtonsToStandardFormat(mask);
});
```

Release all subscription IDs on form close or disconnect via `api.ReleaseSubID(id)`. Collect IDs in a list when there are many.

---

## Threading

- Use `System.Windows.Forms.Timer` for periodic UI/input polling (16.67 ms interval = ~60 Hz).
- Use `Thread.Sleep()` in reconnection loops; no async/await.
- Protect memory-mapped file writes with a `Mutex`:
```csharp
private static Mutex writeLock = new Mutex();
writeLock.WaitOne();
// write
writeLock.ReleaseMutex();
```
- `LuaAutomationTimer` uses a `CallMutex` to prevent re-entrant Lua calls.

---

## Form structure

Constructor order:
1. `InitializeComponent()`
2. Apply saved preferences (`ApplySavedPreferences()` or equivalent inline reads)
3. Set up memory subscriptions
4. Register event handlers and timers

Preferences are persisted immediately when a checkbox changes via `func.ChangeFileLines()`. Preference keys are `private const string` fields at the top of the form class.

Child forms (InputDisplay, GadgetsWindow) are lazily created — check `null` or `IsDisposed` before opening. The parent tracks and closes them in the `FormClosed` handler.

Disconnect/reconnect pattern: `DisconnectGame()` stops timers and releases subs; reconnect loop polls with `Thread.Sleep(3000)` up to a fixed retry count, then re-establishes subs.

---

## Game class structure (IGame subclasses)

File layout order:
1. Static address reference (`public static GameAddresses addr`)
2. Instance fields (`mapIndex`, `speedrunMode`, data arrays)
3. Nested struct/enum definitions
4. Constructor (calls `base(api)`, initializes `addr` and data)
5. Autosplitter address enumeration (if `IAutosplitterAvailable`)
6. `SavePosition` / `LoadPosition` overrides
7. `CheckInputs` override
8. `SetupInputDisplayMemorySubsButtons` / `SetupInputDisplayMemorySubsAnalogs` overrides
9. Public gameplay methods (`LoadMap`, `LoadJob`, `SetCoins`, etc.)
10. Private conversion helpers (`ConvertSlyButtonsToStandardFormat`, etc.)

---

## Address/offset class structure

```
offsets/<GAME>/<game>.cs
```

Each file contains:
- A public address class (`Sly2Addresses`) with a private nested `AddressValues` struct
- A static dictionary mapping game ID strings to `AddressValues` instances
- Expression-bodied public properties forwarding to `values.*`
- Any nested enums for game-specific types (e.g. `LoadTypes`)
- The public game class inheriting `IGame` (if combined in one file)

Version management: if two regions share most addresses, clone the `AddressValues` and override only the differing fields.

---

## Comments

Write comments only when the **why** is non-obvious — a hardware constraint, a workaround, or a subtle invariant. Do not explain what the code does. Avoid multi-line comment blocks; a single line is enough.

Group related address constants with a short category header:
```csharp
// Inputs
// Pointers
// Cutscene skipping
```

`TODO:` comments are acceptable for known missing features.

---

## Static globals

`func.api` (the active `IPS3API`) and `AttachPS3Form.pid` / `AttachPS3Form.game` are global singletons accessible from any class. This is the established pattern — do not try to pass them by dependency injection.

---

## Mod system

`Mod` objects contain patch byte arrays and optional `LuaAutomation`. Before patching, the original bytes are backed up (stored in the mod object) so the patch can be reverted on unload. Mods live in `mods/<gameID>/` directories. The Lua state exposes `API` (the `IPS3API` instance) and `Inputs`.
