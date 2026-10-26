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

From the vault order, the table above, and what the code testing each bit does. Names from the
[Sly Cooper Wiki](https://slycooper.fandom.com/wiki/Category:Power-ups_in_Sly_Cooper_and_the_Thievius_Raccoonus):

| Bit | From | Powerup | How sure |
|---|---|---|---|
| `0x1` | ? (not a vault, not a boss) | ? | tested in `0xBF998` |
| `0x2` | vault 1 | Dive | sure (table; the wiki says the first vault) |
| `0x4` | vault 2 | Roll | sure (table; the wiki says the second vault) |
| `0x8` | vault 3 | Slow (in the air) | sure (table) |
| `0x10` | vault 4 | Dive upgrade (in mid-air too, collects coins) | sure (table; the wiki says the fourth vault) |
| `0x20` | vault 5 | ? passive | tested in `0x4E2BC` |
| `0x40` | vault 6 | Mine | sure (table) |
| `0x80` | vault 7 | Fast | sure (table; the wiki says the seventh vault) |
| `0x100` | vault 8 | ? passive | tested in Sly's code, `0x125388` |
| `0x200` | vault 9 | Decoy | sure (table) |
| `0x400` | vault 10 | Roll upgrade | sure (table) |
| `0x800` | vault 11 | ? passive | tested in `0x2B95C`, `0x2EA98` (Sly's update) |
| `0x1000` | vault 12 | Slow upgrade (on the ground too) | sure (table; the slow code tests it) |
| `0x2000` | vault 13 | Shadow Power? (moving while invisible) | tested with `0x10000` in guard code |
| `0x4000` | vault 14 | ? passive | tested in `0x125388` |
| `0x8000` | vault 15 | Stun | sure (table; the wiki says it comes last) |
| `0x10000` | Mz. Ruby | Invisibility? | sure it's her reward; the name is a guess |
| `0x20000` | ? | ? | tested in `0xC0B6C` |
| `0xF0000000` | ? | ? (`0xBE2E4` tests these together) | |

The other passives on the wiki are Coin Magnet, Water Safety, Computer Hacking and Defy Gravity.
They're probably among `0x20`, `0x100`, `0x800` and `0x4000`. Telling which needs the game: set
`0x3E8000` to one bit and look at the pause menu's Thievius Raccoonus.

## A Powerups tab

Like Sly 2-4's Gadgets tab:

- A checked list of the powerups (the bits above, named), with select all / none. Save writes
  `grfvault` (`0x3E8000`), and Discard reads it again. Bits SluMAN doesn't list are kept as they are.
- An **Equipped** box listing the unlocked equippable powerups (and None). It writes `g_fsp`
  (`0x39F9B0`) and `fspLast` (`0x3E800C`). The game drops it if it isn't available.
- Perhaps **Allow in every level**: write `0xFFFFFFFF` to the level's allowed mask
  (`*0xE5E8D8 + 0x235C`) after each load, so unlocked powerups also work where the level would
  block them. A load puts the level's own mask back.
- Saving the set to the run-file config as Sly 3 does (`_GadgetUnlocks`) could come later.
