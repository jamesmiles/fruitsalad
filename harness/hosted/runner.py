#!/usr/bin/env python3
"""Docker-hosted execution harness for Fruit Salad programs.

Designed to run inside the fruitsalad-harness Docker container.
Monitors /proc for memory usage and enforces resource limits.
"""

import json
import os
import subprocess
import sys
import threading
import time


def get_peak_memory(pid: int) -> int:
    """Read peak memory usage from /proc/<pid>/status (Linux only).

    Returns peak RSS in bytes, or 0 if unavailable.
    """
    try:
        with open(f"/proc/{pid}/status", "r") as f:
            for line in f:
                if line.startswith("VmHWM:"):
                    # VmHWM is in kB
                    return int(line.split()[1]) * 1024
    except (FileNotFoundError, PermissionError, ProcessLookupError):
        pass
    return 0


def monitor_memory(pid: int, limit_bytes: int, result: dict, stop_event: threading.Event):
    """Monitor a process's memory usage, recording peak and killing if over limit."""
    peak = 0
    while not stop_event.is_set():
        mem = get_peak_memory(pid)
        if mem > peak:
            peak = mem
        if limit_bytes > 0 and mem > limit_bytes:
            try:
                os.kill(pid, 9)
            except ProcessLookupError:
                pass
            result["oom_killed"] = True
            break
        stop_event.wait(0.1)
    result["peak_memory_bytes"] = peak


def run(file_path: str, timeout: float = 30.0, memory_limit_mb: int = 256, stdin_data: str | None = None) -> dict:
    """Execute a .fs file inside the container and return results.

    Args:
        file_path: Path to the .fs source file.
        timeout: Maximum execution time in seconds.
        memory_limit_mb: Memory limit in megabytes.
        stdin_data: Optional string to feed to stdin.

    Returns:
        dict with keys: exit_code, stdout, stderr, duration_ms, timeout,
                        peak_memory_bytes, oom_killed
    """
    memory_bytes = memory_limit_mb * 1024 * 1024
    cmd = [sys.executable, "-m", "fs", file_path]

    timed_out = False
    monitor_result: dict = {"peak_memory_bytes": 0, "oom_killed": False}
    stop_event = threading.Event()
    start = time.monotonic()

    try:
        proc = subprocess.Popen(
            cmd,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            stdin=subprocess.PIPE if stdin_data else subprocess.DEVNULL,
            text=True,
        )

        # Start memory monitor thread
        monitor_thread = threading.Thread(
            target=monitor_memory,
            args=(proc.pid, memory_bytes, monitor_result, stop_event),
            daemon=True,
        )
        monitor_thread.start()

        try:
            stdout, stderr = proc.communicate(input=stdin_data, timeout=timeout)
            exit_code = proc.returncode
        except subprocess.TimeoutExpired:
            proc.kill()
            stdout, stderr = proc.communicate()
            timed_out = True
            exit_code = -1
    finally:
        stop_event.set()

    elapsed = time.monotonic() - start
    duration_ms = int(elapsed * 1000)

    return {
        "exit_code": exit_code,
        "stdout": stdout or "",
        "stderr": stderr or "",
        "duration_ms": duration_ms,
        "timeout": timed_out,
        "peak_memory_bytes": monitor_result["peak_memory_bytes"],
        "oom_killed": monitor_result["oom_killed"],
    }


def main():
    """Entry point when run inside Docker container.

    Expects a .fs file path as the first argument.
    Configuration via environment variables:
        FS_TIMEOUT          - timeout in seconds (default: 30)
        FS_MEMORY_LIMIT_MB  - memory limit in MB (default: 256)
    """
    if len(sys.argv) < 2:
        print(json.dumps({"error": "Usage: runner.py <file.fs>"}), file=sys.stderr)
        sys.exit(1)

    file_path = sys.argv[1]
    timeout = float(os.environ.get("FS_TIMEOUT", "30"))
    memory_limit = int(os.environ.get("FS_MEMORY_LIMIT_MB", "256"))

    result = run(file_path, timeout=timeout, memory_limit_mb=memory_limit)
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
