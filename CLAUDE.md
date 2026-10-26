# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

SluMAN (formerly racman) is a Windows speedrun/practice tool for Sly Cooper games on PS3 and RPCS3. It connects to a running game, reads/writes memory, and provides features like position saving, input display, and autosplitter integration. It is a fork of [racman](https://github.com/MichaelRelaxen/racman).

## Build

This is a .NET Framework 4.7.2 WinForms project (x64 only). Open `SluMAN.sln` in Visual Studio and build with **Release|x64** configuration. Output lands in `SluMAN\bin\x64\Release\`.

To build from command line:
```
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe" "SluMAN\SluMAN.csproj" /p:Configuration=Release /p:Platform=x64
```

There are no automated tests.

## Versioning and releases

The version lives only in the release branch name. `AssemblyInfo.cs` stays at `0.0.0.0` in the repo, which makes local builds show as "dev build" and skip the update check (`func.IsDevBuild` / `func.VersionText`). Don't bump it by hand.

- `.github/workflows/build.yml` builds every PR and uploads the output as the `SluMAN` artifact. Builds from a `release/vX.Y.Z` branch get `X.Y.Z` stamped in.
- `.github/workflows/release.yml` runs when a `release/vX.Y.Z` PR is merged into master. It builds and creates a **draft** GitHub release `vX.Y.Z` with `SluMAN.zip` and generated notes, plus a draft `update-xml/vX.Y.Z` PR pointing `update.xml` at it. Nothing is announced until someone reviews the notes and publishes the draft.
- Publishing the release creates the tag and fires `release: published`: `publish-release.yml` adds the published notes to `CHANGELOG.html` (`.github/scripts/changelog.py`, the page the update dialog links to) on the `update-xml/vX.Y.Z` PR and merges it (installed copies poll `update.xml` on master), and `discord-release.yml` posts to Discord. Pre-releases skip the `update.xml` bump.
- Everything that ships must end up in `bin\x64\Release\` via the build (project Content items or the post-build XCOPYs), because the zip is that folder as-is.

## Architecture

### Connection layer (`SluMAN/Memory/`)

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

Each game form creates a `GameSession` (`GameSession.cs`), which owns the connection lifecycle: reconnecting after a game reset (off the UDP thread, finishing on the UI thread), disconnecting when the form closes, the always-on-top and autosplitter settings, the input display, Switch Game/Mode, and power off/reboot. Put shared form behaviour there, not in each form; forms add their own subscriptions in the `Reconnected` event.

Game addresses live in `offsets/<GAME>/<Game>Addresses.cs` (e.g. `Sly2Addresses.cs`), separate from the game class. Each instance of the address class is one supported game version (title ID, display name and every address), built in its `CreateVersions()`. `AttachPS3Form` asks each address class `IsSupportedGameId(titleId)` to pick the game, and the game class gets its addresses with `ForGame(titleId)`.

### Supported games and their folders

| Folder | Game |
|--------|------|
| `SLY1/` | Sly Cooper (NPUA80663) |
| `SLY2/` | Sly 2: Band of Thieves (NPHA80175, NPEA00342; NPUA80664 untested) |
| `SLY3/` | Sly 3: Honor Among Thieves (NPEA00343) |
| `SLY4/` | Sly Cooper: Thieves in Time (BCES01284 PAL disc, NPEA00429 PAL PSN; same addresses) |
| `offsets/BH/` | Bentley's Hackpack (stub, Sly franchise spinoff) |

Each supported game has two UI modes: **Practice Mode** (full trainer features) and **Speedrun Mode** (minimal UI, autosplitter, input display).

Practice windows are a fixed 660 wide with tabs (`PracticeTabs` with `fixedWidth`): the window's own controls on the Practice tab, in two columns of 312-wide group boxes, and tool forms docked on the others. The window's height follows the selected tab: the Practice tab's lowest control, or the tool form's own `ClientSize` height, so lay tools out to their content's height rather than padding them. A **View** menu (`PracticeTabs.AddViewMenu`) picks which tool tabs show and saves it in `config.txt`. Sly 1 Practice Mode (`Sly1Practice`, built in code) loads levels at an entrance through the reload block, has the shared `PositionEditor` on its Player tab (the transform is inside the entity, and the level title stands in for a map name: `PositionEditorLayout.transformInEntity` / `readEntity` / `readMapIndicator`), and edits each world's save data (levels' key/vault/sprint flags, counts, world flags) on its World States tab (`Sly1WorldStatesForm`). Sly 2 and Sly 3 Practice Mode load maps at spawn points from `data/s2_spawn_points.txt` / `data/s3_spawn_points.txt`, plus every spawn point a load has arrived at (`MapPicker.Remember`, kept in `sN_seen_spawn_points.txt` in the user data folder). They load run files from Practice Mode's Run Files tab (`RunFilesForm`); Speedrun Mode only has the autosplitter, always on top and the input display, like Sly 4's. Sly 3 and Sly 4 Practice Mode have a Camera tab with a free camera and camera paths (keyframe animations, `CameraPath`, saved per level or map in the user data folder's `camerapaths`, with Sly 3's in `camerapaths/sly3`); Sly 3's tab (`Sly3CameraForm`) is laid out like Sly 4's; its Hide HUD sets the HUD's scale X (`hudScaleX`) to 0 and No fog sets `fog` to 0, each putting back the value it had. Sly 3's free camera (`Sly3FreeCamera`) runs through a camera hook (`sly3.StartCameraHook`): the game's world matrix update (`worldMatrixUpdateFunction`, which copies every object's local matrix at +0x50 to its world matrix at +0x90 when +0x44 marks it changed) has its calls pointed at a stub, in padding between two ELF sections of the code segment, that first puts SluMAN's matrix into the driven camera. RPCS3's static interpreter fixes the kind of instruction at every address it found code at when the game loaded (even code that never runs), reads only the operands again, and has nothing past the code segment's end: only patch `bl` to `bl`, and only put new code in bytes inside the code segment that were never code. Without the hook (RPCS3 needs the Interpreter PPU decoder) it falls back to writing the camera and the copies the game draws from (`cameraDrawMatrixOffset`, `cameraDrawMatrix`) directly, which flickers. Sly 4 Practice Mode's Gadgets tab (`Sly4GadgetsForm`) sets the state byte each gadget has in the game manager's gadget array (`gadgetOffset`, one byte per gadget in `Sly4Gadgets.All`'s order, not bits as in Sly 2/3): 0 locked, 1 in ThiefNet, 3 bought, as buying it sets (0x2 is the bought bit). The tab has a checklist per character, ticked if bought, like Sly 2/3's: ticking sets 3, unticking a bought one sets 1. The game only reads the array on a load, so changes stay in the lists until Apply writes them and reloads (`Sly4Practice.ReloadForGadgets`: retry the checkpoint in a job, otherwise load the level again). It saves the gadgets each run file loads with in `config.txt` (`Sly4_Episode1_GadgetStates` and so on, through `Sly4Gadgets.LoadRunFile` / `SaveRunFile`). Sly 4's Run Files tab (`Sly4RunFilesForm`, Episodes 1–5, through `Sly4Practice.LoadRunFile`) goes to the hideout of the episode's first job and writes those gadgets; starting a job never resets gadgets. Every game's gadget table (`Sly4Gadgets.All`, and `AllGadgets` in `Sly2Gadgets` / `Sly3Gadgets`) gives each gadget the episode it's available from: the first episode whose run file may have it, as it's in ThiefNet when that episode starts (0 from the start). Saving a run file's gadgets in the Gadgets tab leaves out the ones not available yet and says which inline (`GadgetAvailability`); Sly 4 locks them in the list, Sly 2 and 3 untick them. Memory tools (the Memory and Memory Viewer tabs) belong to Practice Mode only; Speedrun Mode must not offer them.

### Autosplitter (`AutosplitterHelper.cs`)

Writes game state into a named memory-mapped file `"SluMAN-autosplitter"` (128 bytes addresses + 256 bytes config). LiveSplit autosplitter scripts read from this MMF. Games implement `IAutosplitterAvailable` to declare which addresses to expose.

### Lua mod scripting (`LuaAutomation.cs`)

Mods are loaded from `mods/<gameID>/` directories. Each mod can include a Lua automation script. NLua is used to run scripts. The Lua state receives `API` (the `IPS3API` instance), `Inputs` (button state), and timer-based `OnTick` / `OnLoad` / `OnUnload` hooks.

### Input decoding (`Inputs.cs`)

All Sly games normalize their raw input to the standard PS3 layout before storing in `Inputs.RawInputs` (`IGame.SubscribeSlyButtons` does this). Each `Inputs.Buttons` value is its bit in that mask: check a button with `Inputs.IsPressed(button)`, and use `Inputs.DecodeMask(int)` to get a `List<Inputs.Buttons>`.

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

1. Add `offsets/<GAME>/<Game>Addresses.cs`, shaped like `Sly2Addresses.cs` (no `IAddresses` interface needed)
2. Create a game class inheriting `IGame` in `offsets/<GAME>/` — implement `CheckInputs`, `SavePosition`, `LoadPosition`, and set `addr` from `ForGame(gameNameId)` in the constructor
3. Add Practice/Speedrun `Form` classes in a new `<GAME>/` folder
4. Route the game in `AttachPS3Form.IsSupportedTitle()` and `ShowGame()` with `<Game>Addresses.IsSupportedGameId()`

## Adding a version of a supported game

Only the game's `<Game>Addresses.cs` changes: add a `GameId…` constant, then in `CreateVersions()` copy the closest version with `CopyAs(titleId, displayName)`, set the addresses that differ, and add it to the dictionary. Attaching, reconnecting and Switch Game pick it up from there.
