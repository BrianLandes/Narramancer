#!/usr/bin/env python3
"""Serve a Unity WebGL build for the save/load smoke test.

    python3 tools/serve-build.py              # newest build under Builds/
    python3 tools/serve-build.py <path>       # a specific build folder
    python3 tools/serve-build.py --port 8080

Why not just `python3 -m http.server`:

  * It finds the build for you. Unity drops builds in whichever folder was last
    picked, sometimes nested inside a previous one, and serving yesterday's build
    by mistake gives a confident pass on code that isn't in it.

  * It sends `Cache-Control: no-store`. Unity reuses the same file names between
    builds, so a browser will happily serve the *previous* `.data` from cache and
    you will test the old content without any sign that anything is wrong. This
    removes the need to remember a hard reload.

  * It pins the origin. Narramancer's WebGL saves live in IndexedDB, which is
    scoped per origin — save on `localhost:8000` and reopen on `127.0.0.1:8000`
    and the save is simply gone, which looks exactly like a save/load bug.

Stop it with Ctrl+C.
"""

import argparse
import functools
import http.server
import mimetypes
import os
import socket
import sys
import time
from pathlib import Path

DEFAULT_PORT = 8000
REPO_ROOT = Path(__file__).resolve().parent.parent


def find_builds(root: Path):
    """Every folder under `root` that looks like a Unity WebGL build, newest first."""
    builds = []
    for index in root.rglob("index.html"):
        if (index.parent / "Build").is_dir():
            builds.append(index.parent)
    return sorted(builds, key=lambda p: (p / "index.html").stat().st_mtime, reverse=True)


def describe(build: Path) -> str:
    """A one-line summary, so it is obvious whether this is the build you just made."""
    when = time.strftime("%Y-%m-%d %H:%M", time.localtime((build / "index.html").stat().st_mtime))
    payload = sum(f.stat().st_size for f in (build / "Build").iterdir() if f.is_file())
    return f"{when}   {payload / 1_000_000:6.1f} MB   {build.relative_to(REPO_ROOT)}"


class Handler(http.server.SimpleHTTPRequestHandler):
    def end_headers(self):
        # The whole point: Unity reuses file names, so a cached .data silently
        # serves the previous build's content.
        self.send_header("Cache-Control", "no-store, must-revalidate")
        super().end_headers()

    def log_message(self, fmt, *args):
        # One line per request is noise; failures are not.
        status = str(args[1]) if len(args) > 1 else ""
        if not status.startswith("2"):
            super().log_message(fmt, *args)


def main() -> int:
    parser = argparse.ArgumentParser(description="Serve a Unity WebGL build.")
    parser.add_argument("build", nargs="?", help="build folder (default: newest under Builds/)")
    parser.add_argument("--port", type=int, default=DEFAULT_PORT)
    parser.add_argument("--list", action="store_true", help="list the builds found and exit")
    args = parser.parse_args()

    builds = find_builds(REPO_ROOT / "Builds")

    if args.list:
        for build in builds:
            print(describe(build))
        return 0

    if args.build:
        chosen = Path(args.build).resolve()
        if not (chosen / "index.html").is_file():
            print(f"error: {chosen} has no index.html — is that a build folder?", file=sys.stderr)
            return 1
    elif builds:
        chosen = builds[0]
    else:
        print("error: no WebGL build found under Builds/", file=sys.stderr)
        return 1

    # .wasm must be application/wasm or the browser falls back to non-streaming
    # compilation and warns. Python 3.11+ knows it; older ones do not.
    mimetypes.add_type("application/wasm", ".wasm")

    print(f"serving  {describe(chosen)}")
    if len(builds) > 1 and chosen == builds[0]:
        print(f"         ({len(builds) - 1} older build(s) ignored — use --list to see them)")
    print()
    print(f"    http://localhost:{args.port}")
    print()
    print("  Use that exact URL both times: WebGL saves live in IndexedDB, which is")
    print("  scoped per origin, so localhost and 127.0.0.1 do not share saves.")
    print("  To test a reload, close the tab — do not refresh.")
    print()

    handler = functools.partial(Handler, directory=str(chosen))
    try:
        with http.server.ThreadingHTTPServer(("127.0.0.1", args.port), handler) as server:
            server.serve_forever()
    except OSError as error:
        if error.errno in (socket.EADDRINUSE, 98):
            print(f"error: port {args.port} is already in use — stop the other server, "
                  f"or pass --port", file=sys.stderr)
            return 1
        raise
    except KeyboardInterrupt:
        print("\nstopped")
    return 0


if __name__ == "__main__":
    sys.exit(main())
