"""Turns a square piece of icon artwork into the three files Android wants.

    python tools/icon-from-artwork.py "Assets/PuttSeed/Icon/<artwork>.png"

Needs Pillow (`python -m pip install pillow`). This is an art utility, not
part of the game or its build — it exists so the shipped icon can be REMADE
rather than only admired, which is the difference between an asset and a
lucky download.

What it does, and why each step is there:

* Keys out the flat background by flooding in from the frame, so a green
  inside the artwork (the grass) survives while the green behind it does not.
* Reframes so the subject fills 88% of the square. Generators leave polite
  margins; a launcher icon that leaves them reads as small and timid next to
  its neighbours.
* Pulls strong reds onto the game's own flag colour (#DB3D30), keeping each
  pixel's brightness, so the icon and the game agree about what red is.
* Builds the adaptive foreground at 60% scale on transparency and MEASURES
  the result against the mask a round launcher applies: everything must sit
  within a third of the canvas from the centre, or the flag loses its tip.

The adaptive background is not made here — it is the felt the game draws
itself, via PuttSeed → Generate Store Art.
"""

import sys
from collections import deque

from PIL import Image

GAME_RED = (0xDB, 0x3D, 0x30)
ICON_SIZE = 512
ADAPTIVE_SIZE = 432
SUBJECT_FILL = 0.88       # of the square icon
ADAPTIVE_FILL = 0.60      # of the adaptive canvas, before the launcher magnifies it
SAFE_FRACTION = 0.333     # the guaranteed-visible radius of an adaptive layer
OUT_DIR = "Assets/PuttSeed/Icon"


def background_mask(image, size=512, tolerance=26):
    """True where the pixel is background reachable from the frame."""
    work = image.resize((size, size), Image.LANCZOS)
    pixels = work.load()
    background = pixels[2, 2]

    def matches(colour):
        return all(abs(colour[i] - background[i]) <= tolerance for i in range(3))

    outside = [[False] * size for _ in range(size)]
    queue = deque()
    for i in range(size):
        for x, y in ((i, 0), (i, size - 1), (0, i), (size - 1, i)):
            if not outside[y][x] and matches(pixels[x, y]):
                outside[y][x] = True
                queue.append((x, y))

    while queue:
        x, y = queue.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < size and 0 <= ny < size and not outside[ny][nx] and matches(pixels[nx, ny]):
                outside[ny][nx] = True
                queue.append((nx, ny))

    return outside


def redden(image, source_peak=187.0):
    """Maps the artwork's red onto the game's, brightness by brightness."""
    pixels = image.load()
    width, height = image.size
    for y in range(height):
        for x in range(width):
            value = pixels[x, y]
            r, g, b = value[:3]
            if r > 90 and r > g * 1.7 and r > b * 1.7:
                shade = r / source_peak
                pixels[x, y] = tuple(min(255, int(c * shade)) for c in GAME_RED) + tuple(value[3:])


def main(path):
    art = Image.open(path).convert("RGB")
    full = art.size[0]
    size = 512
    outside = background_mask(art, size)

    xs = [x for y in range(size) for x in range(size) if not outside[y][x]]
    ys = [y for y in range(size) for x in range(size) if not outside[y][x]]
    x0, x1, y0, y1 = min(xs), max(xs), min(ys), max(ys)
    centre_x, centre_y = (x0 + x1) / 2, (y0 + y1) / 2
    side = max(x1 - x0 + 1, y1 - y0 + 1) / SUBJECT_FILL
    scale = full / size

    icon = art.crop((
        int((centre_x - side / 2) * scale), int((centre_y - side / 2) * scale),
        int((centre_x + side / 2) * scale), int((centre_y + side / 2) * scale),
    )).resize((ICON_SIZE, ICON_SIZE), Image.LANCZOS)
    redden(icon)
    icon.save(f"{OUT_DIR}/app-icon.png")

    mask = Image.frombytes("L", (size, size), bytes(
        0 if outside[y][x] else 255 for y in range(size) for x in range(size))).resize(art.size, Image.LANCZOS)
    cut = Image.new("RGBA", art.size, (0, 0, 0, 0))
    cut.paste(art, (0, 0), mask)
    cut = cut.crop((int(x0 * scale), int(y0 * scale), int((x1 + 1) * scale), int((y1 + 1) * scale)))
    redden(cut)

    target = int(ADAPTIVE_SIZE * ADAPTIVE_FILL)
    factor = target / max(cut.size)
    cut = cut.resize((max(1, int(cut.size[0] * factor)), max(1, int(cut.size[1] * factor))), Image.LANCZOS)
    foreground = Image.new("RGBA", (ADAPTIVE_SIZE, ADAPTIVE_SIZE), (0, 0, 0, 0))
    foreground.paste(cut, ((ADAPTIVE_SIZE - cut.size[0]) // 2, (ADAPTIVE_SIZE - cut.size[1]) // 2), cut)
    foreground.save(f"{OUT_DIR}/adaptive-fg.png")

    pixels = foreground.load()
    worst = 0.0
    for y in range(ADAPTIVE_SIZE):
        for x in range(ADAPTIVE_SIZE):
            if pixels[x, y][3] > 24:
                dx, dy = x + 0.5 - ADAPTIVE_SIZE / 2, y + 0.5 - ADAPTIVE_SIZE / 2
                worst = max(worst, (dx * dx + dy * dy) ** 0.5)

    limit = ADAPTIVE_SIZE * SAFE_FRACTION
    print(f"icon written; adaptive subject reaches {worst:.0f}px of a {limit:.0f}px mask", end=" ")
    print("(inside)" if worst <= limit else "(OUTSIDE — a round launcher will clip it)")


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else f"{OUT_DIR}/artwork.png")
