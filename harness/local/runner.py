#!/usr/bin/env python3
"""Local execution harness for Fruit Salad programs.

Runs `python -m fs <file>` as a subprocess, capturing stdout, stderr,
exit code, and duration. Outputs a JSON result object.
"""

import argparse
import json
import resource
import subprocess
import sys
import time


def run(
    file_path: str,
    timeout: float = 30.0,
    memory_limit_mb: int = 256,
    stdin_data: str | None = None,
) -> dict:
    """Execute a .fs file via the interpreter and return results.

    Args:
        file_path: Path to the .fs source file.
        timeout: Maximum execution time in seconds.
        memory_limit_mb: Memory limit in megabytes (best-effort via ulimit).
        stdin_data: Optional string to feed to the program's stdin.

    Returns:
        dict with keys: exit_code, stdout, stderr, duration_ms, timeout
    """
    memory_bytes = memory_limit_mb * 1024 * 1024

    def set_limits():
        """Set resource limits for the child process."""
        try:
            resource.setrlimit(resource.RLIMIT_AS, (memory_bytes, memory_bytes))
        except (ValueError, resource.error):
            # RLIMIT_AS may not be available on all platforms (e.g. macOS)
            pass

    cmd = [sys.executable, "-m", "fs", file_path]
    timed_out = False
    start = time.monotonic()

    try:
        proc = subprocess.run(
            cmd,
            capture_output=True,
            text=True,
            timeout=timeout,
            input=stdin_data,
            preexec_fn=set_limits,
        )
        exit_code = proc.returncode
        stdout = proc.stdout
        stderr = proc.stderr
    except subprocess.TimeoutExpired as exc:
        timed_out = True
        exit_code = -1
        stdout = exc.stdout or ""
        stderr = exc.stderr or ""
        if isinstance(stdout, bytes):
            stdout = stdout.decode("utf-8", errors="replace")
        if isinstance(stderr, bytes):
            stderr = stderr.decode("utf-8", errors="replace")

    elapsed = time.monotonic() - start
    duration_ms = int(elapsed * 1000)

    return {
        "exit_code": exit_code,
        "stdout": stdout,
        "stderr": stderr,
        "duration_ms": duration_ms,
        "timeout": timed_out,
    }


def main():
    parser = argparse.ArgumentParser(description="Run a Fruit Salad program locally.")
    parser.add_argument("file", help="Path to the .fs source file")
    parser.add_argument(
        "--timeout",
        type=float,
        default=30.0,
        help="Timeout in seconds (default: 30)",
    )
    parser.add_argument(
        "--memory-limit",
        type=int,
        default=256,
        help="Memory limit in MB (default: 256)",
    )
    args = parser.parse_args()

    result = run(args.file, timeout=args.timeout, memory_limit_mb=args.memory_limit)
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
