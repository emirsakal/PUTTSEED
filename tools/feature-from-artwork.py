"""Composes the Play feature graphic (1024x500) from the icon artwork.

    python tools/feature-from-artwork.py

Needs Pillow. Reads Assets/PuttSeed/Icon/artwork.png, writes
docs/store/feature-graphic.png.

The icon became a rendered diorama; the feature graphic was still the flat
vector scene StoreArt draws, and side by side on a store page the two read
as two different games. This puts the SAME island on the store's banner:
felt background in the game's own stripes (the palette StoreArt uses), the
island keyed out of its artwork with the icon script's flood mask, its flag
pulled onto the game's red the same way, placed right of centre with a
soft drop shadow so it sits ON the green rather than floating over it. No
text — the store prints the title beside this image, and text baked into a
picture cannot be localized.
"""

import importlib.util
import os

from PIL import Image, ImageDraw, ImageFilter

WIDTH, HEIGHT = 1024, 500
FELT = (0x38, 0x85, 0x4F)
FELT_LIGHT = (0x3E, 0x8E, 0x55)
ROUGH = (0x2F, 0x6F, 0x42)
ARTWORK = "Assets/PuttSeed/Icon/artwork.png"
OUTPUT = "docs/store/feature-graphic.png"


def load_icon_tools():
    """The icon script's mask and redden, without renaming the file."""
    spec = importlib.util.spec_from_file_location(
        "icon_from_artwork", os.path.join("tools", "icon-from-artwork.py"))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def felt_background():
    image = Image.new("RGBA", (WIDTH, HEIGHT), FELT + (255,))
    draw = ImageDraw.Draw(image)
    stripe = int(HEIGHT * 0.16)
    for y in range(0, HEIGHT, stripe * 2):
        draw.rectangle((0, y, WIDTH, y + stripe), fill=FELT_LIGHT + (255,))

    # The rough washes: enormous, faint, edges well outside the frame so no
    # arc ever shows — the lesson the first feature graphic taught.
    wash = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 0))
    wash_draw = ImageDraw.Draw(wash)
    r1 = int(HEIGHT * 1.45)
    wash_draw.ellipse((-int(WIDTH * 0.05) - r1, int(HEIGHT * 1.25) - r1,
                       -int(WIDTH * 0.05) + r1, int(HEIGHT * 1.25) + r1), fill=ROUGH + (56,))
    r2 = int(HEIGHT * 1.35)
    wash_draw.ellipse((int(WIDTH * 1.02) - r2, -int(HEIGHT * 0.3) - r2,
                       int(WIDTH * 1.02) + r2, -int(HEIGHT * 0.3) + r2), fill=ROUGH + (46,))

    # Blurred hard, so the washes are gradients with no rim at all — a disc
    # edge crossing the frame read as a crease in the first render.
    wash = wash.filter(ImageFilter.GaussianBlur(90))
    return Image.alpha_composite(image, wash)


def main():
    tools = load_icon_tools()
    art = Image.open(ARTWORK).convert("RGB")
    size = 512
    outside = tools.background_mask(art, size)

    xs = [x for y in range(size) for x in range(size) if not outside[y][x]]
    ys = [y for y in range(size) for x in range(size) if not outside[y][x]]
    x0, x1, y0, y1 = min(xs), max(xs), min(ys), max(ys)
    scale = art.size[0] / size

    mask = Image.frombytes("L", (size, size), bytes(
        0 if outside[y][x] else 255 for y in range(size) for x in range(size))).resize(art.size, Image.LANCZOS)
    island = Image.new("RGBA", art.size, (0, 0, 0, 0))
    island.paste(art, (0, 0), mask)
    island = island.crop((int(x0 * scale), int(y0 * scale), int((x1 + 1) * scale), int((y1 + 1) * scale)))
    tools.redden(island)

    target_height = int(HEIGHT * 0.86)
    factor = target_height / island.size[1]
    island = island.resize((int(island.size[0] * factor), target_height), Image.LANCZOS)

    banner = felt_background()

    # A soft shadow under the island, offset down-right like every shadow in
    # the game, so the diorama sits on the felt instead of hovering.
    shadow = Image.new("RGBA", banner.size, (0, 0, 0, 0))
    alpha = island.split()[3]
    shadow_layer = Image.new("RGBA", island.size, (0, 0, 0, 110))
    shadow_layer.putalpha(alpha.point(lambda a: a * 110 // 255))
    shadow.paste(shadow_layer, (int(WIDTH * 0.64 - island.size[0] / 2) + 14,
                                int(HEIGHT * 0.5 - island.size[1] / 2) + 18))
    shadow = shadow.filter(ImageFilter.GaussianBlur(14))
    banner = Image.alpha_composite(banner, shadow)

    banner.paste(island, (int(WIDTH * 0.64 - island.size[0] / 2),
                          int(HEIGHT * 0.5 - island.size[1] / 2)), island)

    os.makedirs(os.path.dirname(OUTPUT), exist_ok=True)
    banner.convert("RGB").save(OUTPUT)
    print(f"feature graphic written: {OUTPUT} ({WIDTH}x{HEIGHT})")


if __name__ == "__main__":
    main()
