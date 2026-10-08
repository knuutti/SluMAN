# Ratchetron API Reference

Ratchetron is a PS3 memory-access server loaded as a `.sprx` (PRX module) on jailbroken PS3 consoles via webMAN. SluMAN connects to it over TCP and communicates using a custom binary protocol. The C# client is `RaCTrainer/Memory/Ratchetron.cs`; the abstract interface it implements is `RaCTrainer/Memory/IPS3API.cs`.

---

## Connection

**Port:** TCP 9671 (fixed, hardcoded on both sides)

**Handshake:**
```
← 0x01               (CONNECTED byte)
← 0x02 [rev:4]       (VERSION — revision is little-endian uint32; current value is 4)
```
SluMAN reads 6 bytes on connect, checks byte 0 == `0x01` and revision >= 2, then considers the connection live.

**Data channel (UDP — required for subscriptions):**
```
→ 0x09 [port:4]      (big-endian uint32 — client's chosen UDP listen port)
← 0x80               (success; 0x02 = already open, anything else = error)
```
SluMAN tries ports 4000–5000 and opens a background thread to receive UDP packets. Call `OpenDataChannel()` once after `Connect()` before registering any subscriptions.

**PID:** After connecting, call `getCurrentPID()` which calls `GetPIDList()` and returns `pids[2]` (index 2 = the running game process). PIDs are big-endian in the wire format; the client reverses them.

---

## IPS3API — full method surface

All game code talks to `func.api` which is typed as `IPS3API`. The concrete type is `Ratchetron` on real PS3.

### Core memory

```csharp
// Read size bytes from address in pid's process memory. Returns raw big-endian bytes.
byte[] ReadMemory(int pid, uint address, uint size)

// Convenience: returns hex string representation
string ReadMemoryStr(int pid, uint address, uint size)

// Write memory. All three overloads ultimately call the abstract version.
void WriteMemory(int pid, uint address, uint size, byte[] memory)   // abstract — sends raw bytes
void WriteMemory(int pid, uint address, byte[] memory)              // infers size from array length
void WriteMemory(int pid, uint address, uint size, string memory)   // hex string → bytes
void WriteMemory(int pid, uint address, UInt32 intValue)            // 4-byte big-endian write
```

**Read wire format:**
```
→ 0x04 [pid:4] [addr:4] [size:4]   (all big-endian)
← [size bytes of raw memory]
```

**Write wire format:**
```
→ 0x05 [pid:4] [addr:4] [size:4] [bytes:size]   (all big-endian)
(no response)
```

**Limits:** No single read should exceed ~2048 bytes (server recv buffer). For larger reads use the `ReadLarge()` helper in `LuaFunctions` which chunks at 0x8000 bytes.

**Byte order:** PS3 is big-endian. `ReadMemory` returns raw big-endian bytes — always call `.Reverse().ToArray()` before passing to `BitConverter`. `WriteMemory(pid, addr, UInt32)` handles the reversal for you.

---

### Memory subscriptions

The subscription system lets the server notify the client whenever a memory address changes (or meets a condition). Callbacks arrive over UDP.

```csharp
// Full overload — subscribe with explicit condition and comparison value
int SubMemory(int pid, uint address, uint size, MemoryCondition condition, byte[] memory, Action<byte[]> callback)

// Common shorthand — defaults to Changed, zeroed comparison value
int SubMemory(int pid, uint address, uint size, Action<byte[]> callback)

// Explicit condition, zeroed comparison value
int SubMemory(int pid, uint address, uint size, MemoryCondition condition, Action<byte[]> callback)
```

Returns an `int` subscription ID. Store it to release later.

The `callback` receives `byte[]` — the new value already reversed to little-endian by the client, so it is ready for `BitConverter` without further reversal.

**Wire format:**
```
→ 0x0a [pid:4] [addr:4] [size:4] [cond:1] [condValue:size]
← [subID:4]   (big-endian)
```

**UDP callback packet** (sent by server when condition triggers):
```
← 0x06 [subID:4] [size:4] [tick:4] [value:size]
```
The client deduplicates by tick — if `tick == lastTick` the callback is not fired (server resends every ~500 ms for reliability).

**Size limit:** 8 bytes maximum per subscription (server-side `u64` comparison). Attempting > 8 bytes causes the server to return `0xffffffff`.

**Polling rate:** Server checks subscriptions at ~120 Hz (8.33 ms).

---

### Memory freeze

Freeze continuously patches a memory address back to a fixed value at 120 Hz.

```csharp
// Full — freeze with condition and explicit value
int FreezeMemory(int pid, uint address, uint size, MemoryCondition condition, byte[] memory)

// Shorthand — freeze 4-byte uint with any condition
int FreezeMemory(int pid, uint address, UInt32 intValue)

// Freeze 4-byte uint with explicit condition
int FreezeMemory(int pid, uint address, MemoryCondition condition, UInt32 intValue)
```

