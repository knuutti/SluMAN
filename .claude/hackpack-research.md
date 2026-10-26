# Bentley's Hackpack research (NPEA00427, PAL PSN)

How the addresses in `SluMAN/offsets/BH/HackpackAddresses.cs` were found, from the decrypted
EBOOT.elf. The Hackpack is built on Sly 4's engine: the game manager, episode manager, views,
renderer, tunables and pad are Sly 4's classes, mostly with the same layout, so the `sly4` class
runs it with these addresses (`Sly4Addresses.IsHackpack`).

## ELF layout

- Code segment `0x10000`-`0xC868E8` (file offset + 0x10000), data segment `0xC90000`-`0x1026D38`
  (`.bss` from `0xD31480`). TOC (r2) `0xCCC880`; entry descriptor `0xCB82A8`.
- Strings are reached with `lis` + `addic` (not `addi`), which a cross-reference search has to
  follow. Imports: `.lib.stub` at `0x891EEC` (0x2C per library); `cellPadGetData`'s stub is
  `0x890044`.
- SluMAN's stubs go in the last 4 KB page of the code segment, past its end (`0xC86A00`-`0xC86DF0`),
  and their data in the last page of the data segment, past `.bss` (`0x1026E00`-`0x1026ED8`), so
  they're mapped whatever the page size. As with Sly 4, RPCS3 runs them only with the Interpreter
  PPU decoder.

## Pad (input display)

- `cellPadGetData` is called once, from the pad's update (`0x439E78`, vtable `0x8E53F0` + 0x10),
  with its CellPadData at pad + 0xA4 (Sly 4: +0x98). So buttons are at +0xAC, the sticks as floats
  at +0x20 (left) and +0x28 (right), as in Sly 4.
- The app object at `0xE97060` holds the game manager pointer at +0 and a 0x2EC0-byte entry per
  player (`0x32F908` fills them; pad at +0xB0 + 0x1C), so player 1's pad pointer is `0xE9712C`.

## Main loop and code hook

- Boot/main function `0x32FF80`: the main loop at `0x330600` calls the app's frame update
  `0x328648(app)` at `0x33065C` (other calls at `0x33075C`, `0x33076C` and `0x330830` are other
  states: the first frame and two flag-driven paths). Hooked like Sly 4's `0x463D20`.
- The frame update calls `0x327A98(gm)`, which calls the render submission `0x3275E8(gm)`.

## Game manager (`*0xD05B0C`, also app + 0)

- Fields past 0x128000 are Sly 4's + 0x600: current level hash `0x128714` (copied from the loading
  hash `0x128718` in `0x3499D0`, compared with `FrontEnd`'s hash 0xE4927E12), spawn character
  `0x12871C`, spawn point `0x128760`, main view `0x128768`, hacking mechanic `0x1287D4`.
- `LoadLevel(gm, hash, 0)` is `0x336384`: stores the loading hash, uses +0x2F / +0x30 / +0x32 like
  Sly 4's.
