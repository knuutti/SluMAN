# Uniform Practice Mode UI: plan

Practice Mode's shared features (loading a place, jobs, reloading, saved positions, toggles, the
player and camera tabs, the Tools menu) look and sit differently in each game. This plan puts
every shared feature in the same place, with the same name, in Sly 1-4 and Bentley's Hackpack.
Matching the common features comes first; game-specific features move to fit around them.

Speedrun Mode is already the same in every game (Autosplitter, Always On Top, Input Display and
the same menu) and is left alone.

## Decisions

1. **Fly controls are R1 up / L1 down in every game**, as in Sly 4's fly and every free camera.
   Sly 1 and Sly 3 move off L2 up / R2 down. Their horizontal boost is on R1 today, so it moves
   too (proposed: R2). Update the Sly 1 tooltip and the Sly 3 checkbox text
   ("Fly Mode (L2 up, R2 down)") to match.
2. **Sly 1 keeps its own toggle names** ("Freeze Lives", "Freeze Gold Charm"). Lives and charms
   work differently from Sly 2-4's health, so they go with the game-only toggles, not in the
   shared Invulnerability / Infinite Health slots.
3. **Sly 4's Practice Job group shrinks** to job + goal + Load. Go to Hub and Go to Hideout stay
   on the Jobs tab, which already has them (`Sly4JobsForm`).
