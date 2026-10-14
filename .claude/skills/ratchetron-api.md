# Skill: ratchetron-api

## Purpose

Load this skill when you need to write, review, or debug any code that uses the Ratchetron PS3 memory API in the SluMAN project. It gives you instant access to the complete protocol and C# surface so you never have to guess at method signatures, byte ordering, or packet formats.

## When to activate

- Adding a new memory subscription (`SubMemory`) or freeze (`FreezeMemory`)
- Reading or writing PS3 memory from a game class or form
- Setting up or tearing down the data channel
- Debugging a subscription not firing or firing too often
- Writing new game classes that inherit `IGame`
- Any time the user asks about `IPS3API`, `Ratchetron`, `func.api`, memory conditions, or subscription IDs

## What to read

Load the full reference before responding:

**`.claude/ratchetron-api.md`** — complete API reference covering:
- Connection and handshake sequence
- `ReadMemory` / `WriteMemory` with byte-order rules
- `SubMemory` overloads, condition enum, callback byte ordering
- `FreezeMemory` overloads
- `ReleaseSubID` and cleanup rules
- `Notify`, `WriteFile`, `getGameTitleID`, `getCurrentPID`
- Disconnect/reconnect callbacks
- Startup sequence (`PrepareRatchetron`)
- Full limits table

## Critical rules to apply

1. **Byte order:** `ReadMemory` returns big-endian bytes. Always `.Reverse().ToArray()` before passing to `BitConverter`. `SubMemory`/`FreezeMemory` callbacks are already reversed by the client — use them directly.

2. **Subscription size limit:** Never subscribe to more than 8 bytes. For larger values, use multiple subscriptions or poll with `ReadMemory`.

3. **Always release:** Store every subscription ID returned by `SubMemory` and `FreezeMemory`. Release all of them in the form's `FormClosed` handler or disconnect handler via `api.ReleaseSubID(id)`.

4. **Default condition:** Use `MemoryCondition.Changed` for notification subscriptions (avoids callback flooding). Use `MemoryCondition.Any` for unconditional freeze.

5. **Data channel first:** `OpenDataChannel()` must be called before any `SubMemory` call. It is called inside `func.PrepareRatchetron()` — do not call it again.

6. **Large reads:** Use chunked reads (0x8000 bytes per chunk) for addresses that hold large data structures. Never attempt a single `ReadMemory` of more than ~2048 bytes.

7. **`getCurrentPID()`:** This sends a command over TCP every time it is called. Cache the result in a local variable inside the constructor rather than calling it in every tick.

## Typical subscription pattern

```csharp
// In game class constructor or SetupInputDisplayMemorySubsButtons():
int subID = api.SubMemory(pid, addr.inputOffset, 4, (value) =>
{
    // value bytes are already little-endian — pass directly to BitConverter
    int mask = BitConverter.ToInt32(value, 0);
    Inputs.RawInputs = ConvertButtonsToStandardFormat(mask);
});

// In FormClosed / disconnect:
api.ReleaseSubID(subID);
```

## Typical freeze pattern

```csharp
int freezeSubID = api.FreezeMemory(pid, healthAddress, 100u);
// health is now locked at 100 (patched every ~8 ms)

// To unfreeze:
api.ReleaseSubID(freezeSubID);
```