- Lower fields are shifted too (the player ref is most likely `0x1678`, Sly 4's `0x1078` + 0x600);
  the goal array wasn't found, so goals aren't read.

## Episode manager (`*0xCDEBD4`)

Built by `0x24523C` (which reads `Scripts/Episodes.xml`): 4 episodes of 0x35C at +0x14, 128 job
slots of 0x218 at +0x1C, current episode +0x24, current job +0x28, failed goal +0x4C, as Sly 4.
A job's episode index is at +0x1F4.

`StartJobByInst` is `0x248698`: the only function in the episode code that takes a 9th argument
(from the caller's stack), with the episode manager in r3 and the job in r4. It clears the current
job (+0x28) and looks up the job's episode (+0x1F4 into the episode array). Its callers at
`0x248D14` and `0x249564` (a search of the job array by stride 0x218) pass the same arguments as
Sly 4's debug menu. Its 6th argument is 1 in the game's own restart (`0x357684`, a menu callback that
starts the job whose hash is at `*(gm + 0x128764) + 0x541C0`; the fields there are named
RestartJob, QuitEpisode / QuitJob / QuitLevel by their hashes).

Starting a job (`0x24843C`) loads its goals (job + 0x214) into the goal list at gm + 0x1670 with
`0x241F2C`: array at +0x50 (gm + 0x16C0), 16-bit count at +0x58 (gm + 0x16C8). Goals keep their state
at +0x12 (5 = done, as Sly 4) and their ID at +0x9EC (Sly 4: +0xA00). `TeleportToGoal(em, goal, 1,
0)` is `0x2481FC`; the minigame code calls it too. A stage's challenges (score targets, collectibles,
finishing) are not its goals.

The game starts a stage's job with `RequestJob(em, entity, job, goal ID, flag, flag)` (`0x249594`,
called by the job trigger entity code at `0x1D82FC` with the job and goal ID from the trigger at
+0x1A8 / +0x1AC). It stores the request in the episode manager (+0x4 entity, +0x8 job, +0xC goal ID,
+0x10 flags) and sets its state (+0x0) to 1. The episode manager's update (`0x2489A0`, called from
the game manager's update at `0x360754`) then waits (state 1), starts the job with StartJobByInst
and a 9th argument of 0 (state 2), and finally calls `0x262EE4(goal, i)` for i < 5 on the first goal,
teleports to it with `TeleportToGoal(em, goal, 0, 0)` and resets every goal (state 3). Calling
StartJobByInst directly (as Sly 4's debug menu does) skips that, and the stage's challenge menu stays
empty.

## Level table

`Scripts/Sly4HackPackLevels.xml` is read at boot (`0x3B6010`) into the map at `0xEBA94C`: root
node at `*(*(0xEBA94C + 0xC))`, nodes with the level hash at +4, the record at +8 and children at
+0xC / +0x10 (`LoadLevel`'s lookup at `0x34A084` walks it). The record has a path at +0x80 (the boot
code skips levels whose path starts with `Dev/`). Level names aren't in the EBOOT: they're in
`Scripts/Sly4HackPackLevels.xmb` inside `Sly4HackPack.psarc.edat`, which RPCS3's Decrypt PS3
Binaries decrypts with the klicensee the game passes to `sceNpDrmIsAvailable2`
(`8D13952E4ABDCD0C058E8B9DC87696EC`, at `0x8AD244`). The levels are `FrontEnd` (path `UI/Frontend`),
`SystemCrackerA`-`T`, `SparkRunnerA`-`T` and `AlterEgoA`-`T` (paths like `AlterEgo/AlterEgoA`),
plus development levels under `Dev/`, whose level data isn't in the archive (an earlier SluMAN listed levels from this table and left them, and nodes
without a path, out of the list); the XMB stores each name's hash (`sly4.Hash`) next to it.

## Views and free camera

The view class is Sly 4's: fov +0x38, near/far +0x3C/+0x40, aspect +0x70, flags +0x34 (0x80 =
rebuild), effects +0x290 (depth of field at +0xC), view matrix +0x2F0, camera matrix +0x330.

- `CalcRenderMatrices(view, force)` `0x3DB9D0`, view copy `0x3DAD90`, called at `0x3DC458`.
- Hacking mechanic layer views at mechanic + 0x13C / +0x140 / +0x144 ("SystemCracker: TopLayer",
  "BottomLayer", "CompLayer"; Sly 4: +0x128..). Its pre-render `0x2BCFA8(mechanic, main view)` is
  called at `0x32780C` with the mechanic in r25 and the main view in r27.
- Draw list pushes (capacity at +0x9C, 8-byte entries): `0x3DDC2C` / `0x3DDC48` (descriptors
  `0xCB83D8` / `0xCB83E0`) and the sorted pushes `0x59BD20` / `0x59BE90` / `0x59C000` (descriptors
  `0xCB89C8` / `0xCB89D8` / `0xCB89E8`).

## Tunables

Registry `0xEBAA5C`, count `0xEBD61C` (registered by the base constructor `0x3C6228`). Vtables:
bool `0x8D2664`, int `0x8D6080`, float `0x8D2698`, vec2 `0x8D7850`, vec3 `0x8D7884`, colour
`0x8D26CC`, string `0x8DB780`, string hash `0x8D7ED0`. Values (struct + 0x48): UI.3D.FOV
`0xE9ED9C`, Debug.Clock.ClampFreqMin / Max `0xE9D0F0` / `0xE9D13C`, Game.CutSceneEnable `0xE9E1D8`,
Cheat.Invulnerable `0xDE45F0`, Cheat.Undetectable `0xD32268`.

## Not looked for yet

GUI tree, AbandonCurrentJob, the
player and character controller.
