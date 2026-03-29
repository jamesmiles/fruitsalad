#!/usr/bin/env python3
"""Test runner for Fruit Salad programs.

Discovers .fs test files, parses inline annotations, executes each test
via local or Docker harness, compares results, and reports a reward signal.

Annotation format (in Fruit Salad comments):
    -- @test <id>: <description>
    -- @input <text>
    -- @expect <expected stdout>
    -- @expect_error <expected compile-time error substring>
    -- @expect_runtime_error <expected runtime error substring>

Usage:
    python tests/run_tests.py [--harness local|docker] [--timeout SECS] [--verbose]
"""

import argparse
import json
import os
import pathlib
import re
import sys
import time

# Ensure project root is on the path
PROJECT_ROOT = pathlib.Path(__file__).resolve().parent.parent
sys.path.insert(0, str(PROJECT_ROOT))


# ---------------------------------------------------------------------------
# Annotation parsing
# ---------------------------------------------------------------------------

# Patterns for annotation lines.  Annotations live in Fruit Salad comments
# that start with "--".  We strip the leading "-- " and match the tag.
_RE_TEST = re.compile(r"^@test\s+(.+)$")
_RE_INPUT = re.compile(r"^@input\s+(.+)$")
_RE_EXPECT = re.compile(r"^@expect\s+(.+)$")
_RE_EXPECT_ERROR = re.compile(r'^@expect_error\s+"?([^"]+)"?$')
_RE_EXPECT_RUNTIME_ERROR = re.compile(r'^@expect_runtime_error\s+"?([^"]+)"?$')


def parse_annotations(file_path: pathlib.Path) -> dict:
    """Parse inline test annotations from a .fs file.

    Returns a dict with keys:
        test_id       - str or None
        description   - str or None
        inputs        - list[str]  (multiple @input lines are concatenated)
        expects       - list[str]  (multiple @expect lines)
        expect_error  - str or None
        expect_runtime_error - str or None
    """
    result = {
        "test_id": None,
        "description": None,
        "inputs": [],
        "expects": [],
        "expect_error": None,
        "expect_runtime_error": None,
    }

    text = file_path.read_text(encoding="utf-8")
    for line in text.splitlines():
        stripped = line.strip()
        # Only look at comment lines
        if not stripped.startswith("--"):
            continue
        # Remove the leading dashes and optional whitespace
        content = stripped.lstrip("-").strip()

        m = _RE_TEST.match(content)
        if m:
            full = m.group(1)
            if ":" in full:
                tid, desc = full.split(":", 1)
                result["test_id"] = tid.strip()
                result["description"] = desc.strip()
            else:
                result["test_id"] = full.strip()
            continue

        m = _RE_INPUT.match(content)
        if m:
            result["inputs"].append(m.group(1))
            continue

        m = _RE_EXPECT.match(content)
        if m:
            result["expects"].append(m.group(1))
            continue

        m = _RE_EXPECT_ERROR.match(content)
        if m:
            result["expect_error"] = m.group(1).strip()
            continue

        m = _RE_EXPECT_RUNTIME_ERROR.match(content)
        if m:
            result["expect_runtime_error"] = m.group(1).strip()
            continue

    return result


# ---------------------------------------------------------------------------
# Test discovery
# ---------------------------------------------------------------------------


def discover_tests(test_dirs: list[pathlib.Path]) -> list[dict]:
    """Walk the given directories for .fs files and return parsed test info."""
    tests = []
    for d in test_dirs:
        if not d.exists():
            continue
        for fs_file in sorted(d.rglob("*.fs")):
            annotations = parse_annotations(fs_file)
            test_id = annotations["test_id"] or fs_file.stem
            tests.append(
                {
                    "file": str(fs_file),
                    "relative": str(fs_file.relative_to(PROJECT_ROOT)),
                    "annotations": annotations,
                    "test_id": test_id,
                }
            )
    return tests


# ---------------------------------------------------------------------------
# Execution
# ---------------------------------------------------------------------------


def run_test_local(test: dict, timeout: float, memory_limit: int) -> dict:
    """Run a single test using the local harness."""
    from harness.local.runner import run as local_run

    stdin_data = "\n".join(test["annotations"]["inputs"]) if test["annotations"]["inputs"] else None
    return local_run(
        test["file"],
        timeout=timeout,
        memory_limit_mb=memory_limit,
        stdin_data=stdin_data,
    )


