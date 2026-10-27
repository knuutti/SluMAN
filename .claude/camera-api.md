# Camera Script API Reference

A small local HTTP API that lets a script on the same PC (Python, a shell `curl`, anything that can make an HTTP GET) read and move a game's free camera, and toggle Hide HUD and No fog. It works in every supported game: Sly 1, Sly 2, Sly 3, Sly 4 and Bentley's Hackpack. The server is `SluMAN/CameraScriptApi.cs`. It drives the game's `IFreeCamera` and the Hide HUD / No fog / far distance delegates from the game's `CameraTabLayout` (`SluMAN/CameraTabLayout.cs`), the same ones the Camera tab uses.

---

## Turning it on

1. Attach SluMAN to the game and open **Practice Mode**. The API only runs while a Practice window is open. Speedrun Mode doesn't have it.
2. On the **Camera** tab, tick **Script API on 127.0.0.1:9675**.

The choice is kept in `config.txt` (`cameraApi = true`), so from then on the API starts whenever a Practice window opens, whichever tab is showing. It stops when the window closes (shutdown, Switch Game/Mode).

| | |
|---|---|
| Address | `http://127.0.0.1:9675/`, loopback only, so nothing else on the network can reach it |
| Port | 9675 by default. Set `cameraApiPort = <port>` in `config.txt` to change it, then untick and tick the box again. If the port is taken, the box stays unticked and the status line says so. |
| Old keys | `sly4CameraApi` / `sly4CameraApiPort` (from when only Sly 4 had the API) are still read if the new keys aren't set |

To check that it's up, run `curl http://127.0.0.1:9675/`.

---

## Protocol

- **GET only.** Any other method gets `405`.
- Parameters go in the **query string**. Names are case-insensitive. Numbers are parsed with an invariant culture (`.` as the decimal point; exponents like `1e3` work). NaN and infinity are rejected.
- Every answer is **JSON** (`Content-Type: application/json; charset=utf-8`), sent with `Connection: close` and `Cache-Control: no-store`. Make one request per connection.
- A trailing `/` on a path is ignored (`/camera/` is `/camera`).
- Requests are handled **one at a time**: they all drive the same camera.
- **Angles are in degrees** on the wire. Internally they're radians.

### Errors

Every error is `{"error": "<message>"}` with one of these statuses:

| Status | When |
|---|---|
| `400 Bad Request` | A value isn't a number, `hidden` / `off` is missing, `fov` is below 0, or `far` was given to a game without a far distance |
| `404 Not Found` | Unknown path |
| `405 Method Not Allowed` | Not a GET |
| `409 Conflict` | The game refused. The free camera couldn't start (a level is still loading, the game has no camera yet, or the camera hook couldn't be put in), the game can't hide the HUD or turn off the fog, or there's no camera to change the fog on yet. The message says which. |

---

## Requests

### `GET /`

Lists the requests this game supports and which world axis is up.

```json
{
  "requests": [
    "/camera",
    "/camera/set?x=&y=&z=&yaw=&pitch=&fov= (any of them; degrees; fov=0 is the game's)",
    "/camera/off",
    "/hud?hidden=1|0",
    "/fog?off=1|0"
  ],
  "upAxis": "Z"
}
```

