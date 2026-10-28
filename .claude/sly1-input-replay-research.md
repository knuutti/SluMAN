# Sly 1 (NPUA80663) input recording and replay research

Goal: record gameplay (mostly movement) with the game's own camera, then replay it frame for
frame while the free camera plays a camera path, so the movement doesn't have to be played with
the controls following an unnatural camera. Static analysis of the NTSC PSN `EBOOT.elf`, matched
against the [Sly 1 decompilation](https://github.com/TheOnlyZac/sly1) (`joy.c`, `clock.c`,
`main.c`, PS2 NTSC `config/symbol_addrs.txt`). Nothing here has been checked on a running game
yet.

A replay is exact only if everything the game reads is the same as when it was recorded. The
user's way to get the same start every time is to load the level with the game clock frozen
(everything then starts on frame 1). From there the game reads three things each frame that can
differ: the **controller**, the **camera the controls are relative to**, and **real time**
(the clock's time step). Record all three every frame, and put them back on replay.

## The frame (`0xA4874`, called from the main loop `0x10354`)

In this order:

1. `UpdateJoy(&g_joy)` at `0xA4B40` (`0x84C44`), which calls `FReadJoy` (`0x8475C`).
2. If a level is loaded (`*0xE5E8D8 != 0`):
   - `SetupCm(g_pcm)` at `0xA4B84` (`0x4AB20`).
   - `MarkClockTick(&g_clock)` at `0xA4B98` (`0x480AC`).
3. The world update and everything else (from `0xA4BD4`).

## Controller

`g_joy` is at `0x428B54` and laid out as the PS2 `JOY`: left stick floats at +0x40/+0x44, right
at +0x74/+0x78, buttons at +0xA8 with the pressed / released copies at +0xAA/+0xAC. SluMAN's
`analogOffsetLeft`, `analogOffsetRight` and `inputOffset` are these.

`FReadJoy` (`0x8475C`, `r31` = the `JOY`) zeroes a 32-byte buffer on its stack (`r29`, kept across
calls) and fills it with `bl 0x172078` at **`0x847C4`**. `0x172078` is the port's stand-in for
`scePadRead`: it reads the real pad's state (`*(0xEA18E0 + port * 4)`) and writes PS2 DualShock
bytes (buttons through the table at `0x2CE688`). Its return is > 0 when there's data. `FReadJoy`
then uses only bytes 1 to 7:

| Byte | Meaning |
|---|---|
| 1 | pad mode (to +0x10) |
| 2, 3 | buttons, inverted (`nor`), to +0xA8; pressed / released worked out from the last |
| 4, 5 | right stick X, Y (`0x83C00` turns them into +0x74...) |
| 6, 7 | left stick X, Y (into +0x40...) |

So **8 bytes per frame** is the whole controller, and replaying them at this level lets the game
work out everything else (dead zones, pressed and released edges) itself. On the analog path,
the first call after the read is `bl 0x83C00` at **`0x84890`**, with the buffer still in `r29`.

## The camera the controls follow

`SetupCm` (`0x4AB20`) calls `SetupCmRotateToCam` (`0x4A950`). That builds the matrices Sly's
movement turns the stick into world directions with, and keeps them in the camera at
+0x370 to +0x3CF. It works them out from the camera's **forward row (+0x80, +0x84: X and Y, made
horizontal and normalised) and up row (+0xA0 to +0xAF)**. `SetupCm` also reads the position
(+0x40/+0x48) to look up the camera's zone (stored at +0x21C).

`SetupCm` runs before the camera is updated that frame, so it sees the rotation the camera ended
the last frame with. With the free camera on, that's SluMAN's (the camera hook puts it in at
`UpdateCmMat4`), which is why the controls follow the free camera. For replay: record +0x80,
+0x84 and +0xA0 to +0xAF (24 bytes) just before `SetupCm`, and put them back there. The controls
then follow the recorded camera whatever the free camera shows, and nothing depends on the game's
own (hidden) camera behaving the same.

## Real time

`MarkClockTick` (`0x480AC`) is as in the decomp. `g_clock` is **`0x3E62C0`**: +0 enabled,
+4 `t`, +8 `dt`, +0xC `dtPrev`, +0x10 `tReal`, +0x14 `dtReal`, +0x18 the last tick (64-bit).

