# Developing PUTTSEED

Everything that only matters once the repo is checked out: building, signing,
capturing the images, and putting the demo on the web.

The [README](../README.md) is about the game and how it works, and it stays
that way — a reader deciding whether this project is interesting does not
need to know how a keystore is generated. This file is where that went.

## Requirements

- **.NET SDK 8** — enough on its own for the core test suite, which needs no
  engine at all.
- **Unity 6000.3.x** with the **Android** module for the game, plus the
  **WebGL** module for the browser demo.
- **Python 3** with Pillow, for the image tools (`tools/*.py`). No ffmpeg
  anywhere.

Open the **repo root** in Unity Hub. There is no `unity/` subfolder: the
Unity project is the repository, and `core/` sits beside `Assets/` rather
than inside it.

## Everything you can run

| What | How |
|---|---|
| Core test suite | `dotnet test core` |
| Purity check + core tests, as CI runs them | `scripts\test.bat` |
| Purity check alone | `python tools/check-purity.py` (`--self-test` to test the check) |
| Unity EditMode tests | `scripts\unity-tests.bat` |
| ASCII course viewer | `dotnet run --project tools/CourseViewer -c Release -- 3 --stats` |
| Generator scan / benchmark | `... -- --scan 200 --v4 --feel`, `... -- --bench 40 --v4 --feel` |
| Rebuild the baked scenes | **PuttSeed → Rebuild Scenes** (batch: `-executeMethod PuttSeed.Unity.Editor.BuildTools.CreateScenes`) |
| Debug Android build | `scripts\build-android.bat` (`apk` for an installable APK) |
| Release .aab (signed) | `scripts\build-release.bat` |
| WebGL demo | `scripts\build-webgl.bat`, then `scripts\deploy-webgl.bat push` |
| Screenshot | Play mode, then **F9** |
| Hero animation | Play mode, then **F10**, then `python tools/make-gif.py` |
| Store banner art | `python tools/feature-from-artwork.py [--social]` |

**Unity batch commands need the editor CLOSED.** With it open they exit with
`return code 1` and no compile error in the log, which looks like a build
failure and is a lock. The editor also takes a minute or two to actually exit
after a batch test run finishes — the next command will refuse until it does.

**A green test run is not a green build.** The test assembly does not
reference the Editor one, so a broken editor script runs no tests and reports
success. `scripts\unity-tests.bat` fails on `error CS` in the log for exactly
this reason; if you invoke Unity directly, check the log yourself.

## The UI is code

The whole interface is constructed in `UiConstruction` + `UIFactory` and
**baked into the scenes**, where it stays editable in the Inspector. It is
not rebuilt on Play. So a UI change means running **PuttSeed → Rebuild
Scenes**, and the scene diff that follows will be enormous — Unity reassigns
fileIDs on a rebake, so thousands of changed lines can carry a handful of
real ones. Search the diff for the property you changed before assuming it
did not take.

Any MonoBehaviour that serialises into a scene must live in a file named
after it, or the scene loads with a missing script.

## Typography

The game is set in **Outfit SemiBold**. Six OFL candidates live in
`Assets/PuttSeed/UI/Fonts/Library/`; `Fonts/active.txt` names the one in use.
Change that line and run **PuttSeed → Rebuild Scenes** to re-set the whole
UI.

`UiFontTests` asserts the active face can print every character the UI can
show, in both languages — two of the six candidates failed that audition, one
missing a right arrow and one missing Turkish ğ/İ/ş.

## Signing (release builds)

Create `keystore.properties` in the repo root — it is gitignored and must
never be committed:

```
storeFile=puttseed.keystore
storePassword=<store password>
keyAlias=puttseed
keyPassword=<key password>
```

Generate a keystore once with:

```bash
keytool -genkeypair -v -keystore puttseed.keystore -alias puttseed -keyalg RSA -keysize 2048 -validity 10000
```

`scripts\build-release.bat` picks it up automatically and warns loudly if it
builds unsigned.

## The WebGL demo

`scripts\build-webgl.bat` writes `artifacts/webgl`, then
`scripts\deploy-webgl.bat push` publishes it to
<https://emirsakal.github.io/PUTTSEED/>.

Three things about it are load-bearing:

- **Gzip with `decompressionFallback`.** GitHub Pages serves static files and
  cannot add a `Content-Encoding` header, so a normally-compressed build would
  arrive as bytes the browser never inflates. Unity's own JS decompressor
  keeps the download small and lets the host stay dumb.
- **`gh-pages` is an orphan branch**, published through a throwaway worktree.
  An 11 MB WebAssembly build has no business in the tree people browse or the
  history they read. The deploy script refuses its own delete step unless
  `pushd` succeeded *and* HEAD really is `gh-pages`.
- **The canvas renders at the handset's pixel count.** The page keeps the
  phone's 1170×2532 aspect, so on a laptop the window's *height* decides its
  width — about 400 CSS pixels. The committed WebGL template answers
  `devicePixelRatio` with whatever reaches 1170, capped at 3×, so the browser
  build is as sharp as the phone rather than a third of it.

## Screenshots and the hero animation

Both are captured from the running game, and both are on function keys
because the frames worth having exist only while a drag is **held** — and
reaching a menu means letting go of it.

**F9** (`PuttSeed → Capture Screenshot`) writes
`docs/media/shot-<timestamp>.png`. Those are working files and gitignored; a
shot earns a stable name when it is curated into the README.

**F10** (`PuttSeed → Record Hero Frames`) records a take into
`artifacts/hero/take-NN/` as a lossless PNG per frame. Nothing is ever deleted, so record as many as you like and pick
afterwards. It uses `Time.captureFramerate`, which pins the frame delta so
the recording is evenly spaced however long each encode took — the game runs
slow while recording and the result plays back at normal speed.

Then:

```bash
python tools/make-gif.py --list
python tools/make-gif.py --take take-04 --start 0 --end 66 --hold 900
```

A good take is one putt: aim held, released, in the cup, stars. Use
**Practice**, not the daily — a daily replays your previous best as a ghost,
which puts a second ball on screen. Do not press Retry mid-recording; the
stroke counter resets and the animation looks like it jumped.

## The tools

| File | What it does |
|---|---|
| `tools/CourseViewer` | prints any course as ASCII; scans and benchmarks the generator |
| `tools/check-purity.py` | the float-free rule, run by CI and by `scripts\check-purity.bat` |
| `tools/make-gif.py` | assembles a recorded take into `docs/media/hero.gif` |
| `tools/webshot.py` | serves the WebGL build with a capture hook, for shots without the editor |
| `tools/icon-from-artwork.py` | the app icon from `Assets/PuttSeed/Icon/artwork.png` |
| `tools/feature-from-artwork.py` | the Play feature graphic, and `--social` for the GitHub card |
