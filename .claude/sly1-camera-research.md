# Sly 1 (NPUA80663) camera research

Static analysis of the NTSC PSN `EBOOT.elf` (stripped, PPC64 big-endian, TOC `r2 = 0x39C378`), matched
against the PS2 freecam in [Meos4/Enhanced-Freecam](https://github.com/Meos4/Enhanced-Freecam/tree/main/src/Games/PS2/Sly1)
and the "Sly 1" tab of the Sly addresses sheet. Function names are the PS2 ones; the PS3 binary has no
symbols, so each is identified by what it does. Nothing here has been checked on a running game yet
(see "Verify live" at the end).

## The camera (`CM`)

`g_pcm` (pointer to the main `CM`) = **`0x3E6340`**. Its TOC entry is loaded at 340 places in 114
functions, and the pointer is read at +0x40, +0x1C4, +0x1E0 and +0x1F0, which is the PS2 layout.

The PS3 port keeps the PS2 struct layouts. Sly's entity already showed this (transform +0xD0, position
+0x30 → +0x100, as on PS2), and every camera function below uses the PS2 `CM` offsets:

| Offset | Type | What |
|---|---|---|
| +0x40 | VECTOR (x, y, z, w) | position |
| +0x50 – +0x7C | floats | copied by `UpdateCmMat4` from +0x180 – +0x1B8 (frustum planes) |
| +0x80, +0x90, +0xA0 | 3 × VECTOR | rotation rows: forward, left, up (Z up), as Sly 2/3 |
| +0xB0 | float | LOD / fade scale, `max(1, +0x1C0 / fov)`, set by `RecalcCmFrustrum` |
| +0xC0 | MATRIX4 | projection (built by `BuildSimpleProjectionMatrix`) |
| +0x100, +0x140, +0x180 | matrices | derived by `UpdateCmMat4` (world→clip, its inverse, frustum planes; inferred) |
| +0x1C4 | float | field of view, radians (eases to +0x1C8) |
| +0x1C8 | float | target field of view |
| +0x1E0 | float | aspect ratio |
| +0x1E4 / +0x1E8 | float | near / far clip |
| +0x1EC | float | written by `RecalcCmFrustrum` (`sqrt(aspect² + 1)·tan·near + 1`) |
| +0x1F0 / +0x1F4 | float | frustum: tan(fov/2)·aspect, tan(fov/2) |
| +0x1F8 / +0x1FC | float | fog start / end |
| +0x200 | float | fog amount (0 – 1) |
| +0x204 | RGBA | fog colour |
| +0x218 | float | screen-shake start time (`UpdateCmMat4` takes the shake path while it's recent) |
| +0x220 | int | reset camera (sheet: write 1) |

Each frame `UpdateCmMat4` copies the fog fields into a render-state global at `0xF4DF20`
(+0x120 – +0x14C).

## Camera functions (PS3 addresses)

| PS2 name | PS3 | What it writes |
|---|---|---|
| `UpdateCmMat4(CM*)` | **`0x4B0CC`** | Reads pos/rot/proj and builds +0x100/+0x140/+0x180/+0x50. With screen shake active it rebuilds the projection from the frustum and near/far |
| `SetCm` | `0x4B688` | pos (`std` +0x40/+0x48), rot through `LoadRotateMatrixPanTilt`, then `bl UpdateCmMat4` |
| `SetCmLookAtSmooth` | `0x4B7C0` | pos +0x40/+0x48, rows `stvx` +0x80/+0x90/+0xA0, then `bl UpdateCmMat4` |
| `SetCmPosMat(CM*, pos, mat)` | `0x4BCD0` | pos +0x40/+0x48 if given, rows +0x80/+0x90/+0xA0 if given, then `bl UpdateCmMat4` |
| `RecalcCmFrustrum(CM*)` | `0x4BD80` | +0x1F0/+0x1F4/+0xB0/+0x1EC, projection +0xC0, then **tail call** `b UpdateCmMat4` |
| `UpdateCplook` (the normal follow camera) | `0x51484` | rows into the CM, then `bl UpdateCmMat4` at `0x52088`. Only called through its function descriptor |
| `LoadRotateMatrixPanTilt` | `0xAA5E8` | |
| `BuildSimpleProjectionMatrix(sx, sy, ox, oy, near, far, MATRIX4*)` | `0xA9CEC` | |
| `tanf` | `0x20F1B4` | |
| (per-level clip/fog setter, likely) | `0x4C1B0` | +0x1E0 – +0x207 on the CM, then `RecalcCmFrustrum` |

Projection, from `RecalcCmFrustrum` + `BuildSimpleProjectionMatrix` (TOC constants 640, 480, 4096):

```
ty = tan(fov / 2)            -> +0x1F4
tx = ty * aspect             -> +0x1F0
m[0][0] = 640 / (tx * 4096)  m[1][1] = 480 / (ty * 4096)
m[2][0] = m[2][1] = 0        m[2][2] = (near + far) / (near - far)
m[2][3] = TOC[-0x6208] (1.0 on PS2)   m[3][2] = near * (1 - m[2][2])   everything else 0
```

## Every camera write ends in `UpdateCmMat4`

Every function that writes the camera's pos/rot/projection calls `UpdateCmMat4` straight after. The
function has exactly **5 call sites** and is never called through a pointer (its descriptor `0x384DF8`
is referenced nowhere):

| Call site | Instruction | Caller |
|---|---|---|
| `0x4B78C` | `bl` (`4BFFF941`) | SetCm |
| `0x4B954` | `bl` (`4BFFF779`) | SetCmLookAtSmooth |
| `0x4BD3C` | `bl` (`4BFFF391`) | SetCmPosMat |
| `0x4BE74` | **`b`** (`4BFFF258`) | RecalcCmFrustrum (tail call) |
| `0x52088` | `bl` (`4BFF9045`) | UpdateCplook |

This is the same position Sly 3's world matrix update is in, so the same hook works. Point all five at a
stub (`bl`→`bl`, `b`→`b`: same instruction kind, so RPCS3's static interpreter is fine with it). When
the camera passed in `r3` is the one SluMAN drives, the stub copies SluMAN's pos, rotation, projection
and frustum into the CM, then branches on to `UpdateCmMat4`. The game's own writes are overwritten
just before every matrix build, so they never show. The game's camera code (`UpdateCplook` etc.) keeps
running and only loses its output.

### Where the stub and its data go

- **Stub: `0x2FA298` – `0x2FA300`** (0x68 bytes of zeros between two ELF sections, inside the R E code
  segment `0x10000` – `0x37DC68`). A second gap at `0x2CD3B8` – `0x2CD400` (0x48) is free if more room
  is needed. `0x37DC68` – `0x380000` is past the segment end, so it isn't usable on RPCS3.
- **Data: `0xFC0300`**. BSS ends at `0xFC02C8` (data segment `0x380000` + memsz `0xC402C8`), no TOC
  entry points past `0xFC02C0`, and the block (≈0x290 bytes) ends inside the last page whether pages
  are 4 KB or 64 KB.

Data layout (the copied fields sit at the same offsets they have in the CM):

```
+0x000 on (0 = pass through)   +0x004 camera (CM*)   +0x008 count   +0x00C mark
+0x040 ..+0x1F7  SluMAN's values at their CM offsets
+0x200 word offsets to copy, 0-terminated (35 words): 40 44 48 4C, 80..B0, C0..FC, 1F0 1F4
```

Stub (19 words, 0x4C bytes; it only touches r5–r7, r11, r12 and cr0, which are free at all five
call sites):

```
002FA298  3D6000FC  lis   r11, 0xFC
002FA29C  396B0300  addi  r11, r11, 0x300
002FA2A0  818B0000  lwz   r12, 0(r11)        ; on?
002FA2A4  2C0C0000  cmpwi r12, 0
002FA2A8  41820038  beq   0x2FA2E0
002FA2AC  818B0004  lwz   r12, 4(r11)        ; the driven camera?
002FA2B0  7C036000  cmpw  r3, r12
002FA2B4  4082002C  bne   0x2FA2E0
002FA2B8  38AB01FC  addi  r5, r11, 0x1FC     ; offset list - 4
002FA2BC  84C50004  lwzu  r6, 4(r5)
002FA2C0  2C060000  cmpwi r6, 0
002FA2C4  41820010  beq   0x2FA2D4
002FA2C8  7CEB302E  lwzx  r7, r11, r6
002FA2CC  7CE3312E  stwx  r7, r3, r6
002FA2D0  4BFFFFEC  b     0x2FA2BC
002FA2D4  80EB0008  lwz   r7, 8(r11)         ; count++
002FA2D8  38E70001  addi  r7, r7, 1
002FA2DC  90EB0008  stw   r7, 8(r11)
002FA2E0  4BD50DEC  b     0x4B0CC            ; UpdateCmMat4
```

Patched call sites: `0x4B78C` → `482AEB0D`, `0x4B954` → `482AE945`, `0x4BD3C` → `482AE55D`,
`0x52088` → `482A8211`, `0x4BE74` → `482AE424` (the `b`). To stop, set `on` = 0 and put the original
words back.

Offset list bytes for `0xFC0500`:

```
0000004000000044000000480000004C0000008000000084000000880000008C0000009000000094000000980000009C
000000A0000000A4000000A8000000AC000000B0000000C0000000C4000000C8000000CC000000D0000000D4000000D8
000000DC000000E0000000E4000000E8000000EC000000F0000000F4000000F8000000FC000001F0000001F400000000
```

FOV: SluMAN computes the projection and frustum with the formulas above from its FOV and the game's
aspect/near/far. While shake is active `UpdateCmMat4` rebuilds the projection from the copied frustum,
so it still gets SluMAN's FOV. When the free camera stops, write the projection, frustum and +0xB0
back from the game's current +0x1C4/+0x1E0/+0x1E4/+0x1E8 (the PS2 freecam does the same), and
probably set +0x220 = 1 so the follow camera snaps back.

### Game paused

`UpdateCplook` returns early when its dt (`f27`) is 0, so `UpdateCmMat4` stops being called and the
free camera freezes. This is the PS2 freecam's "force recalc if game paused" patch. On PS3 it's
`bne cr7, 0x515AC` at **`0x51530`** (`409E007C`). Changing it to the always-taken `bc 20,0,0x515AC`
(`4280007C`) keeps it a `bc`, so RPCS3's static interpreter allows it. Only needed if SluMAN pauses
the game while the free camera runs.

### RPCS3

As with Sly 2/3, the code patches need the Interpreter (static) PPU decoder. With LLVM, a hookless
fallback would have to write the derived matrices (+0x100/+0x140/+0x180/+0x50), which means
reimplementing `UpdateCmMat4`'s math. Writing only pos/rot nonstop barely works here, because the game
rebuilds the matrices straight after its own writes.

## Implemented

The hook above is `sly1.StartCameraHook` / `Sly1FreeCamera`, on the Camera tab (`Sly1CameraForm`).
SluMAN builds the projection from the game's own one at start: only m[0][0] and m[1][1] depend on
the field of view, so they're scaled by the ratio of the frustum values, with no screen constants.
The player is held by writing their position (and zero velocity) each tick, as Sly 3 does.

**Hide HUD.** Every HUD element is a blot (see the [decomp](https://github.com/TheOnlyZac/sly1)'s
`screen.h`), drawn by `DrawUi → DrawBlots` through `VTBLOT::pfnDrawBlot`, slot 9. PS3 vtables are
arrays of 4-byte pointers to function descriptors. The blot vtables were found by their shared base
slots, `DtAppearBlot` / `DtVisibleBlot` / `DtDisappearBlot` (`0xD5CAC` / `0xD5CB4` / `0xD5CBC`,
returning BLOT +0x550/+0x554/+0x558). There are 36 vtables (`0x3ABE70` – `0x3B522C`, listed in
`Sly1Addresses.blotVtableDraws` with their Draw descriptors). Hide HUD writes the descriptor of the
lone-`blr` function `0x10750` (`0x383588`) into each one's +0x24. That's a data write, so it works on
every RPCS3 decoder. It hides every blot, the pause menu's included.

**No fog.** Writes 0 to the camera's +0x200, per level, and puts the old value back.

Checked offline: the C# hook code was run against the EBOOT's memory image through a fake
`IPS3API`, and the patched call sites and stub were run in an emulator (PPC32 mode; every
instruction SluMAN adds behaves the same as in 64-bit mode) up to `UpdateCmMat4`'s entry.

**Field of view.** Copying only the projection and frustum didn't change the view on hardware. The
render setup (`0x49250`, and the context builders `0x18F574` / `0x18F5B4`) uploads +0x100/+0x140, and
also hands the renderer the raw aspect, field of view (+0x1C4), near and far. So the stub also puts
SluMAN's field of view into +0x1C4 and its target +0x1C8 (the game eases one to the other).
`SetCmFov` is `0x4BF48`: it sets both, then calls `RecalcCmFrustrum`. Two special passes
(`0xC4374`, `0x13B864`) set a fixed field of view around a render and then put it back. While the
hook runs they get SluMAN's. When the free camera stops, the game's field of view, target,
projection, frustum and LOD scale from the start are written back.

**Player control.** The sheet's "Control Sly" (PS2 NTSC `0x262C68`) is the decomp's `g_grfjoyt`:
which parts of the game read the controller. `UpdateGrfjoytFromGrfusr` (PS3 `0x83B78`) sets it from
`g_grfusr` whenever that changes: 0 if `g_grfusr & 0x84`, else 5 with the menu bit (1), else 4
with the hands-off bit (2, cutscenes), else 7. On PS3, `g_grfusr` is `0x428B48`, `g_grfjoyt` is
`0x428B4C`, and `g_joy` is `0x428B54` (its sticks at +0x40 are SluMAN's `analogOffsetLeft`).
Bit 2 is tested by `0x2F7B0` and `0x32CD8` (both called from `0x35774`, Sly's update, and reading
the camera's +0x478 for camera-relative moves), `0xD8180` (among the blot functions) and
`0x142278`. Bit 4 is tested by `0x5AC18`, `0x92288`, `0xB6108`, `0xD8EA4` and `0x140C3C`, and bit 1
by `0x4BF60` (camera). The controller is still read with bit 2 cleared, so the free camera keeps
its sticks. While the free camera flies, SluMAN clears bit 2 every tick, so R1 / L1 don't open the
binocucom. When it stops, it writes back what `0x83B78` would.

## Not done yet

- **Pause**: PS2 patches `MarkClockTick`; the PS3 clock hasn't been found yet. The decomp's
  `config/symbol_addrs.txt` (PS2 NTSC, data symbols included: `g_clock 0x261858`, `g_joy 0x262C70`,
  `g_ui 0x275BC0`) is the best place to start.
- **Live checks**: that +0x200 = 0 really removes the fog, and that the free camera keeps working
  while the game's paused menu shows (if the game doesn't update the camera then, the hook doesn't
  run and the view stays put until it's unpaused).

## Verify live

1. `*0x3E6340` is non-zero in a level; `+0x40` is near Sly (`*0x428D04 + 0x100`); `+0x1C4` looks
   like a FOV in radians, `+0x1E0` like an aspect ratio, `+0x1E4/+0x1E8` like near/far.
2. `0x2FA298` – `0x2FA300` reads as zeros; `0xFC0300` – `0xFC0600` reads as zeros and keeps a write.
3. Hook by hand: copy the CM's current +0x40 – +0x1F7 to `0xFC0340`, write the offset list to
   `0xFC0500`, `*0xFC0304` = the CM pointer, the stub at `0x2FA298`, the 5 call sites, then
   `*0xFC0300` = 1. `*0xFC0308` should count up, and the view should freeze where it was. Then change
   `0xFC0340` (x) and the view should move, with no flicker.
4. Write 0 to CM +0x200 and check the fog goes away.