The step is real elapsed time (`0x47F24`, the time base) clamped to **1/60 to 2/60 s**, times
`g_rtClock` (**`0x39C314`**, 1.0; 0 freezes game time) and `g_rtClockPowerUp` (`0x39C318`, the
Slow / Fast powerups). The game targets 60 fps, and any frame it takes longer to make (a wide free
camera view can cost frames) runs with a longer step. So replay should **force each frame's clock**:
record +4 to +0x17 (20 bytes) after the tick, and on replay write them instead of ticking. A
replay that renders slower then plays back slower in real time, with the same movement.

## Hooks

All `bl`→`bl`, each stub ending by branching on into the original function, so none needs a
stack frame. One frame's record is 8 + 24 + 20 = 52 bytes (64 with a frame number to check sync):
about 3.8 KB/s.

| Call | Record | Replay |
|---|---|---|
| `0x847C4` `bl 0x172078` (pad read) | branch on | copy the frame's 8 pad bytes into the buffer (`r5`), return 1 |
| `0x84890` `bl 0x83C00` | copy the 8 pad bytes from `r29` | branch on |
| `0xA4B84` `bl 0x4AB20` (`SetupCm`, camera in `r3`) | copy the 24 camera bytes | put them back |
| `0xA4B98` `bl 0x480AC` (`MarkClockTick`, clock in `r3`) | branch on | write the frame's clock fields, return |
| `0xA4BE8` `bl 0xC4E00` (after the tick) | copy the clock fields, next frame | next frame |

On replay, the real controller is ignored. SluMAN's own readers (input display, free camera
sticks) see the replayed input, which is fine: a camera path drives the camera then.

### Where the code and data go

The stubs need roughly 250 to 300 bytes. That's more than the padding left inside the code
segment (`0x2CD3B8` to `0x2CD400`, 0x48 bytes, and the 0x1C after the camera stub at `0x2FA298`).
The zero runs in sections 12 and 13 sit inside referenced data, so they may be in use. That leaves
**`0x37DC68` to `0x380000`** (0x2398 bytes), past the code segment's end in its last mapped page,
as Sly 4 does (`hookCodeCave`). That works on a PS3. On RPCS3 it needs a decoder that decodes code
as it runs, so most likely **Interpreter (dynamic)**: `CLAUDE.md` notes the static interpreter has
nothing past the segment end. **To check live.**

Data: a ring buffer after the camera hook's data, e.g. `0xFC1000` to `0xFC9000` (512 frames,
8.5 s at 60 fps of slack). SluMAN fills it ahead for replay and empties it behind for recording,
so a recording's length isn't limited by game memory. **To check live:** that `0xFC1000` to
`0xFCFFFF` is mapped (it is if the data segment's pages are 64 KB; the camera hook has only
used up to about `0xFC0600`).

## Starting in sync

The user's method: freeze game time (`g_rtClock` = 0), load the level (or a position), and start.
SluMAN can do the start itself: set `g_rtClock` back to 1 and arm recording or replay in the same
moment, so the first frame recorded or replayed is the first frame time moves. Replay forces the
clock from the recording, so its frames line up with the recording's by construction.

The camera path should then run on **game frames**, not the PC's clock: the free camera plays
paths by a `Stopwatch` now, which would drift from a replay that renders slower than real time.
Time the path by the replay's frame number (or the recorded `t`).

## Risks

- **Anything else that varies between runs.** A random number state that isn't reset by a load,
  or something timed by audio streaming rather than the game clock (dialogue, music-synced
  sections). The user finds a frozen-clock load gives the same outcome every time, so movement
  should hold. Guards, and long recordings, are the places to watch.
- **The digital path.** `FReadJoy` only reaches `0x84890` with an analog pad (mode > 1). A
  DualShock 3 always is, but recording would miss frames otherwise.
- **The free camera's other effects.** `SetupCm`'s zone lookup reads the camera position, which
  is SluMAN's during replay (it was the game's when recording). Zones may drive fog or music
  rather than gameplay; to watch.

## Live checks before building

1. `0xFC1000` to `0xFCFFFF` reads as zeros and keeps a write.
2. A stub past the segment end runs: on a PS3, and on RPCS3 with each interpreter.
3. Writing `0` to `0x39C314` freezes the game, and `1` resumes it.
