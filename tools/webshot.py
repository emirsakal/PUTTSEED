"""Serves the WebGL build with a capture harness, for README screenshots.

    python tools/webshot.py [--port 8123] [--dpr 3.2]

Then drive the game in a browser and, from the console, run:

    puttseedShot('menu')      ->  docs/media/shot-menu.png

Why this exists: the README's screenshots have to be sharp, and the game is
a 1170x2532 portrait phone game. A browser window that small is not a window
anyone has, and a browser screenshot is only ever as big as the surface it
paints. So the picture does not come from the window at all — it comes from
the canvas.

Two things make that possible, and BOTH live here rather than in the
shipped page. The product should not carry a debug mode it never uses:

  * devicePixelRatio is overridden before Unity boots, so Unity sizes its
    drawing buffer to the handset's real pixel count while the canvas keeps
    its small CSS box. The game lays out in CSS pixels either way, so this
    changes the resolution and nothing else.
  * preserveDrawingBuffer is forced on, without which reading the canvas
    after a frame returns transparent black. It costs performance, which is
    exactly why it is not on in the build people play.

The server rewrites index.html on the way out and leaves the build on disk
untouched, so what it serves is the shipping page plus a capture hook.
"""

import argparse
import base64
import functools
import http.server
import os
import re
import socketserver
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BUILD = os.path.join(ROOT, "artifacts", "webgl")
SHOTS = os.path.join(ROOT, "docs", "media")

# The capture hook. It runs in <head>, before the page's own script appends
# the Unity loader, which is the only moment devicePixelRatio can still be
# changed and be believed.
HEAD_HOOK = """
<script>
// --- injected by tools/webshot.py; not part of the build ---
(function () {
  var DPR = %(dpr)s;
  Object.defineProperty(window, "devicePixelRatio", { get: function () { return DPR; } });

  window.puttseedShot = function (name) {
    var canvas = document.querySelector("#unity-canvas");
    var data = canvas.toDataURL("image/png");
    return fetch("/__shot/" + encodeURIComponent(name), {
      method: "POST",
      headers: { "Content-Type": "text/plain" },
      body: data,
    }).then(function (r) { return r.text(); });
  };

  window.puttseedSize = function () {
    var c = document.querySelector("#unity-canvas");
    return [c.width, c.height];
  };

  // A capture bar, so taking the shot needs neither a console nor a hand off
  // the game. Everything here sits OUTSIDE the canvas: Unity swallows what
  // lands on it, and a control the game eats is not a control.
  window.addEventListener("DOMContentLoaded", function () {
    var bar = document.createElement("div");
    // Bottom-left, where the page's own footer lives: the frame is centred
    // and never reaches down here, so the bar cannot cover a control the
    // player needs. Top-left looked tidier and ate the back button.
    bar.style.cssText = "position:fixed;left:10px;bottom:8px;z-index:99;display:flex;gap:6px;" +
      "align-items:center;font:12px system-ui;color:#f7f5e6;background:rgba(16,22,29,.94);" +
      "border:1px solid rgba(247,245,230,.18);border-radius:10px;padding:6px 8px";

    var name = document.createElement("input");
    name.value = "menu";
    name.title = "file name: docs/media/shot-<name>.png";
    name.style.cssText = "width:104px;background:#0a0d12;color:inherit;border:1px solid " +
      "rgba(247,245,230,.2);border-radius:6px;padding:3px 6px;font:inherit";

    var shoot = document.createElement("button");
    shoot.textContent = "Capture (F9)";
    shoot.style.cssText = "background:#f2c14e;color:#0a0d12;border:0;border-radius:6px;" +
      "padding:4px 9px;font:inherit;font-weight:600;cursor:pointer";

    var status = document.createElement("span");
    status.style.cssText = "opacity:.65;min-width:96px";

    // The name is a filename, and a filename that silently becomes something
    // else is how a capture ends up overwriting the previous one.
    function slug() {
      var s = name.value.trim().replace(/[^A-Za-z0-9._-]/g, "-");
      return s || "shot";
    }

    function take() {
      var target = slug();
      status.textContent = "capturing...";
      window.puttseedShot(target).then(function () {
        var size = window.puttseedSize();
        status.textContent = target + " " + size[0] + "x" + size[1];
      }).catch(function (e) { status.textContent = "failed: " + e; });
    }

    shoot.addEventListener("click", take);

    // Capture phase on window, because a focused canvas would otherwise eat
    // the key before it ever reaches this listener.
    window.addEventListener("keydown", function (e) {
      if (e.key === "F9") { e.preventDefault(); e.stopPropagation(); take(); }
    }, true);

    bar.appendChild(name);
    bar.appendChild(shoot);
    bar.appendChild(status);
    document.body.appendChild(bar);
  });
})();
</script>
"""


