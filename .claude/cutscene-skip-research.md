# Sly 2 and 3 cutscene skip research

The PS4/PS5 release of Sly 2 shipped a cutscene skip on START in its emulator's Lua patches.
[andshrew/Sly2-Cutscene-Skip](https://github.com/andshrew/Sly2-Cutscene-Skip) has those scripts and
a PS2 port on SELECT. SluMAN does the same thing on PS3 by a different mechanism (`SeqeSkipWindow`). Sly 3 has no official
skip that we know of; it's at the end of this file.

## What the PS4 method does

A cutscene is a script ("seq") run by threads (`CSeqThread`). Each thread sits on one command
("seqe"). `UpdateSeqe` loops: unless the command says not to wait, ask `FIsSeqeComplete`; if it's
done, go to the next command and run it; otherwise return until next frame.

For the next 30 checks after a press, the patch forces `FIsSeqeComplete` to true when the command
is 59, 69 or 82 and its `+4` field is 0. The script then runs through its dialogue waits, often in
one frame, until a wait of another kind holds it. The old SluMAN skip only ended one dialogue line.

A separate hook sets the MPEG player's end flag for FMVs. SluMAN's `fmvState` write already covers
those (Bink on PS3).

## Where it is on PS3 (NPHA80175, from EBOOT.elf)

Each command is 0x50 bytes: `+0` the command, `+4` 0 to wait for it, then arguments from `+0x10`.

| What | Address |
|------|---------|
| `FIsSeqeComplete(thread r3, seqe r4)`; returns r3 | 0x1002AC |
| Its switch (`cmplwi cr7, r0, 0x57`, command - 3) | 0x1002E0 |
| Its jump table (offsets from the table's start) | 0x100304 |
| Default case, returns 1 (`li r29, 1`) | 0x1004D8, entry value 0x1D4 |
| `UpdateSeqe(thread)`; wait check at 0x101438 and 0x101444 | 0x1013C4 |
| Finishes the thread's pending command at `+0x40`, which also calls it (0x101360) | 0x101338 |
| Next command (thread `+0x30` += 0x50) | 0x101674 |
| Runs the command's start | 0x10167C |
| `CSeq` update (threads in a list at `+0x4C`) | 0x104450 |

| Command | Table entry | Value | What the case waits for |
|---------|-------------|-------|-------------------------|
| 59 | 0x1003E4 | 0x8C8 | Object at thread `+0xA0`, its `+0xF08` |
| 69 | 0x10040C | 0xA48 | A handle (`+0x10`), on the object at 0x7B09C0 |
| 82 | 0x100440 | 0x9C4 | A time on that object reaching `+0x10`, handle from thread `+0x68` |

The command numbers come from the PS2 scripts, and these three have cases of their own here. That
they wait on dialogue and voice is inferred from the static code, not seen in a running game.

## Why the jump table

A code hook on the `bl` at 0x101444 would be closest to the original, but RPCS3 only runs hook stubs
with the Interpreter decoder. The table is read with `lwzx` each time the function runs, and 0x1004D8
is already one of its targets, so writing it should work with any decoder. Not yet tried on a
recompiler or a PS3.

Differences from the PS4 patch:

- The window is 500 ms (`SeqeSkipWindow.WindowMs`), not 30 checks: nothing runs in the game to count them.
  Commands can chain further within it.
- It also answers yes to the check for a thread's pending command (0x101360).
- There's no hook for a sequence ending (0x20F060 on NTSC PS2, not found on PS3). The window just
  times out, so a cutscene starting within it gets skipped too.

## Other versions

`SeqeSkipWindow.FindTable` searches 0xFF000 to 0x101000 for the switch and checks the default case and each
command's first instruction. If it isn't found, Skip Cinematic falls back to the old dialogue write,
and says so on the console.

Checked against each version's decrypted EBOOT; the switch occurs once in each, the entry values are
the same (0x8C8, 0xA48, 0x9C4), and every check `FindTable` makes passes:

| Version | Switch | Table | Default case |
|---------|--------|-------|--------------|
| KOR (NPHA80175) | 0x1002E0 | 0x100304 | 0x1004D8 |
| PAL (NPEA00342) | 0xFFE70 | 0xFFE94 | 0x100068 |
| NTSC (NPUA80664) | 0xFFE6C | 0xFFE90 | 0x100064 |

The skip has only been run on KOR.

The code that loads `fmvState` and `inputOffset` through the TOC was also compared across the EBOOTs.
It gives PAL's existing values (0x90667C, 0x500EF6) and, for NTSC, KOR's (0x9066FC, 0x500F76), which
is what NTSC already uses.

## Known limits

From the PS4 release's reports, likely the same here since the skip is the same:

- Episode 4, Eavesdrop on Contessa, last cutscene: skipping early doesn't spawn the Train Hack job.
  Wait for Bentley to turn around.
- Episode 6, Aerial Assault, first cutscene on the train: the chopper doesn't get into position.
- Skipping on the first possible frame of some job-ending cutscenes can softlock (Steal a Tuxedo,
  Cabin Crimes).

## Sly 3 (NPEA00343, from EBOOT.elf)

The same code, with its own command numbers. Unlike Sly 2's, this hasn't been checked against an
official skip: the commands come from comparing the code, and no list of cutscenes that break exists.

Commands are packed: `+0` is a word with the command in its top byte and a flag `0x00800000` that
means wait for it, then arguments from `+8`.

| What | Address |
|------|---------|
| `FIsSeqeComplete(thread r3, seqe r4)`; returns r3 | 0x154B64 |
| Its switch (`cmplwi cr7, r0, 0x68`, command - 12) | 0x154BB0 |
| Its jump table | 0x154BD4 |
| Default case, returns 1 (`li r28, 1`) | 0x154DE4, entry value 0x210 |
| `UpdateSeqe(thread)`; wait check at 0x1560D8 | 0x156058 |
| Finishes the thread's pending command at `+0x48`, which also calls it (0x155FF4) | 0x155FCC |
| Voice manager; 0x17FE1C and 0x180390 read it | 0x788A90 |

| Sly 2 | Sly 3 | Table entry | Value | Why |
|-------|-------|-------------|-------|-----|
| 59 | 70 | 0x154CBC | 0x24C | Both start by looking up an object with type 0x405, and wait on the dialogue object's flag (`+0xF08` / `+0xF70`) |
| 69 | 82 | 0x154CEC | 0xFD0 | Both start a sound, and wait in "is it playing" (0x12218C / 0x17FE1C, the same code) |
| 82 | 91 | 0x154D10 | 0x27C | Neither has a start action; both wait for a voice's time to reach a value, though Sly 3 reads the time through a stack of voices (0x180390) |
| none | 95 | 0x154D20 | 0xD58 | Waits for any voice to finish (`17FE1C(-1)`) |

Left out for now: 77 shares 70's wait and 88 shares 91's, but their starts do something else.

The old skip's `dialogueState` (0x788CB8) is the voice manager's current voice pointer (`+0x228`).
Writing 0 there makes "is it playing" say no, which only gets past 82 and 95, not the dialogue object
wait (70).