Sly 2, Sly 4 and the Hackpack list `&far=` in `/camera/set` as well. Use `upAxis` to tell the two coordinate systems apart (see [Coordinates and angles](#coordinates-and-angles)).

### `GET /camera`

Reads the free camera.

```json
{"on": true, "x": 1520.5, "y": -340.0, "z": 812.25, "yaw": 90.0, "pitch": -15.0, "fov": 45.0, "gameFov": 45.0}
```

| Field | Present | Meaning |
|---|---|---|
| `on` | always | Whether the free camera is on |
| `x`, `y`, `z` | when `on` | Camera position in world units |
| `yaw`, `pitch` | when `on` | Facing in degrees (see below) |
| `fov` | when `on` | Field of view in degrees. `0` when the game's couldn't be read (Sly 1–3), in which case the game's own is used and zoom doesn't work |
| `gameFov` | when the game's field of view is known | The game's own field of view in degrees, read when the free camera started |
| `far` | Sly 2, Sly 4, Hackpack | The free camera's far distance. `0` means the game's |
| `gameFar` | Sly 2, Sly 4, Hackpack | The game's own far distance, read when the free camera started (Sly 2: `0` until it has) |

When the camera is off, only `on: false` and whichever of `gameFov` / `far` / `gameFar` apply are returned.

### `GET /camera/set?x=&y=&z=&yaw=&pitch=&fov=&far=`

Moves the free camera, **turning it on first** if it's off (from where the game's camera is now). Every parameter is optional; anything left out keeps its current value. So `/camera/set` with no parameters just turns the camera on. Answers with the same JSON as `/camera`, after the move.

| Parameter | Notes |
|---|---|
| `x`, `y`, `z` | World position |
| `yaw` | Degrees, any value (not wrapped) |
| `pitch` | Degrees. Clamped to −90…90 (straight down to straight up) in every game |
| `fov` | Degrees, above 0 and below 180 in every game: clamped to 0.0001°–179.9999°, since exactly 0° or 180° breaks the projection. `0` means the game's |
| `far` | Sly 2, Sly 4 and Hackpack only (`400` elsewhere). `0` means the game's. It applies only while the free camera is on and stays set when it's turned off and on. In Sly 2 it's the game's render distance (75000 on at least one map), which SluMAN puts back when the camera stops. The PAL version's address hasn't been checked |

Setting the camera **stops any camera path playing** on the Camera tab.

### `GET /camera/off`

Turns the free camera off and hands the view back to the game. Answers with `/camera`'s JSON (`on: false`).

### `GET /hud?hidden=1` or `?hidden=0`

Hide HUD on or off, as the Camera tab's checkbox does. `1` and `true` (any case) mean on; anything else means off. Answers `{"hidden": true|false}` as read back from the game.

| Game | How |
|---|---|
| Sly 1 | Every HUD element's Draw entry is pointed at an empty function |
| Sly 2 / 3 | The HUD's scale X is set to 0 |
| Sly 4 / Hackpack | The `UI.3D.FOV` tunable is set to 0, which hides the pause menu too |

### `GET /fog?off=1` or `?off=0`

No fog on or off. Answers `{"off": true|false}` (what was asked). `409` if the game has no camera yet. In Sly 4 / Hackpack the fog is moved out of sight and kept there after loads.

---

## Coordinates and angles

There are two coordinate systems. Check `upAxis` from `GET /` rather than hard-coding a game.

| | Sly 1, 2, 3 (`upAxis` `"Z"`) | Sly 4, Hackpack (`upAxis` `"Y"`) |
|---|---|---|
| Up | +Z | +Y |
| Look direction | (cos p·cos y, cos p·sin y, sin p) | (cos p·sin y, sin p, cos p·cos y) |
| `yaw` 0 looks along | +X | +Z |
| `yaw` 90 looks along | +Y | +X |
| `pitch` | + looks up, − down | + looks up, − down |
| Scale | Large units: the Camera tab's move speed runs 10–20000 | Small units: the move speed runs 1–200 |

The camera never rolls: it stays level with the horizon.

Converting a look-at point to yaw and pitch:

```python
import math

def look_at(cam, target, up_axis):
    dx, dy, dz = (t - c for t, c in zip(target, cam))
    if up_axis == "Z":      # Sly 1-3
        yaw = math.atan2(dy, dx)
        pitch = math.atan2(dz, math.hypot(dx, dy))
    else:                   # Sly 4 / Hackpack
        yaw = math.atan2(dx, dz)
        pitch = math.atan2(dy, math.hypot(dx, dz))
    return math.degrees(yaw), math.degrees(pitch)
```

---

## Behaviour to plan around

- **Loads turn the free camera off.** It doesn't start again by itself. Poll `/camera` and check `on`, or simply send `/camera/set` again, which turns it back on.
- **The player.** While the camera flies, Sly 1 and Sly 2 make the player uncontrollable and Sly 3 holds the player where they stood. Sly 4 holds the player too, but in its hacking minigames, and in every Hackpack level but the hub, it holds the game's time nearly still instead. Turning the camera off gives control back. (The Camera tab's character control checkbox lets the controller move the character while the camera stays put. The API doesn't change it.)
- **The controller still flies the camera.** The sticks, R1/L1 and R2/L2 keep moving it between your requests. Scripts that want an exact shot should send `/camera/set` with every value.
- **Rate.** Each request is a new TCP connection and a write to game memory. The rate hasn't been measured; start around 30 requests a second for smooth moves and check how it looks, especially on a real PS3 over Ratchetron, which is slower than RPCS3.
- **RPCS3 PPU decoder.** The camera hooks need RPCS3's Interpreter PPU decoder in Sly 3 and Sly 4 / Hackpack, and Sly 1's hook has to go in for its camera to show. Without the hook, Sly 3 falls back to writing the camera directly (it flickers) and Sly 1 has no fallback. The status line on the Camera tab says whether the hook runs.
- **No path API.** Camera paths (keyframes, playback) are only on the Camera tab. A script plays its own path by sending `/camera/set` in a loop, as in the example below.
- Nothing here reads the player or the level. A script that needs those has to work them out itself or ask the user.

---

## Example: orbit a point (Python 3, standard library only)

```python
import json, math, time, urllib.error, urllib.parse, urllib.request

BASE = "http://127.0.0.1:9675"

def get(path, **params):
    url = BASE + path
    if params:
        url += "?" + urllib.parse.urlencode(params)
    try:
        with urllib.request.urlopen(url, timeout=5) as r:
            return json.load(r)
    except urllib.error.HTTPError as e:
        raise RuntimeError(json.load(e).get("error", str(e)))

up = get("/")["upAxis"]
cam = get("/camera/set")                 # turn it on where the game's camera is
center = (cam["x"], cam["y"], cam["z"])
radius = 500 if up == "Z" else 5         # Sly 1-3 units are far larger than Sly 4's

get("/hud", hidden=1)
try:
    start = time.time()
    while time.time() - start < 10:
        a = (time.time() - start) / 10 * 2 * math.pi
        if up == "Z":
            x, y, z = center[0] + radius * math.cos(a), center[1] + radius * math.sin(a), center[2]
        else:
            x, y, z = center[0] + radius * math.sin(a), center[1], center[2] + radius * math.cos(a)
        # Both systems face the center at yaw a + 180 (see look_at above).
        get("/camera/set", x=x, y=y, z=z, yaw=math.degrees(a) + 180, pitch=-10)
        time.sleep(1 / 30)
finally:
    get("/hud", hidden=0)
    get("/camera/off")
```

---

## Adding a game

There's nothing to do in the API itself. A game gets it when its Practice window builds its Camera tab's `CameraTabLayout` once, creates `new CameraScriptApi(layout)` (which sets `layout.scriptApi`), calls `StartFromConfig()`, opens the tab with `new CameraForm(cameraApi.Layout)`, and calls `cameraApi.Stop()` in FormClosing before stopping the free camera. Requests come in on the API's own threads, so the game's `IFreeCamera` and the layout's delegates must be safe to call off the UI thread (every game's are: they lock their state and raise events on the UI thread).