Returns a subscription ID (same pool as `SubMemory`). Release with `ReleaseSubID()`.

**Wire format:**
```
→ 0x0b [pid:4] [addr:4] [size:4] [cond:1] [value:size]
← [subID:4]
```

Freeze sends no UDP callbacks — it just silently patches memory. The condition gates *when* patching happens (e.g., `MemoryCondition.Any` = always patch).

---

### Memory conditions

```csharp
public enum MemoryCondition : byte
{
    Any      = 1,   // Always trigger — use for unconditional freeze/subscribe
    Changed  = 2,   // Trigger when value differs from last seen  ← default for SubMemory
    Above    = 3,   // Trigger when current > conditionValue
    Below    = 4,   // Trigger when current < conditionValue
    Equal    = 5,   // Trigger when current == conditionValue
    NotEqual = 6,   // Trigger when current != conditionValue
}
```

`Changed` is the correct default for input subscriptions (fires on every button press/release without flooding). `Any` is the correct default for freeze (patches every tick). `Equal`/`NotEqual` are rarely useful for freeze.

---

### Releasing subscriptions

```csharp
void ReleaseSubID(int memSubID)   // cancel one subscription
// (no public ReleaseAllSubs in IPS3API — it's internal to Ratchetron)
```

**Wire format:**
```
→ 0x0c [subID:4]
← 0x01
```

Always release subscription IDs when the form closes or the game disconnects. Failing to release leaks server-side state and can cause callbacks on stale addresses.

---

### Game identity

```csharp
string getGameTitleID()   // returns "NPEA00343" etc, empty string if not in game
int getCurrentPID()       // returns pids[2] from the PID list (the game process)
```

`getGameTitleID()` sends `0x06`, reads 16 bytes, strips null bytes. `getCurrentPID()` sends `0x03` (LIST_PROCESSES), reads 64 bytes (16 × 4-byte PIDs), and returns index 2.

---

### Notifications

```csharp
void Notify(string message)   // displays a VSH popup on the PS3 screen
```

**Wire format:**
```
→ 0x02 [len:4] [ascii_bytes] [0x00]   (len includes the null terminator)
```

Max 2048 bytes (including null). Used to confirm connection and report tool version.

---

### File I/O

```csharp
void WriteFile(string remotePath, byte[] buffer)    // write bytes to PS3 filesystem path
void WriteFile(string remotePath, string filePath)  // write a local file to PS3
```

Opens a remote file (`0x10`), writes in 2048-byte chunks (`0x11`), then sends a zero-length write to close. File is always created/truncated (no append). `remotePath` is an absolute PS3 path such as `/dev_hdd0/tmp/patch.bin`.

---

### Disconnect/reconnect callbacks (Ratchetron-specific)

```csharp
void setDisconnectCallback(Action action)   // called when IS_INGAME goes 0 (game exits)
void setReconnectCallback(Action action)    // called when IS_INGAME goes 1 (game launches)
```

These arrive over the UDP data channel (`0x08` packet). Wire format:
```
← 0x08 [enteringOrLeaving:1]   (0 = leaving game, 1 = entering game)
```

SluMAN forms use these to pause/resume their timers and re-subscribe when the game restarts.

---

## Startup sequence

```csharp
func.api = new Ratchetron(ip);
func.PrepareRatchetron(ip);    // opens the connection and data channel; see func.cs

// Inside PrepareRatchetron():
if (!func.api.Connect()) { /* show error */ return false; }
((Ratchetron)func.api).OpenDataChannel();
```

After `OpenDataChannel()` returns, subscriptions are fully functional.

---

## Limits and quirks summary

| Item | Limit / note |
|------|-------------|
| TCP port | 9671 (fixed) |
| UDP port | 4000–5000 (client picks first available) |
| Single read max | ~2048 bytes (use chunked reads for larger) |
| Single write max | 64 KB |
| Subscription data size | 8 bytes max |
| Subscription polling rate | ~120 Hz (8.33 ms) |
| UDP resend interval | ~500 ms (60 ticks) |
| PID list size | 16 entries; game is always at index 2 |
| Notify max length | 2048 bytes including null terminator |
| Protocol byte order | All multi-byte values big-endian on the wire |
| `AllocatePage` (0x0E) | Server command never worked; the C# client no longer has a method for it |
| Thread safety | Each request and its reply run under one lock in `Ratchetron`, so the UI, Lua mods and subscription callbacks can share the connection |
| Subscriptions on disconnect | Destroyed server-side; must re-subscribe after reconnect |
| `SubMemory` callback bytes | Already reversed by client — pass directly to `BitConverter` |
| `ReadMemory` return bytes | Raw big-endian — must `.Reverse()` before `BitConverter` |
