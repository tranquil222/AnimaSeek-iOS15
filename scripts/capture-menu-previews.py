#!/usr/bin/env python3
"""Capture mock-only menu previews at an iPhone 7-sized logical viewport on macOS."""
import json
import os
from pathlib import Path
import subprocess
import sys
import time


def simctl(*args, **kwargs):
    return subprocess.check_output(["xcrun", "simctl", *args], text=True, **kwargs).strip()


def main():
    app = next(Path(sys.argv[1]).glob("*.app"))
    output = Path("dist/previews")
    output.mkdir(parents=True, exist_ok=True)
    runtimes = json.loads(simctl("list", "runtimes", "--json"))["runtimes"]
    runtime = next(r["identifier"] for r in runtimes if r.get("isAvailable") and r["name"].startswith("iOS 26"))
    device = simctl("create", "AnimaSeek 15 menu preview", "com.apple.CoreSimulator.SimDeviceType.iPhone-SE-3rd-generation", runtime)
    try:
        simctl("boot", device)
        simctl("bootstatus", device, "-b", timeout=180)
        simctl("install", device, str(app))
        cases = [("file-dark", "file", "dark", "large"),
                 ("folder-light", "folder", "light", "large"),
                 ("file-large-text", "file", "dark", "accessibility-extra-extra-extra-large"),
                 ("search-bars", "", "dark", "large")]
        for name, menu, appearance, size in cases:
            subprocess.run(["xcrun", "simctl", "terminate", device, "com.animaseek.app"], capture_output=True)
            simctl("ui", device, "appearance", appearance)
            simctl("ui", device, "content_size", size)
            env = os.environ.copy()
            env["SIMCTL_CHILD_ANIMASEEK_UI_MENU"] = menu
            env["SIMCTL_CHILD_ANIMASEEK_UI_ROUTE"] = "home" if menu else "search"
            env["SIMCTL_CHILD_ANIMASEEK_UI_QUERY"] = "" if menu else "jazz"
            simctl("launch", device, "com.animaseek.app", env=env)
            time.sleep(5)
            simctl("io", device, "screenshot", str(output / f"{name}.png"))
    finally:
        subprocess.run(["xcrun", "simctl", "shutdown", device], capture_output=True)
        subprocess.run(["xcrun", "simctl", "delete", device], capture_output=True)


if __name__ == "__main__":
    main()