def rewrite_index(html, dpr):
    """The shipping page plus a capture hook: head script, and one config key."""
    html = html.replace("</head>", (HEAD_HOOK % {"dpr": dpr}) + "</head>", 1)

    # preserveDrawingBuffer has to reach createUnityInstance, and the config
    # object is built inline in the page. One anchored insertion, so a
    # template that stops containing it fails loudly rather than silently
    # producing black images.
    anchor = "arguments: [],"
    if anchor not in html:
        raise SystemExit(
            "webshot: could not find the config anchor in index.html - "
            "the WebGL template changed; update tools/webshot.py.")
    html = html.replace(
        anchor,
        anchor + "\n      webglContextAttributes: { preserveDrawingBuffer: true },",
        1)
    return html


class Handler(http.server.SimpleHTTPRequestHandler):
    dpr = 3.2

    def do_POST(self):
        match = re.match(r"^/__shot/(.+)$", self.path)
        if not match:
            self.send_error(404)
            return

        name = re.sub(r"[^A-Za-z0-9._-]", "-", match.group(1))
        length = int(self.headers.get("Content-Length", 0))
        payload = self.rfile.read(length).decode("ascii", "replace")
        if not payload.startswith("data:image/png;base64,"):
            self.send_error(400, "expected a PNG data URL")
            return

        raw = base64.b64decode(payload.split(",", 1)[1])
        os.makedirs(SHOTS, exist_ok=True)
        path = os.path.join(SHOTS, "shot-%s.png" % name)
        with open(path, "wb") as handle:
            handle.write(raw)

        message = "wrote %s (%d bytes)" % (path, len(raw))
        print(message, flush=True)
        body = message.encode()
        self.send_response(200)
        self.send_header("Content-Type", "text/plain")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def do_GET(self):
        # Split the query off before matching: a cache-busting "/?v=2" is
        # still the page that needs rewriting, and falling through to the
        # static handler would quietly serve the un-hooked build.
        route = self.path.split("?", 1)[0]
        if route in ("/", "/index.html"):
            with open(os.path.join(BUILD, "index.html"), encoding="utf-8") as handle:
                html = rewrite_index(handle.read(), self.dpr)
            body = html.encode("utf-8")
            self.send_response(200)
            self.send_header("Content-Type", "text/html; charset=utf-8")
            self.send_header("Content-Length", str(len(body)))
            # The page is rewritten on every request and the rewrite is the
            # whole point; a cached copy is the un-hooked build.
            self.send_header("Cache-Control", "no-store, must-revalidate")
            self.end_headers()
            self.wfile.write(body)
            return
        super().do_GET()

    def log_message(self, fmt, *args):
        pass


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--port", type=int, default=8123)
    parser.add_argument("--dpr", type=float, default=3.2,
                        help="canvas pixels per CSS pixel; 3.2 puts a ~370 px "
                             "frame at roughly the handset's 1170 px width")
    args = parser.parse_args()

    if not os.path.isfile(os.path.join(BUILD, "index.html")):
        raise SystemExit("webshot: no build at artifacts/webgl - "
                         "run scripts\\build-webgl.bat first.")

    Handler.dpr = args.dpr
    handler = functools.partial(Handler, directory=BUILD)
    socketserver.TCPServer.allow_reuse_address = True
    with socketserver.TCPServer(("127.0.0.1", args.port), handler) as server:
        print("webshot serving %s at http://127.0.0.1:%d/ (dpr %s)"
              % (BUILD, args.port, args.dpr), flush=True)
        print("in the page console: puttseedShot('menu')", flush=True)
        try:
            server.serve_forever()
        except KeyboardInterrupt:
            print("stopped", flush=True)
            sys.exit(0)


if __name__ == "__main__":
    main()
