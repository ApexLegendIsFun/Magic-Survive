#!/usr/bin/env python3
"""Run a built Seondong integration game on macOS and preserve evidence."""

from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import time


SCENARIOS = ("death", "victory", "timeout", "layout", "growth")
ELEMENTS = ("Fire", "Lightning", "Frost", "Earth", "Dark")
DEFAULT_WIDTH = 1280
DEFAULT_HEIGHT = 720
DEFAULT_TIMEOUT = 1100


def project_root() -> Path:
    return Path(__file__).resolve().parents[4]


def executable_path(root: Path) -> Path:
    return root / "Builds" / "SeondongIntegrationMac" / "Magic-Survive.app" / "Contents" / "MacOS" / "Magic-Survive"


def parser() -> argparse.ArgumentParser:
    result = argparse.ArgumentParser(description="Run a Seondong macOS integration build.")
    result.add_argument("--scenario", choices=SCENARIOS, default="death")
    result.add_argument("--element", choices=ELEMENTS, default="Fire")
    result.add_argument("--width", type=int, default=DEFAULT_WIDTH)
    result.add_argument("--height", type=int, default=DEFAULT_HEIGHT)
    result.add_argument("--output", type=Path, help="Evidence directory; must not already exist.")
    result.add_argument("--timeout", type=float, default=DEFAULT_TIMEOUT, help="Process timeout in seconds (default: 1100).")
    return result


def make_output(root: Path, requested: Path | None, scenario: str, width: int, height: int) -> Path:
    if requested is not None:
        output = requested if requested.is_absolute() else root / requested
        if output.exists():
            raise RuntimeError(f"Output directory already exists; refusing overwrite: {output}")
        output.parent.mkdir(parents=True, exist_ok=True)
        output.mkdir()
        return output

    base = root / "Logs" / "SeondongIntegration"
    base.mkdir(parents=True, exist_ok=True)
    stamp = dt.datetime.now().astimezone().strftime("%Y%m%d-%H%M%S-%f")
    output = base / f"{scenario}-{width}x{height}-{stamp}"
    output.mkdir()
    return output


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def write_json(path: Path, value: dict) -> None:
    path.write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")


def read_report(path: Path) -> dict | None:
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except (FileNotFoundError, json.JSONDecodeError, OSError):
        return None
    return value if isinstance(value, dict) else None


def run(args: argparse.Namespace) -> int:
    root = project_root()
    executable = executable_path(root)
    if not executable.is_file():
        raise RuntimeError(f"Build macOS first: {executable}")
    if args.width <= 0 or args.height <= 0:
        raise RuntimeError("Width and height must be positive.")
    if args.timeout <= 0:
        raise RuntimeError("Timeout must be positive.")

    output = make_output(root, args.output, args.scenario, args.width, args.height)
    log_file = output / "Player.log"
    command = [
        str(executable),
        "-screen-fullscreen", "0",
        "-screen-width", str(args.width),
        "-screen-height", str(args.height),
        "-integration-auto", args.scenario,
        "-integration-element", args.element,
        "-integration-output", str(output),
        "-logFile", str(log_file),
    ]

    executable_hash = sha256(executable)
    started = time.monotonic()
    process = subprocess.Popen(command, cwd=root)
    print(f"Started {args.scenario} at {args.width}x{args.height}, process {process.pid}. Evidence: {output}")
    timed_out = False
    try:
        exit_code = process.wait(timeout=args.timeout)
    except subprocess.TimeoutExpired:
        timed_out = True
        process.terminate()
        try:
            exit_code = process.wait(timeout=10)
        except subprocess.TimeoutExpired:
            process.kill()
            exit_code = process.wait()

    report = read_report(output / "report.json")
    report_status = report.get("status") if report else None
    passed = not timed_out and report_status == "PASS" and exit_code == 0
    process_data = {
        "status": "PASS" if passed else "FAIL",
        "exitCode": exit_code,
        "reportStatus": report_status,
        "processExited": True,
        "timedOut": timed_out,
        "seconds": round(time.monotonic() - started, 3),
        "executableSHA256": executable_hash,
    }
    if timed_out:
        process_data["reason"] = "Process deadline exceeded"
    write_json(output / "process.json", process_data)
    print(json.dumps(process_data, separators=(",", ":")))
    return 0 if passed else 1


def main() -> int:
    args = parser().parse_args()
    try:
        return run(args)
    except (OSError, RuntimeError) as error:
        print(f"Run-BuildCheck.py: {error}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
