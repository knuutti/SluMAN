# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

SluMAN (formerly racman) is a Windows speedrun/practice tool for Sly Cooper games on PS3 and RPCS3. It connects to a running game, reads/writes memory, and provides features like position saving, input display, and autosplitter integration. It is a fork of [racman](https://github.com/MichaelRelaxen/racman).

## Build

This is a .NET Framework 4.7.2 WinForms project (x64 only). Open `SluMAN.sln` in Visual Studio and build with **Release|x64** configuration. Output lands in `RaCTrainer\bin\x64\Release\`.

To build from command line:
```
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe" "RaCTrainer\SluMAN.csproj" /p:Configuration=Release /p:Platform=x64
```

There are no automated tests.

## Versioning and releases

The version lives only in the release branch name. `AssemblyInfo.cs` stays at `0.0.0.0` in the repo, which makes local builds show as "dev build" and skip the update check (`func.IsDevBuild` / `func.VersionText`). Don't bump it by hand.

- `.github/workflows/build.yml` builds every PR and uploads the output as the `SluMAN` artifact. Builds from a `release/vX.Y.Z` branch get `X.Y.Z` stamped in.
- `.github/workflows/release.yml` runs when a `release/vX.Y.Z` PR is merged into master. It builds and creates a **draft** GitHub release `vX.Y.Z` with `SluMAN.zip` and generated notes. Nothing is announced until someone reviews the notes and publishes the draft.
- Publishing the release creates the tag and fires `release: published`: `publish-release.yml` commits the new version to `update.xml` on master (which installed copies poll), and `discord-release.yml` posts to Discord. Pre-releases skip the `update.xml` bump.
- Everything that ships must end up in `bin\x64\Release\` via the build (project Content items or the post-build XCOPYs), because the zip is that folder as-is.

## Architecture

### Connection layer (`RaCTrainer/Memory/`)

`IPS3API` is the abstract base for all PS3/emulator connections. Three implementations:
- **`Ratchetron`** — TCP connection to `ratchetron_server.sprx` loaded on the PS3 via webMAN; the primary/modern API (port 9671). The class name refers to the external SPRX project and should not be renamed.
- **`WebMAN`** — legacy HTTP-based API using webMAN MOD endpoints directly
- **`RPCS3`** — reads the local RPCS3 process memory directly (no network)

`AttachPS3Form` selects the API based on user choice (RPCS3 checkbox or old API checkbox), then calls `Attach()` which reads the game title ID and routes to the correct game form.

> **Full Ratchetron API reference:** [`.claude/ratchetron-api.md`](.claude/ratchetron-api.md) — covers the complete TCP/UDP protocol, all `IPS3API` method signatures, byte-order rules, subscription lifecycle, conditions, and known limits. Use the `/ratchetron-api` skill to load this context automatically when working with memory code.

### Game layer

`IGame` is the abstract base class for each supported game. Each game subclass holds an `IPS3API` reference and implements:
- `CheckInputs` — runs on a 60 Hz timer, reads controller state
- `SavePosition` / `LoadPosition` — game-specific coordinate save/restore

Game addresses live in `offsets/<GAME>/` as standalone classes (e.g. `sly2.cs` contains `Sly2Addresses` with separate `AddressValues` instances for each supported region/build).

### Supported games and their folders

| Folder | Game |
|--------|------|
| `SLY1/` | Sly Cooper (NPUA80663) |
| `SLY2/` | Sly 2: Band of Thieves (multiple regions) |
| `SLY3/` | Sly 3: Honor Among Thieves (NPEA00343) |
| `offsets/BH/` | Bentley's Hackpack (stub, Sly franchise spinoff) |

Each supported game has two UI modes: **Practice Mode** (full trainer features) and **Speedrun Mode** (minimal UI, autosplitter, input display).

### Autosplitter (`AutosplitterHelper.cs`)

Writes game state into a named memory-mapped file `"racman-autosplitter"` (128 bytes addresses + 256 bytes config). LiveSplit autosplitter scripts read from this MMF. Games implement `IAutosplitterAvailable` or `IAutosplitterWVariables` to declare which addresses to expose.

### Lua mod scripting (`LuaAutomation.cs`)

Mods are loaded from `mods/<gameID>/` directories. Each mod can include a Lua automation script. NLua is used to run scripts. The Lua state receives `API` (the `IPS3API` instance), `Inputs` (button state), and timer-based `OnTick` / `OnLoad` / `OnUnload` hooks.

### Input decoding (`Inputs.cs`)

`Inputs.DecodeMask(int)` converts a raw PS3 button bitmask into a `List<Inputs.Buttons>`. All Sly games normalize their raw input to the standard PS3 layout before storing in `Inputs.RawInputs`.

### Config

Settings are stored in `config.txt` (next to the executable) as `key = value` lines. Read/write via `func.GetConfigData()` and `func.ChangeFileLines()`. The file is created on first run.

### Static utility class (`func.cs`)

`func` is a static utility class with: hex/byte conversion helpers, config file I/O, memory read/write wrappers that delegate to `func.api`, and Win32 P/Invoke helpers for window enumeration.

## Coding style

Follow the conventions in [`.claude/coding-style.md`](.claude/coding-style.md). Key points:

- Explicit types preferred over `var`
- PascalCase methods, camelCase fields (no `_` prefix on private fields)
- Game implementation classes are lowercase (`sly2`, `sly3`)
- PS3 bytes are big-endian — always `.Reverse().ToArray()` before `BitConverter`
- Bare `catch` blocks for expected non-fatal errors; `MessageBox` for user-facing failures
- No async/await; no null-conditional `?.`; no LINQ query syntax

## Adding a new game

1. Add offset addresses in `offsets/<GAME>/` as a standalone class (no `IAddresses` interface needed)
2. Create a game class inheriting `IGame` in `offsets/<GAME>/` — implement `CheckInputs`, `SavePosition`, `LoadPosition`
3. Add Practice/Speedrun `Form` classes in a new `<GAME>/` folder
4. Add game title ID detection in `AttachPS3Form.Attach()`
