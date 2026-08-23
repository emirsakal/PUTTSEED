"""Builds the README's hero animation from a recorded frame sequence.

    python tools/make-gif.py --list
    python tools/make-gif.py [--take take-03] [--start N] [--end N] [--width 360]

Reads artifacts/hero/take-NN/frame-*.png (written by PuttSeed → Record Hero
Frames, one folder per take, newest used by default) and writes
docs/media/hero.gif. Needs Pillow; deliberately does NOT need ffmpeg, because
the frames arrive as lossless PNGs and never pass through a video codec at
all.

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
HERO = os.path.join(ROOT, "artifacts", "hero")
OUTPUT = os.path.join(ROOT, "docs", "media", "hero.gif")


def takes():
    """Every recorded take, newest last, as (name, directory, frame count)."""
    found = []
    if os.path.isdir(HERO):
        for name in sorted(os.listdir(HERO)):
            folder = os.path.join(HERO, name)
            if os.path.isdir(folder):
                count = len(glob.glob(os.path.join(folder, "frame-*.png")))
                if count:
                    found.append((name, folder, count))
    # Frames sitting loose in artifacts/hero, from before takes were foldered.
    loose = len(glob.glob(os.path.join(HERO, "frame-*.png")))
    if loose:
        found.insert(0, ("(loose)", HERO, loose))
    return found


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
    parser.add_argument("--colors", type=int, default=256, help="palette size, up to 256")
    parser.add_argument("--dither", action="store_true",
                        help="dither the quantisation (measured worse here; see the note)")
    parser.add_argument("--hold", type=int, default=0,
                        help="ms to hold the LAST frame before looping (0 = no hold)")
    parser.add_argument("--out", default=OUTPUT)
    parser.add_argument("--take", help="take folder name, e.g. take-03 (default: the newest)")
    parser.add_argument("--list", action="store_true", help="list recorded takes and exit")
    args = parser.parse_args()

    available = takes()
    if args.list:
        if not available:
            print("no takes in %s" % HERO)
        for name, folder, count in available:
            print("%-12s %4d frames  %s" % (name, count, folder))
        return 0

    if not available:
        raise SystemExit("make-gif: no frames under %s - record some with F10 in Play mode."
                         % HERO)

    if args.take:
        match = [t for t in available if t[0] == args.take]
        if not match:
            raise SystemExit("make-gif: no take named %r. Known: %s"
                             % (args.take, ", ".join(t[0] for t in available)))
        chosen_take = match[0]
    else:
        chosen_take = available[-1]

    print("take: %s (%d frames)" % (chosen_take[0], chosen_take[2]))
    paths = sorted(glob.glob(os.path.join(chosen_take[1], "frame-*.png")))

    chosen = paths[args.start:args.end][::args.every]
    if len(chosen) < 2:
        raise SystemExit("make-gif: %d frame(s) selected; need at least 2." % len(chosen))

    print("frames: %d of %d (start=%d end=%s every=%d)"
          % (len(chosen), len(paths), args.start, args.end, args.every))

    frames = list(load(chosen, args.width))
    palette = build_palette(frames, args.colors)

    # Dithering OFF by default, which is the opposite of the usual advice and
    # was measured rather than assumed. This game is drawn in flat colour, so
    # a 256-entry palette has an exact entry for nearly every pixel and there
    # is no gradient for dithering to smooth -- all it adds is noise, and noise
    # is precisely what LZW cannot compress. On the hero take, at 360 px:
    #
    #     dithered, 128 colours   1.62 MB   RMSE 4.89
    #     dithered, 256 colours   1.96 MB
    #     flat,     128 colours   0.38 MB   RMSE 3.97
    #     flat,     256 colours   0.45 MB   RMSE 2.56   <- default
    #
    # More colours AND no dithering is both the most faithful and a quarter of
    # the size. Photographic frames would want the flag; this art does not.
    dither = Image.FLOYDSTEINBERG if args.dither else Image.NONE
    mapped = [f.quantize(palette=palette, dither=dither) for f in frames]

    # GIF stores delays in hundredths of a second, so the achievable rate is
    # quantised too; report what it will ACTUALLY play at rather than what was
    # asked for.
    effective_fps = args.fps / args.every
    delay_ms = max(20, int(round(1000.0 / effective_fps / 10.0)) * 10)
    print("playback: %.1f fps requested, %.1f fps actual (%d ms per frame)"
          % (effective_fps, 1000.0 / delay_ms, delay_ms))

    # A held last frame instead of a tail of identical ones. The celebration
    # settles and then just sits there, and paying 30 frames a second to
    # re-send a picture that is not changing costs size and reads as dead air.
    # One frame with a long delay gives the loop its beat for nothing.
    durations = [delay_ms] * len(mapped)
    if args.hold:
        durations[-1] = args.hold
        print("hold: last frame for %d ms" % args.hold)

    os.makedirs(os.path.dirname(args.out), exist_ok=True)
    mapped[0].save(args.out, save_all=True, append_images=mapped[1:],
                   duration=durations, loop=0, optimize=True, disposal=1)

    size = os.path.getsize(args.out)
    print("wrote %s — %dx%d, %d frames, %.1f MB"
          % (args.out, frames[0].width, frames[0].height, len(mapped), size / 1048576.0))
    if size > 10 * 1024 * 1024:
        print("WARNING: over 10 MB. GitHub will serve it, but a reader on a phone "
              "pays for every byte — try --every 2, a smaller --width, or fewer --colors.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
