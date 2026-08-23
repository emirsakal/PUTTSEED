"""Builds the README's hero animation from a recorded frame sequence.

    python tools/make-gif.py [--start N] [--end N] [--width 360] [--fps 30]

Reads artifacts/hero/frame-*.png (written by PuttSeed → Record Hero Frames)
and writes docs/media/hero.gif. Needs Pillow; deliberately does NOT need
ffmpeg, because the frames arrive as lossless PNGs and never pass through a
video codec at all.

Two decisions carry the quality:

ONE PALETTE FOR THE WHOLE ANIMATION. A GIF holds 256 colours, and the obvious
approach — let each frame pick its own best 256 — makes the greens shift very
slightly from frame to frame, which the eye reads as the picture boiling. A
palette is built once from frames sampled across the sequence and every frame
is mapped onto it, so the grass stays exactly the colour it was.

TRIM BEFORE SCALING, SCALE BEFORE QUANTISING. Cutting the dead frames off the
ends is what makes the loop read as a loop, and resampling in full colour
before dropping to 256 keeps the ball's edge smooth instead of quantising the
aliasing along with everything else.
"""

import argparse
import glob
import os
import sys

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FRAMES = os.path.join(ROOT, "artifacts", "hero")
OUTPUT = os.path.join(ROOT, "docs", "media", "hero.gif")


def load(paths, width):
    for path in paths:
        image = Image.open(path).convert("RGB")
        if width and image.width != width:
            height = round(image.height * width / image.width)
            image = image.resize((width, height), Image.LANCZOS)
        yield image


def build_palette(frames, colors):
    """One palette for every frame, sampled across the whole sequence.

    Sampling matters: a palette taken from the first frames alone would have
    no entries for the colours that only appear at the end — the stars, the
    confetti — and those would arrive dithered out of the nearest green.
    """
    step = max(1, len(frames) // 12)
    sample = frames[::step][:12]
    strip = Image.new("RGB", (sample[0].width, sample[0].height * len(sample)))
    for i, frame in enumerate(sample):
        strip.paste(frame, (0, i * frame.height))
    return strip.quantize(colors=colors, method=Image.MEDIANCUT)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--start", type=int, default=0, help="first frame to keep")
    parser.add_argument("--end", type=int, default=None, help="last frame to keep (exclusive)")
    parser.add_argument("--every", type=int, default=1, help="keep 1 frame in N (halves size at 2)")
    parser.add_argument("--width", type=int, default=360, help="output width in pixels")
    parser.add_argument("--fps", type=float, default=30.0, help="frames per second of the SOURCE")
    parser.add_argument("--colors", type=int, default=128, help="palette size, up to 256")
    parser.add_argument("--out", default=OUTPUT)
    args = parser.parse_args()

    paths = sorted(glob.glob(os.path.join(FRAMES, "frame-*.png")))
    if not paths:
        raise SystemExit("make-gif: no frames in %s — record some with F10 in Play mode."
                         % FRAMES)

    chosen = paths[args.start:args.end][::args.every]
    if len(chosen) < 2:
        raise SystemExit("make-gif: %d frame(s) selected; need at least 2." % len(chosen))

    print("frames: %d of %d (start=%d end=%s every=%d)"
          % (len(chosen), len(paths), args.start, args.end, args.every))

    frames = list(load(chosen, args.width))
    palette = build_palette(frames, args.colors)
    mapped = [f.quantize(palette=palette, dither=Image.FLOYDSTEINBERG) for f in frames]

    # GIF stores delays in hundredths of a second, so the achievable rate is
    # quantised too; report what it will ACTUALLY play at rather than what was
    # asked for.
    effective_fps = args.fps / args.every
    delay_ms = max(20, int(round(1000.0 / effective_fps / 10.0)) * 10)
    print("playback: %.1f fps requested, %.1f fps actual (%d ms per frame)"
          % (effective_fps, 1000.0 / delay_ms, delay_ms))

    os.makedirs(os.path.dirname(args.out), exist_ok=True)
    mapped[0].save(args.out, save_all=True, append_images=mapped[1:],
                   duration=delay_ms, loop=0, optimize=True, disposal=1)

    size = os.path.getsize(args.out)
    print("wrote %s — %dx%d, %d frames, %.1f MB"
          % (args.out, frames[0].width, frames[0].height, len(mapped), size / 1048576.0))
    if size > 10 * 1024 * 1024:
        print("WARNING: over 10 MB. GitHub will serve it, but a reader on a phone "
              "pays for every byte — try --every 2, a smaller --width, or fewer --colors.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