def run_test_docker(test: dict, timeout: float, memory_limit: int) -> dict:
    """Run a single test using the Docker harness."""
    import subprocess

    stdin_data = "\n".join(test["annotations"]["inputs"]) if test["annotations"]["inputs"] else None
    # Mount the file into the container
    file_path = os.path.abspath(test["file"])
    container_path = f"/tmp/test/{os.path.basename(file_path)}"

    cmd = [
        "docker",
        "run",
        "--rm",
        "--network=none",
        "--read-only",
        "--tmpfs",
        "/tmp",
        f"--memory={memory_limit}m",
        f"--cpus=1",
        "-v",
        f"{file_path}:{container_path}:ro",
        "-e",
        f"FS_TIMEOUT={timeout}",
        "-e",
        f"FS_MEMORY_LIMIT_MB={memory_limit}",
        "fruitsalad-harness:latest",
        container_path,
    ]

    start = time.monotonic()
    try:
        proc = subprocess.run(
            cmd,
            capture_output=True,
            text=True,
            timeout=timeout + 10,  # extra buffer for Docker overhead
        )
        if proc.returncode == 0 and proc.stdout.strip():
            return json.loads(proc.stdout)
        return {
            "exit_code": proc.returncode,
            "stdout": proc.stdout,
            "stderr": proc.stderr,
            "duration_ms": int((time.monotonic() - start) * 1000),
            "timeout": False,
        }
    except subprocess.TimeoutExpired:
        return {
            "exit_code": -1,
            "stdout": "",
            "stderr": "Docker container timed out",
            "duration_ms": int((time.monotonic() - start) * 1000),
            "timeout": True,
        }
    except json.JSONDecodeError:
        return {
            "exit_code": -1,
            "stdout": proc.stdout,
            "stderr": f"Failed to parse harness JSON output: {proc.stderr}",
            "duration_ms": int((time.monotonic() - start) * 1000),
            "timeout": False,
        }


# ---------------------------------------------------------------------------
# Result evaluation
# ---------------------------------------------------------------------------


def normalize(text: str) -> str:
    """Normalize text for comparison: strip leading/trailing whitespace from
    each line, collapse multiple blank lines, strip overall."""
    lines = [l.strip() for l in text.strip().splitlines()]
    return "\n".join(lines)


def evaluate(test: dict, result: dict) -> dict:
    """Compare execution result against expected annotations.

    Returns a dict with:
        passed  - bool
        reason  - str (explanation on failure)
    """
    ann = test["annotations"]
    stdout = result.get("stdout", "")
    stderr = result.get("stderr", "")
    exit_code = result.get("exit_code", -1)
    timed_out = result.get("timeout", False)

    if timed_out:
        return {"passed": False, "reason": f"Timed out after {result.get('duration_ms', '?')}ms"}

    # -- Negative test: expect compile error --
    if ann["expect_error"]:
        expected_substr = ann["expect_error"]
        # The program should fail (non-zero exit) and stderr should contain the expected message
        if exit_code == 0:
            return {"passed": False, "reason": f"Expected compile error containing '{expected_substr}', but program succeeded"}
        combined_output = stderr + stdout
        if expected_substr.lower() in combined_output.lower():
            return {"passed": True, "reason": ""}
        return {
            "passed": False,
            "reason": f"Expected error containing '{expected_substr}', got: {combined_output[:200]}",
        }

    # -- Negative test: expect runtime error --
    if ann["expect_runtime_error"]:
        expected_substr = ann["expect_runtime_error"]
        if exit_code == 0:
            return {"passed": False, "reason": f"Expected runtime error containing '{expected_substr}', but program succeeded"}
        combined_output = stderr + stdout
        if expected_substr.lower() in combined_output.lower():
            return {"passed": True, "reason": ""}
        return {
            "passed": False,
            "reason": f"Expected runtime error containing '{expected_substr}', got: {combined_output[:200]}",
        }

    # -- Positive test: expect specific output --
    if ann["expects"]:
        if exit_code != 0:
            return {"passed": False, "reason": f"Non-zero exit code {exit_code}. stderr: {stderr[:200]}"}
        expected = normalize("\n".join(ann["expects"]))
        actual = normalize(stdout)
        if actual == expected:
            return {"passed": True, "reason": ""}
        return {
            "passed": False,
            "reason": f"Output mismatch.\n  Expected: {expected!r}\n  Actual:   {actual!r}",
        }

    # -- No expectations: just check it runs without error --
    if exit_code != 0:
        return {"passed": False, "reason": f"Non-zero exit code {exit_code}. stderr: {stderr[:200]}"}
    return {"passed": True, "reason": ""}


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------


