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

Each game form creates a `GameSession` (`GameSession.cs`), which owns the connection lifecycle: reconnecting after a game reset (off the UDP thread, finishing on the UI thread), disconnecting when the form closes, the always-on-top and autosplitter settings, the input display, Switch Game/Mode, power off/reboot, and every Practice window's Tools menu (`FillPracticeToolsMenu`; a game's own items, such as Language, go before its separator through `AddGameMenuItem`). Put shared form behaviour there, not in each form; forms add their own subscriptions in the `Reconnected` event.

Practice windows are uniform across the games: every shared feature in the same place and under the same name, game-specific ones around them. A new game's Practice window follows the same slots and tabs:

- **Practice tab**, built in code with `PracticeLayout` (no Designer files). Left column, in slot order: Level / Map (place, entrance or spawn point, Load), Job (job, checkpoint or goal, Load), Reload (Reload / Retry Checkpoint / Restart Job, Skip Cinematic, game extras), Saved Positions (slot, Save, Load). Right column: Game (status rows: level or map, job, checkpoint or goal, then the game's own), Utility (value text box + Set rows: coins, health, ...), Toggles. A game without a group leaves it out and the ones below move up. Toggles go in this order, in two sub-columns: Invulnerability, Infinite Health, Infinite Gadget Power, Undetectable, Freeze Game Clock / Slow Motion, Fly Mode, Infinite Jump, Auto-skip Cutscenes, the game's own toggles, Always On Top last. Shared labels: "Load" on every load button, "Invulnerability", "Auto-skip Cutscenes", "Skip Cinematic", "Retry Checkpoint", "Toggles". A feature another game has and this one lacks goes in its slot as a disabled placeholder (`PlaceholderToggle`, `PlaceholderButton`, `PlaceholderSetter`). Fly is R1 up / L1 down everywhere.
- **Tabs**, in this order: Practice, Player (the shared `PositionEditor`, through the game's `GetPositionEditorLayout`), Camera (the shared `CameraForm`, through the game's `GetCameraTabLayout`: a `CameraTabLayout` with its `IFreeCamera`, path folder, place reader, Hide HUD / No fog, speed and field of view ranges, and optionally a far distance; the Practice window makes its `CameraScriptApi` from that layout, the local HTTP API on 127.0.0.1:9675 that scripts drive the free camera through (reference for scripts: [`.claude/camera-api.md`](.claude/camera-api.md)), and the tab shows it from `CameraScriptApi.Layout`), then the game's own tabs (Gadgets, then Run Files: Sly 2 / 3's Gadgets on `GadgetsForm`, which adds the run file gadgets and Discard Changes / Save / Save and Reload bar, Sly 4's `Sly4GadgetsForm` a column per character with Apply / Discard; Run Files on `RunFilesForm`; Jobs, World States, ...), then Memory and Memory Viewer. `PracticeTabs.AddViewMenu` takes the tabs shown by default.
- **Tools menu**: `GameSession.FillPracticeToolsMenu`, with the game's own items added through `AddGameMenuItem`.

Register the window's own FormClosing handler before creating its `GameSession`, whose handler disconnects. The decisions behind the layout, its phases and the features some games still lack are in [`.claude/ui-uniform-plan.md`](.claude/ui-uniform-plan.md); follow it when changing a Practice window.

Game addresses live in `offsets/<GAME>/<Game>Addresses.cs` (e.g. `Sly2Addresses.cs`), separate from the game class. Each instance of the address class is one supported game version (title ID, display name and every address), built in its `CreateVersions()`. `AttachPS3Form` asks each address class `IsSupportedGameId(titleId)` to pick the game, and the game class gets its addresses with `ForGame(titleId)`.

### Supported games and their folders

| Folder | Game |
|--------|------|
| `SLY1/` | Sly Cooper (NPUA80663) |
| `SLY2/` | Sly 2: Band of Thieves (NPHA80175, NPEA00342; NPUA80664 untested) |
| `SLY3/` | Sly 3: Honor Among Thieves (NPEA00343) |
| `SLY4/` | Sly Cooper: Thieves in Time (BCES01284 PAL disc, NPEA00429 PAL PSN; same addresses) |
| `BH/`, `offsets/BH/` | Bentley's Hackpack (NPEA00427 PAL PSN; runs on the Sly 4 code, see below) |

Each supported game has two UI modes: **Practice Mode** (full trainer features) and **Speedrun Mode** (minimal UI, autosplitter, input display).

Practice windows are a fixed 660 wide with tabs (`PracticeTabs` with `fixedWidth`): the window's own controls on the Practice tab, in two columns of 312-wide group boxes, and tool forms docked on the others. The window's height follows the selected tab: the Practice tab's lowest control, or the tool form's own `ClientSize` height, so lay tools out to their content's height rather than padding them. A **View** menu (`PracticeTabs.AddViewMenu`) picks which tool tabs show and saves it in `config.txt`. Sly 1 Practice Mode (`Sly1Practice`, built in code) loads levels at an entrance through the reload block, has the shared `PositionEditor` on its Player tab (the transform is inside the entity, and the level title stands in for a map name: `PositionEditorLayout.transformInEntity` / `readEntity` / `readMapIndicator`; Sly 4's Player tab is the same editor on the Havok body, through `writePosition` (`sly4.Teleport`), `readVelocity`, `heightAxis` 1 and `getFlySpeed`, with its fly run by `Sly4FreeMove` from the Practice tab's Fly Mode), and edits each world's save data (levels' key/vault/sprint flags, counts, world flags) on its World States tab (`Sly1WorldStatesForm`). Its Powerups tab (`Sly1PowerupsForm`, research in [`.claude/sly1-powerups-research.md`](.claude/sly1-powerups-research.md)) edits the game save's unlocked powerups (`powerupsOffset`, bits in the fixed order vaults give them, `sly1.VaultPowerups`, plus Invisibility and the worlds' blueprints) and the equipped one (`equippedPowerup`); a powerup also has to be allowed by the level to work. Sly 2 and Sly 3 Practice Mode load maps at spawn points from `data/s2_spawn_points.txt` / `data/s3_spawn_points.txt`, plus every spawn point a load has arrived at (`MapPicker.Remember`, kept in `sN_seen_spawn_points.txt` in the user data folder). They load run files from Practice Mode's Run Files tab (`RunFilesForm`); Speedrun Mode only has the autosplitter, always on top and the input display, like Sly 4's. Sly 3 and Sly 4 Practice Mode have a Camera tab with a free camera and camera paths (keyframe animations, `CameraPath`, saved per level or map in the user data folder's `camerapaths`, with Sly 3's in `camerapaths/sly3`); every game's Camera tab is the shared `CameraForm`. Sly 3's Hide HUD sets the HUD's scale X (`hudScaleX`) to 0 and No fog sets `fog` to 0, each putting back the value it had. Sly 3's free camera (`Sly3FreeCamera`) runs through a camera hook (`sly3.StartCameraHook`): the game's world matrix update (`worldMatrixUpdateFunction`, which copies every object's local matrix at +0x50 to its world matrix at +0x90 when +0x44 marks it changed) has its calls pointed at a stub, in padding between two ELF sections of the code segment, that first puts SluMAN's matrix into the driven camera. RPCS3's static interpreter fixes the kind of instruction at every address it found code at when the game loaded (even code that never runs), reads only the operands again, and has nothing past the code segment's end: only patch `bl` to `bl`, and only put new code in bytes inside the code segment that were never code. Without the hook (RPCS3 needs the Interpreter PPU decoder) it falls back to writing the camera and the copies the game draws from (`cameraDrawMatrixOffset`, `cameraDrawMatrix`) directly, which flickers. Sly 1 Practice Mode has the same Camera tab (paths in `camerapaths/sly1`) with its own hook (`sly1.StartCameraHook`, research in [`.claude/sly1-camera-research.md`](.claude/sly1-camera-research.md)): every place the game sets the camera calls its camera matrix update (`cameraMatrixUpdateFunction`, `UpdateCmMat4`) just after, so its 4 `bl` calls and 1 tail-call `b` are pointed at a stub that copies SluMAN's position, rotation, projection, frustum and field of view (`sly1.CameraState`; the renderer is handed the raw field of view as well as the matrices) into the driven camera first. Sly 1 has no fallback without the hook (camera writes outside that update never show). While it flies, the player can't control Sly: bit 2 (the player, binocucom included) of the game's input targets (`inputTargets`, `g_grfjoyt`) is cleared every tick, since the game works them out again from its state flags (`userState`) on every state change, and `sly1.SetPlayerControlBlocked(false)` puts back what the game would have. Its Hide HUD points the Draw entry of every blot (HUD element) vtable (`blotVtableDraws`) at an empty function, which, being data, works with every PPU decoder; No fog sets the camera's fog amount to 0. Sly 4's free camera (`Sly4FreeMove`) draws the main view through a view override (`sly4.StartViewOverride`): the per-view copy call is pointed at a stub that copies SluMAN's matrices in first. The stub can also make the view orthographic (the Camera tab's Orthographic row, `CameraTabLayout.getOrthoHeight` and the rest, and the script API's `ortho`; R2 / L2 then zoom the height shown): it sets the view's projection type (`viewProjectionTypeOffset`; 0 has the game's `CalcRenderMatrices` build an orthographic projection from the rectangle at `viewOrthoRectOffset`, and culling takes its planes from the view-projection matrix, so it culls the box). The game doesn't set the type every frame, so `PutBackProjection` writes the old one back when ortho or the camera stops. The stub moved to `viewOverrideStub` when it grew; `legacyViewOverrideStub` is where earlier versions put it. Its stubs sit past the code segment's end (`viewOverrideStub`, `hackPreRenderStub`), so on RPCS3 it needs the Interpreter PPU decoder. While it's on, the renderer's draw list pushes (`drawListPushDescriptors`, which never check the lists' capacity) go through guard stubs (`drawListGuardStubs`) that drop items once a list is full: a free camera can see far more than the game's cameras, and an overfull list crashes the render thread. The hacking minigames (System Cracker, Spark Runner, Alter Ego) are levels of their own (`sly4.IsHackLevel`). Their mechanic copies the main view into three layer views of its own earlier in the frame (`hackPreRenderCall`), so the override hooks that call too and turns off the depth of field it blurs them with. Their player is the minigame's ship, ball or avatar, not a character: nothing follows the character controller there (`GetPlayerRef` returns 0), and the free camera holds the game's time nearly still (`sly4.SetTimeFrozen`, through the clock's frame time clamps) instead of holding the player. Bentley's Hackpack is built on Sly 4's engine, so it has no game class of its own: its versions are `Sly4Addresses` instances with `IsHackpack` set (`offsets/BH/HackpackAddresses.cs`, a partial of `Sly4Addresses`; `IsSupportedGameId` leaves them out and `IsHackpackGameId` finds them), and `HackpackPractice` / `HackpackSpeedrun` (`BH/`) drive the `sly4` class and Sly 4's Jobs, Camera and Tunables tabs (both Practice windows start jobs, warp to goals and load levels through a `Sly4JobQueue`, which the Jobs tab's `Sly4JobsForm` uses too). Hackpack Practice Mode starts and restarts jobs (each minigame stage is one) and loads the hub; `sly4.StartJob` queues Hackpack jobs through the game's RequestJob (`requestJobFunction`), as the arcade machines do, since only the episode manager's own start sets a stage's challenges up. Every Hackpack level but the hub (`FrontEnd`) counts as a hacking minigame (`sly4.IsHackLevel`), so the free camera freezes time there and not in the hub; nothing has a character (`GetPlayerRef` returns 0). Its levels are named from `data/hackpack_levels.txt` (`HackpackLevels`, names as `Scripts/Sly4HackPackLevels.xml` writes them, or hashes); its camera paths are kept in `camerapaths/hackpack` by level hash, so they stay put once a level is named. Its autosplitter (`autosplitters/hackpack-autosplitter.asl`) uses Sly 4's values. Research notes: [`.claude/hackpack-research.md`](.claude/hackpack-research.md). Sly 4's and Hackpack's Tunables tab (`Sly4TunablesForm`, `Sly4Tunables`) remembers each tunable's value from before SluMAN first wrote it (in `sly4_tunable_defaults.cfg` / `hackpack_tunable_defaults.cfg` in the user data folder, until put back) and puts them back with its Reset to Defaults button, when Practice Mode closes (shutdown, Switch Game/Mode) and when Speedrun Mode opens. Sly 4 Practice Mode's Gadgets tab (`Sly4GadgetsForm`) sets the state byte each gadget has in the game manager's gadget array (`gadgetOffset`, one byte per gadget in `Sly4Gadgets.All`'s order, not bits as in Sly 2/3): 0 locked, 1 in ThiefNet, 3 bought, as buying it sets (0x2 is the bought bit). The tab has a checklist per character, ticked if bought, like Sly 2/3's: ticking sets 3, unticking a bought one sets 1. The game only reads the array on a load, so changes stay in the lists until Apply writes them and reloads (`Sly4JobQueue.ReloadForGadgets`: retry the checkpoint in a job, otherwise load the level again). It saves the gadgets each run file loads with in `config.txt` (`Sly4_Episode1_GadgetStates` and so on, through `Sly4Gadgets.LoadRunFile` / `SaveRunFile`). Sly 4's Run Files tab (`Sly4RunFilesForm`, Episodes 1–5, through `Sly4JobQueue.LoadRunFile`) goes to the hideout of the episode's first job and writes those gadgets; starting a job never resets gadgets. Every game's gadget table (`Sly4Gadgets.All`, and `AllGadgets` in `Sly2Gadgets` / `Sly3Gadgets`) gives each gadget the episode it's available from: the first episode whose run file may have it, as it's in ThiefNet when that episode starts (0 from the start). Saving a run file's gadgets in the Gadgets tab leaves out the ones not available yet and says which inline (`GadgetAvailability`, the warning under `GadgetsForm`'s bar in Sly 2 and 3); Sly 4 locks them in the list, Sly 2 and 3 untick them. Memory tools (the Memory and Memory Viewer tabs) belong to Practice Mode only; Speedrun Mode must not offer them.

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
