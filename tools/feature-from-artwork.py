"""Composes the banner art from the icon artwork, in two sizes.

    python tools/feature-from-artwork.py             # Play feature graphic
    python tools/feature-from-artwork.py --social    # GitHub social preview

Needs Pillow. Reads Assets/PuttSeed/Icon/artwork.png, writes
docs/store/feature-graphic.png (1024x500) or docs/media/social-preview.png
(1280x640).

The icon became a rendered diorama; the feature graphic was still the flat
vector scene StoreArt draws, and side by side on a store page the two read
as two different games. This puts the SAME island on the store's banner:
felt background in the game's own stripes (the palette StoreArt uses), the
island keyed out of its artwork with the icon script's flood mask, its flag
pulled onto the game's red the same way, placed right of centre with a
soft drop shadow so it sits ON the green rather than floating over it.

The store graphic carries NO text: the store prints the title beside this
image, and text baked into a picture cannot be localized. The social
preview is the opposite case — GitHub prints it into a link card with
nothing beside it, and a nameless green rectangle in a timeline is not a
link anyone clicks. So that variant, and only that variant, is set with the
game's own typeface: the wordmark and one line of what it is.
"""

import importlib.util
import os
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont

FELT = (0x38, 0x85, 0x4F)
FELT_LIGHT = (0x3E, 0x8E, 0x55)
ROUGH = (0x2F, 0x6F, 0x42)
CREAM = (0xF7, 0xF5, 0xE6)
ARTWORK = "Assets/PuttSeed/Icon/artwork.png"
FONT = "Assets/PuttSeed/UI/Fonts/Library/Outfit-SemiBold.ttf"

# Every placement below is a fraction of the canvas, so the two variants are
# the same picture at two sizes; only the island's x centre moves, to open
# the left half for the wordmark.
STORE = dict(size=(1024, 500), output="docs/store/feature-graphic.png",
             island_x=0.64, island_h=0.86, wordmark=False)
SOCIAL = dict(size=(1280, 640), output="docs/media/social-preview.png",
              island_x=0.735, island_h=0.82, wordmark=True)


def load_icon_tools():
    """The icon script's mask and redden, without renaming the file."""
    spec = importlib.util.spec_from_file_location(
        "icon_from_artwork", os.path.join("tools", "icon-from-artwork.py"))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def felt_background(width, height):
    image = Image.new("RGBA", (width, height), FELT + (255,))
    draw = ImageDraw.Draw(image)
    stripe = int(height * 0.16)
    for y in range(0, height, stripe * 2):
        draw.rectangle((0, y, width, y + stripe), fill=FELT_LIGHT + (255,))

    # The rough washes: enormous, faint, edges well outside the frame so no
    # arc ever shows — the lesson the first feature graphic taught.
    wash = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    wash_draw = ImageDraw.Draw(wash)
    r1 = int(height * 1.45)
    wash_draw.ellipse((-int(width * 0.05) - r1, int(height * 1.25) - r1,
                       -int(width * 0.05) + r1, int(height * 1.25) + r1), fill=ROUGH + (56,))
    r2 = int(height * 1.35)
    wash_draw.ellipse((int(width * 1.02) - r2, -int(height * 0.3) - r2,
                       int(width * 1.02) + r2, -int(height * 0.3) + r2), fill=ROUGH + (46,))

    # Blurred hard, so the washes are gradients with no rim at all — a disc
    # edge crossing the frame read as a crease in the first render.
    wash = wash.filter(ImageFilter.GaussianBlur(int(90 * height / 500)))
    return Image.alpha_composite(image, wash)


def tracked_text(draw, xy, text, font, fill, tracking):
    """Pillow has no letter-spacing, so the wordmark is drawn glyph by glyph."""
    x, y = xy
    for char in text:
        draw.text((x, y), char, font=font, fill=fill)
        x += draw.textlength(char, font=font) + tracking
    return x - tracking


def tracked_width(draw, text, font, tracking):
    total = sum(draw.textlength(c, font=font) for c in text)
    return total + tracking * (len(text) - 1)


def draw_wordmark(banner, width, height):
    """The name and one line of what it is, in the game's own face."""
    draw = ImageDraw.Draw(banner)
    title_font = ImageFont.truetype(FONT, int(height * 0.145))
    line_font = ImageFont.truetype(FONT, int(height * 0.051))
    tracking = height * 0.020

    left = int(width * 0.072)
    title_y = int(height * 0.375)
    tracked_text(draw, (left, title_y), "PUTTSEED", title_font, CREAM + (255,), tracking)

    # A rule the width of the wordmark, then the sentence under it — the same
    # order the menu uses, pennant over title over line.
    rule_y = title_y + int(height * 0.185)
    rule_w = int(tracked_width(draw, "PUTTSEED", title_font, tracking))
    draw.rectangle((left, rule_y, left + rule_w, rule_y + max(2, int(height * 0.005))),
                   fill=CREAM + (70,))

    draw.text((left, rule_y + int(height * 0.045)),
              "One mini-golf hole per day.", font=line_font, fill=CREAM + (215,))
    draw.text((left, rule_y + int(height * 0.115)),
              "Everyone plays the same course.", font=line_font, fill=CREAM + (140,))


def compose(variant):
    width, height = variant["size"]
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

    target_height = int(height * variant["island_h"])
    factor = target_height / island.size[1]
    island = island.resize((int(island.size[0] * factor), target_height), Image.LANCZOS)

    banner = felt_background(width, height)

    left = int(width * variant["island_x"] - island.size[0] / 2)
    top = int(height * 0.5 - island.size[1] / 2)

    # A soft shadow under the island, offset down-right like every shadow in
    # the game, so the diorama sits on the felt instead of hovering.
    shadow = Image.new("RGBA", banner.size, (0, 0, 0, 0))
    alpha = island.split()[3]
    shadow_layer = Image.new("RGBA", island.size, (0, 0, 0, 110))
    shadow_layer.putalpha(alpha.point(lambda a: a * 110 // 255))
    shadow.paste(shadow_layer, (left + int(14 * height / 500), top + int(18 * height / 500)))
    shadow = shadow.filter(ImageFilter.GaussianBlur(int(14 * height / 500)))
    banner = Image.alpha_composite(banner, shadow)

    banner.paste(island, (left, top), island)

    if variant["wordmark"]:
        draw_wordmark(banner, width, height)

    output = variant["output"]
    os.makedirs(os.path.dirname(output), exist_ok=True)
    banner.convert("RGB").save(output)
    print(f"written: {output} ({width}x{height})")


def main():
    compose(SOCIAL if "--social" in sys.argv else STORE)


if __name__ == "__main__":
    main()
