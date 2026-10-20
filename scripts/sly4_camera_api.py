"""
Drives SluMAN's Sly 4 free camera from Python, through SluMAN's script API.

Turn the API on first: Camera tab -> "Script API on 127.0.0.1:9675". It only answers on this PC.
Needs nothing beyond Python 3.

    from sly4_camera_api import SluCamera
    cam = SluCamera()
    print(cam.get())                                  # {'on': True, 'x': ..., 'fov': ...}
    cam.set(x=100, y=800, z=-50, yaw=0, pitch=-90, fov=3)
    cam.hud(hidden=True)
    cam.fog(off=True)

Angles are in degrees. set() turns the free camera on if it's off; values left out stay as they
are. far=0 puts the game's own far distance back.

Run this file directly for a grid capture: it flies a straight-down camera over a rectangle in
tiles, so each image is nearly orthographic, and presses RPCS3's screenshot key (F12 by default)
at each one. RPCS3 has to be the focused window while it runs.
"""
import json
import math
import time
import urllib.error
import urllib.parse
import urllib.request


class SluCamera:
    def __init__(self, host="127.0.0.1", port=9675, timeout=5):
        self.base = f"http://{host}:{port}"
        self.timeout = timeout

    def _get(self, path, **params):
        params = {k: v for k, v in params.items() if v is not None}
        url = self.base + path
        if params:
            url += "?" + urllib.parse.urlencode(params)
        try:
            with urllib.request.urlopen(url, timeout=self.timeout) as response:
                return json.loads(response.read().decode("utf-8"))
        except urllib.error.HTTPError as error:
            body = json.loads(error.read().decode("utf-8"))
            raise RuntimeError(body.get("error", str(error))) from None

    def get(self):
        """The free camera: on, x, y, z, yaw, pitch, fov, far, gameFov, gameFar."""
        return self._get("/camera")

    def set(self, x=None, y=None, z=None, yaw=None, pitch=None, fov=None, far=None):
        """Moves the free camera (turning it on). Returns the camera as get() does."""
        return self._get("/camera/set", x=x, y=y, z=z, yaw=yaw, pitch=pitch, fov=fov, far=far)

    def off(self):
        """Turns the free camera off."""
        return self._get("/camera/off")

    def hud(self, hidden=True):
        return self._get("/hud", hidden=1 if hidden else 0)

    def fog(self, off=True):
        return self._get("/fog", off=1 if off else 0)


def ground_width(height, fov_degrees, aspect=16 / 9):
    """
    How much ground a straight-down camera at `height` above it sees, as (width, depth). fov is
    the vertical field of view, as the game uses it; aspect is the image's width / height.
    """
    depth = 2 * height * math.tan(math.radians(fov_degrees) / 2)
    return depth * aspect, depth


def press_screenshot_key(virtual_key=0x7B):
    """Presses a key (F12 by default, RPCS3's screenshot key) in the focused window. Windows only."""
    import ctypes
    user32 = ctypes.windll.user32
    user32.keybd_event(virtual_key, 0, 0, 0)
    time.sleep(0.05)
    user32.keybd_event(virtual_key, 0, 2, 0)


def capture_grid(cam, x_min, x_max, z_min, z_max, height, ground_y=0.0, fov=2.0, overlap=0.1, settle=0.5, yaw=0.0):
    """
    Flies a straight-down camera over the rectangle x_min..x_max, z_min..z_max (world units, Y up)
    at `height` (ground at `ground_y`), one tile per image, with `overlap` (a fraction) between neighbours, and takes a
    screenshot at each tile after `settle` seconds. A small fov from high up gives a nearly
    orthographic image; raise far if the ground gets clipped.
    """
    width, depth = ground_width(height - ground_y, fov)
    step_x = width * (1 - overlap)
    step_z = depth * (1 - overlap)
    columns = max(1, math.ceil((x_max - x_min) / step_x))
    rows = max(1, math.ceil((z_max - z_min) / step_z))
    print(f"Each image covers {width:.1f} x {depth:.1f}; {columns} x {rows} = {columns * rows} images.")

    cam.set(fov=fov, far=(height - ground_y) * 4)
    for row in range(rows):
        for column in range(columns):
            x = x_min + step_x * (column + 0.5)
            z = z_min + step_z * (row + 0.5)
            cam.set(x=x, y=height, z=z, yaw=yaw, pitch=-90)
            time.sleep(settle)
            press_screenshot_key()
            print(f"row {row + 1}/{rows}, column {column + 1}/{columns}: x={x:.1f} z={z:.1f}")
            time.sleep(0.3)


if __name__ == "__main__":
    camera = SluCamera()
    print("Camera now:", camera.get())
    # Example: an area of Japan Village. Read real bounds with camera.get() while flying around.
    # camera.hud(hidden=True)
    # camera.fog(off=True)
    # capture_grid(camera, x_min=-200, x_max=200, z_min=-200, z_max=200, height=1500, fov=2)