def run_all(harness: str, timeout: float, memory_limit: int, verbose: bool) -> dict:
    """Discover, execute, and evaluate all tests. Returns summary dict."""
    solutions_dir = PROJECT_ROOT / "tests" / "solutions"
    negative_dir = PROJECT_ROOT / "tests" / "negative"

    tests = discover_tests([solutions_dir, negative_dir])

    if not tests:
        print("No test files found.")
        return {"passing": 0, "total": 0, "reward": 0.0, "tests": []}

    runner = run_test_local if harness == "local" else run_test_docker

    results = []
    passing = 0
    total = len(tests)

    for i, test in enumerate(tests, 1):
        label = test["annotations"].get("description") or test["test_id"]
        relative = test["relative"]

        if verbose:
            print(f"[{i}/{total}] {relative} ({label}) ... ", end="", flush=True)

        result = runner(test, timeout, memory_limit)
        evaluation = evaluate(test, result)
        passed = evaluation["passed"]

        if passed:
            passing += 1

        if verbose:
            status = "PASS" if passed else "FAIL"
            print(status)
            if not passed:
                for line in evaluation["reason"].splitlines():
                    print(f"         {line}")
        elif not passed:
            print(f"FAIL: {relative} - {evaluation['reason']}")

        results.append(
            {
                "test_id": test["test_id"],
                "file": test["relative"],
                "description": label,
                "passed": passed,
                "reason": evaluation["reason"],
                "exit_code": result.get("exit_code"),
                "duration_ms": result.get("duration_ms"),
                "timeout": result.get("timeout", False),
            }
        )

    reward = passing / total if total > 0 else 0.0

    # Print summary
    print()
    bar_len = 30
    filled = int(bar_len * passing / total) if total > 0 else 0
    bar = "=" * filled + " " * (bar_len - filled)
    print(f"[{bar}] {passing}/{total} tests passed ({reward:.1%})")

    if passing < total:
        print("\nFAILED:")
        for r in results:
            if not r["passed"]:
                print(f"  {r['file']} - {r['reason'].splitlines()[0]}")

    print(f"\nReward signal: {reward:.3f}")

    return {
        "passing": passing,
        "total": total,
        "reward": reward,
        "tests": results,
    }


def write_report(summary: dict):
    """Write the test results JSON report to metrics/test_results.json."""
    metrics_dir = PROJECT_ROOT / "metrics"
    metrics_dir.mkdir(exist_ok=True)
    report_path = metrics_dir / "test_results.json"

    report = {
        "timestamp": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
        "passing": summary["passing"],
        "total": summary["total"],
        "reward": summary["reward"],
        "tests": summary["tests"],
    }
    report_path.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description="Fruit Salad test runner")
    parser.add_argument(
        "--harness",
        choices=["local", "docker"],
        default="local",
        help="Execution harness to use (default: local)",
    )
    parser.add_argument(
        "--timeout",
        type=float,
        default=30.0,
        help="Per-test timeout in seconds (default: 30)",
    )
    parser.add_argument(
        "--memory-limit",
        type=int,
        default=256,
        help="Per-test memory limit in MB (default: 256)",
    )
    parser.add_argument(
        "--verbose",
        "-v",
        action="store_true",
        help="Show per-test progress",
    )

    args = parser.parse_args()

    summary = run_all(
        harness=args.harness,
        timeout=args.timeout,
        memory_limit=args.memory_limit,
        verbose=args.verbose,
    )

    write_report(summary)

    sys.exit(0 if summary["passing"] == summary["total"] else 1)


if __name__ == "__main__":
    main()