4. **Features missing from some games are added later** (see [Future features](#future-features)).
   The Practice tab can show their controls already, disabled and calling nothing.

## Target layout

### Practice tab

Two columns of 312-wide group boxes. Each group has a fixed slot in every game; a group a game
doesn't have collapses and the ones below it move up.

| Slot | Group | Contents | Games |
|---|---|---|---|
| L1 | Level / Map | place, entrance / spawn point, **Load**; Character (Sly 4) | all (Hackpack: Load Hub) |
| L2 | Job | job, checkpoint / goal, **Load** | Sly 2, 3, 4, Hackpack |
| L3 | Reload | Reload / Retry Checkpoint / Restart Job, **Skip Cinematic**; Kill (Sly 2), Fast / Full (Sly 3), Character (Sly 2, 3) | all |
| L4 | Saved Positions | slot, **Save**, **Load** | Sly 1-4 |
| R1 | Game | Level / Map, Job, Checkpoint / Goal, then game rows (Sly 1 lives, charms; Sly 4 episode) | all |
| R2 | Utility | Coins, Health (Sly 2, 3) or Lives and Charms (Sly 1): text box + Set | Sly 1-4 |
| R3 | Toggles | in the order below, in two sub-columns | all |

Toggle order:

1. Invulnerability
2. Infinite Health
3. Infinite Gadget Power
4. Undetectable
5. Freeze Game Clock (Sly 2, 3) / Slow Motion (Sly 4, Hackpack)
6. Fly Mode
7. Infinite Jump
8. Auto-skip Cutscenes
9. Game-only toggles: Disable Death Barriers (Sly 3), Freeze Lives and Freeze Gold Charm (Sly 1)
10. Always On Top (always last)

Shared labels: "Invulnerability" (not "Invulnerable"), "Auto-skip Cutscenes" (not "Skip
Cutscenes" / "Auto-skip Cinematics"), "Skip Cinematic", "Load" on every load button, "Toggles"
(not "Options"), "Retry Checkpoint" (Sly 4's "Retry").

### Tabs

Practice, Player, Camera, then the game's own tabs, then Memory and Memory Viewer:

| Game | Tabs | Shown by default |
|---|---|---|
| Sly 1 | Practice, Player, Camera, World States, Powerups, Memory, Memory Viewer | Player, Camera, World States, Powerups |
| Sly 2 | Practice, Player, Camera, Gadgets, Run Files, Memory, Memory Viewer | Player, Camera, Gadgets, Run Files |
| Sly 3 | as Sly 2 | as Sly 2 |
| Sly 4 | Practice, Player, Camera, Jobs, Gadgets, Run Files, Tunables, GUI, Memory, Memory Viewer | Player, Camera, Jobs, Gadgets, Run Files |
| Hackpack | Practice, Camera, Jobs, Tunables, Memory, Memory Viewer (no player) | Camera, Jobs |

### Tools menu

Switch Game/Mode, Input Display, Controller Combos..., Memory Utilities..., Open User Data
Folder, the game's own items (Language on PAL Sly 2 / 3, Copy Episode State on Sly 2), a
separator, Power Off (PS3), Reboot (PS3). Sly 4 gains Controller Combos (its combos already run).

## Phases

### Phase 1: names, order and menu (no logic moves) — done

Left for Phase 2: Sly 4's "Retry" becomes "Retry Checkpoint" when it moves to the Reload group
(the Game group has no room for the longer label). Sly 1's R2 fly boost also changes the equipped
powerup in the game (L2 / R2 cycle it), as L2 / R2 height did before.

- A shared Tools menu builder on `GameSession`, used by all five Practice windows.
- Labels as above; toggles in the fixed order; tabs in the fixed order with the defaults above
  (`*_visible_tabs` keys).
- Sly 2 / 3 toggle errors go to the status line instead of `Console.WriteLine`.
- Fly controls switch to R1 / L1 (decision 1) in `PositionEditor`'s fly and Sly 3's fallback
  fly in `Sly3Practice.FreezeTimer_Tick`.

### Phase 2: one Practice-tab builder — done

Every Practice window is now built in code with `PracticeLayout` (the Designer files of Sly 2, 3
and 4 are gone; Sly 2 / 3's job lists are in `Sly2Jobs.cs` / `Sly3Jobs.cs`). Notes:

- Sly 4's Job group's Load starts the picked job at the picked goal (its first without one), or
  warps to the running job's goal when no job is picked; To Hub / To Hideout are on the Jobs tab.
- Sly 2's Kill with the Character list on "Default" kills the current character; it used to
  switch to Sly first (the list was read one entry off).
- Sly 2 / 3's Game group shows Map, Coins and Health; Job and Checkpoint need readers (below).
- Sly 3's Disable Guard AI moved to the game-only toggles, leaving the Undetectable slot to the
  placeholder.
- A window's own FormClosing handler is registered before its `GameSession`, whose handler
  disconnects (Sly 1's used to run after it).

- A shared `PracticeLayout` from the helpers Sly 1 and Hackpack each copy (`AddGroup`,
  `AddButton`, `AddCheckBox`, `AddValueLabel`), placing named groups in the slots above.
- Sly 2, 3 and 4 Practice built in code with it instead of the Designer.
- Moves: Sly 4 Load Level and Job to the left column, Game to the right, Coins from the Player
  tab to Utility, Retry from Game to Reload, Job group shrunk (decision 3); Sly 2 / 3 Skip
  Cinematic from Utility to Reload; Sly 1 Coins / Lives / Charms to the right column; Hackpack
  Restart Job to Reload and Load Hub to the Level slot.
- Saved Positions (slot, Save, Load) on Sly 1-3's Practice tab, through
  `IGame.selectedPositionIndex`, as Sly 4's.
- A Game status group on Sly 2 / 3 (map, job, coins, health).
- Disabled placeholders for the [future features](#future-features).

### Phase 3: Player tab — done

Sly 4's Player tab is the shared `PositionEditor` (`sly4.GetPositionEditorLayout`), and
`Sly4PositionForm` is gone. Notes:

- New `PositionEditorLayout` hooks: `writePosition` (Sly 4 moves the Havok body with
  `Teleport`), `readVelocity` (the character controller's), `heightAxis` (Y is up in Sly 4),
  `isLoading` and `getFlySpeed` / `setFlySpeed` (a Fly speed row in Set Position).
- Saved positions need no migration: the editor keeps them under the same keys
  (`sly4_<level hash>`, 24 hex digits) as `sly4.SavePosition`. Sly 4 has no shipped warps
  (`data/sly4_warp_locations.txt`); user warps go to `sly4_user_warps.txt`.
- Character Info has a Top Speed row with Reset in every game.
- Sly 4's Fly Mode is a Practice toggle driving `Sly4FreeMove`; it unticks when the free camera
  takes over. Its speed stays on the Player tab. The editor's freezes are dropped when a Sly 4
  load starts.
- Sly 2's Fly Mode is still a placeholder (Future features).

- Sly 4 on the shared `PositionEditor`, with `PositionEditorLayout` delegates for its rigid body
  (as Sly 1 has `readEntity` / `transformInEntity`). Sly 4 gains Freeze, Warp Locations and the
  slot grid. Check its saved positions in `config.txt` against `PositionEditor`'s, or migrate.
- Sly 4's speed / top speed readout in Character Info for every game.
- Fly Mode is a Practice toggle in every game; Sly 4's fly speed stays on its Player tab.

### Phase 4: shared code, so the games don't drift apart again — done

The shared tabs are one class each now, with what differs per game passed in. Notes:

- `CameraForm` is every game's Camera tab, built from the game's `GetCameraTabLayout`
  (`CameraTabLayout`). The free cameras implement `IFreeCamera`; Sly 4 / Hackpack wrap
  `Sly4FreeMove`'s free camera mode in `Sly4FreeCamera`, and their Far distance row and Script
  API checkbox come from the layout (`getFarDistance`, `scriptApi`). The Set Camera tooltip now
  says Y is up in Sly 4.
- `GadgetsForm` is Sly 2 / 3's Gadgets base: the same bar under the Bentley and Murray columns
  (Run file gadgets Load / Save; Discard Changes, Save, Save and Reload), with the first column
  left to the game (Sly 2's vault gadgets, Sly 3's special moves). Sly 2 gains Discard Changes,
  which now reads the game's gadgets again instead of closing the tab. The per-column
  Check / Uncheck All boxes were already the same in both and stay with the columns. The
  Designer still lays the columns out; the base class is abstract, so the VS designer can't
  open those two forms any more.
- `Sly4JobQueue` holds the game call tracking and the queued goal warp, job restart and level
  load for both `Sly4Practice` and `HackpackPractice`, and the Jobs tab calls it directly
  (`ISly4JobHost` is gone). Where the games differ it checks `sly4.addr.IsHackpack`: the
  Hackpack restarts a running job through RequestJob and warps to a running job's goal in place,
  Sly 4 abandons the job first and starts it again at the goal.
- Merged from master: Sly 4's Gadgets and Run Files tabs, and run files leaving out gadgets not
  available yet in their episode (`GadgetAvailability`). The warning line is part of
  `GadgetsForm`'s bar: `SaveRunFileGadgets` returns the gadgets it unticked. `Sly4GadgetsForm`
  keeps its own layout (a column per character, Apply / Discard, as the game only reads gadgets on
  a load); the run file load and Apply's reload are `Sly4JobQueue.LoadRunFile` /
  `ReloadForGadgets`. Putting it on `GadgetsForm` too (four columns, the same bar) is left for
  later.

- One camera tab form with a small per-game adapter (path folder, level key and name, Hide HUD,
  No fog, field of view, Sly 4's Far distance) in place of `Sly1CameraForm`, `Sly2CameraForm`,
  `Sly3CameraForm` (near-identical copies) and `Sly4CameraForm`.
- A Gadgets base for Sly 2 / 3 with one button bar (Save, Save and Reload, Discard Changes,
  Check / Uncheck All), as `RunFilesForm` is for run files.
- The job start, goal warp and game call queue `Sly4Practice` and `HackpackPractice` each have,
  shared.
- `CLAUDE.md` describes the fixed slots and tabs, so new games follow them.

## Future features

Features one game has and another lacks. Each is new work for its game. Phase 2 adds the
control in its slot, **disabled, with a "Not available yet" tooltip and no game calls**, so the
layout already matches; making it work means adding the game method and wiring the control.

| Feature | Missing in | Notes |
|---|---|---|
| Fly Mode | Sly 2 | Make `Sly2Practice` an `IPositionEditorHost`, as `Sly3Practice` is. |
| Undetectable | Sly 3 | `sly3.SetUndetectable` exists, unused. Decide how it sits next to "Disable Guard AI". |
| Auto-skip Cutscenes | Sly 1 | Needs a `sly1` auto-skip, like `sly2` / `sly3.SetAutoSkip`. |
| Skip Cinematic (one-shot) | Sly 4, Hackpack | Needs a `sly4.SkipCinematic`. |
| Reload | Sly 4 | Reload the current level / checkpoint in place; Retry Checkpoint covers jobs only. |
| Invulnerability, Infinite Health | Sly 1 | Only if they mean something beyond Freeze Gold Charm / Freeze Lives (decision 2). |
| Infinite Gadget Power | Sly 4 | Needs research. |
| Infinite Jump | Sly 4 | Needs research. |
| Freeze Game Clock / Slow Motion | Sly 1 | Needs research. |
| Set Health | Sly 4 | Needs research. |
| Game status rows | Sly 2, 3 | Map, coins and health show. Job needs the current job ID (`currentJobId` / `currentJob`) mapped to its name; checkpoint needs a reader. |