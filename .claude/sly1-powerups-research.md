# Sly 1 (NPUA80663) powerups research

How Sly 1's powerups (thief moves, Thievius Raccoonus pages) are unlocked and used, and how
SluMAN can edit them, for a Practice Mode tab like Sly 2-4's Gadgets. Static analysis of the NTSC
PSN `EBOOT.elf`, matched against the [Sly 1 decompilation](https://github.com/TheOnlyZac/sly1)
(`game.h`, `steppower.h/.c`, `game.c`, and its PS2 NTSC `config/symbol_addrs.txt`). Nothing here has
been checked on a running game yet.

## Where they're kept

The game save (`GS`, `g_pgsCur` = `*0x3A4FC0` = `0x3E660C`, SluMAN's `gameState`) is laid out
as on PS2 up to +0x19E8. The PS3 version has an extra float at +0x19EC, so everything after it is
4 further on than in the decomp and the sheet:

| Field | PS2 | PS3 | |
|---|---|---|---|
| float (new on PS3) | - | +0x19EC | read and kept with the settings |
| settings (`grfgs`) | +0x19EC | +0x19F0 | the sheet's "Unlocked Thief Moves" offset is this on PS3 |
| **powerups (`grfvault`)** | +0x19F0 | **+0x19F4** = `0x3E8000` | bitmask, below |
| unlocked cutscenes | +0x19F4 | +0x19F8 | |
| completion (`fgs`) | +0x19F8 | +0x19FC | |
| **last powerup (`fspLast`)** | +0x19FC | **+0x1A00** = `0x3E800C` | -1 for none |

How the PS3 offsets were found: `InitGameState` (`0x704CC`) writes -1 to +0x1A00 (the decomp's
`fspLast = -1`), 5 lives to +0x19E0, and 0 to +0x19F4 (`grfvault = 0`). When keeping settings, it
copies +0x19EC and the 0x3EE0 bits of +0x19F0. `0x705A0` applies the settings from +0x19F0's bits.

The **equipped powerup** (`g_fsp`, the sheet's "Active Thief Move") is **`0x39F9B0`**: 0 Dive,
1 Roll, 2 Slow, 3 Mine, 4 Fast, 5 Decoy, 6 Stun, -1 none. L2 / R2 cycle it among the unlocked ones
(`IRotatePowerUp`, `0x121574`), and Triangle uses it (`UpdateJtActivePowerUp`, `0x1218E0`).

## Unlocking

- **Vaults** (`0xBE080`): opening one adds 1 to the world's vault count (`g_pwsCur` +0x43C), sets
  the level's vault flag (`g_plsCur` +0, `|= 4`), and ORs the vault's reward (vault +0x748) into
  `grfvault`. The reward is set up in `0xBED8C`: starting at `0x2`, the bit moves up while the save
  already has it (at most 15 steps). So **the Nth vault opened gives bit N**, whichever level it's in.
- **Bosses** (`DefeatBossFromWorld`, `0x70A78`): sets the world's boss-defeated flag (`0x20`) and
  unlocks cutscenes. Only Mz. Ruby (Vicious Voodoo) also gives a powerup: `grfvault |= 0x10000`.
- **Scripts**: `SetGrfvault` (`0xBDF4C`) sets the whole field (level script command).
- `0xBDFC8` treats `grfvault & 0x0FFFFFFF == 0x0FFFFFFF` as having all of them.

## Using

`GetGrfvault` (`0x70100`) is what almost every check calls:
`return gs.grfvault & (*0xE5E8D8)->+0x235C`. `*0xE5E8D8` is the current level (SluMAN's
`entranceListPtr`), and +0x235C is that **level's allowed powerups**. It's loaded with the level,
so a powerup works only if it's unlocked **and** the level allows it. Writing `grfvault` takes
effect at once: nothing caches it. If the equipped powerup stops being available,
`UpdateJtActivePowerUp` drops it by itself.

The 7 equipped powerups each need any of these bits (`s_mpfspgrfvault`, `0x39F9B4`, right after
`g_fsp`):

| `g_fsp` | Powerup | Bits |
|---|---|---|
| 0 | Dive | `0x2` or `0x10` |
| 1 | Roll | `0x4` or `0x400` |
| 2 | Slow | `0x8` or `0x1000` |
| 3 | Mine | `0x40` |
| 4 | Fast | `0x80` |
| 5 | Decoy | `0x200` |
| 6 | Stun | `0x8000` |

## The bits

Every third vault opened in a world gives the world's blueprint (`GetBlueprintInfo`, `0x70288`),
not a powerup. That leaves 15 powerup vaults (World 1 has 6 vaults, World 2 has 5, Worlds 3 and 4
have 4 each, minus one blueprint per world), giving bits `0x2` to `0x8000` in this fixed order.
Names and cutscene timings come from the user. Each timing is NTSC frames from the first frame of
the final input on the final digit to the first frame Sly can act.

| # | Bit | Powerup | Cutscene |
|---|---|---|---|
| 1 | `0x2` | Dive 1 | 1533 frames (25.55 s) |
| 2 | `0x4` | Roll 1 | 1885 (31.42 s) |
| 3 | `0x8` | Slow 1 | 1862 (31.04 s) |
| 4 | `0x10` | Dive 2 | 1809 (30.15 s) |
| 5 | `0x20` | Magnet | 1899 (31.65 s) |
| 6 | `0x40` | Mine | 1831 (30.52 s) |
| 7 | `0x80` | Fast | 1727 (28.79 s) |
| 8 | `0x100` | Water Safety | 1610 (26.84 s) |
| 9 | `0x200` | Decoy | 1754 (29.24 s) |
| 10 | `0x400` | Roll 2 | 1872 (31.20 s) |
| 11 | `0x800` | Binoc Upgrade | 1813 (30.22 s) |
| 12 | `0x1000` | Slow 2 | 1781 (29.69 s) |
| 13 | `0x2000` | Sneaking | 1863 (31.05 s) |
| 14 | `0x4000` | Void Safety | 1923 (32.05 s) |
| 15 | `0x8000` | Stun | 1876 (31.27 s) |

The other bits:

| Bit | Unlock | Cutscene |
|---|---|---|
| `0x10000` | Invisibility (defeating Mz. Ruby) | |
| `0x20000000` | Blueprint: Tide of Terror | 1857 (30.95 s) |
| `0x40000000` | Blueprint: Sunset Snake Eyes | 1594 (26.57 s) |
| `0x80000000` | Blueprint: Vicious Voodoo | 1491 (24.85 s) |
| `0x10000000` | Blueprint: Fire in the Sky | 1265 (21.09 s) |
| `0x1`, `0x20000` | unknown (tested in `0xBF998` and `0xC0B6C`) | |

`0x2000` (Sneaking) and `0x10000` (Invisibility) are tested together in the guard code. The
blueprint bits come from `GetBlueprintInfo`'s jump table on the current world.

## The Powerups tab

`Sly1PowerupsForm`, like Sly 2-4's Gadgets tab:

- A checked list of the powerups (the bits above, named), with select all / none. Save writes
  `grfvault` (`0x3E8000`), and Discard reads it again. Bits SluMAN doesn't list are kept as they are.
- An **Equipped** box listing the unlocked equippable powerups (and None). It writes `g_fsp`
  (`0x39F9B0`) and `fspLast` (`0x3E800C`). The game drops it if it isn't available.
- Not done: **Allow in every level** (writing `0xFFFFFFFF` to the level's allowed mask,
  `*0xE5E8D8 + 0x235C`, after each load), and saving the set to the run-file config as Sly 3 does
  (`_GadgetUnlocks`).
