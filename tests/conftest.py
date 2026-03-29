"""pytest configuration for Fruit Salad test discovery.

This module allows pytest to discover and run .fs test files alongside
the custom test runner. Each .fs file with annotations becomes a
parameterised pytest test case.
"""

import pathlib
import sys

import pytest

# Ensure project root is importable
PROJECT_ROOT = pathlib.Path(__file__).resolve().parent.parent
sys.path.insert(0, str(PROJECT_ROOT))

from tests.run_tests import (  # noqa: E402
    discover_tests,
    evaluate,
    run_test_local,
)

# Directories containing test .fs files
_SOLUTIONS_DIR = PROJECT_ROOT / "tests" / "solutions"
_NEGATIVE_DIR = PROJECT_ROOT / "tests" / "negative"


def _collect_fs_tests():
    """Gather all .fs test cases for parameterisation."""
    return discover_tests([_SOLUTIONS_DIR, _NEGATIVE_DIR])


# Build the parameter list once at import time so pytest can display
# individual test IDs.
_FS_TESTS = _collect_fs_tests()


@pytest.fixture(params=_FS_TESTS, ids=[t["relative"] for t in _FS_TESTS])
def fs_test_case(request):
    """Fixture that yields each discovered .fs test case."""
    return request.param


def test_fs_program(fs_test_case):
    """Run a Fruit Salad test file and assert it meets expectations."""
    result = run_test_local(fs_test_case, timeout=30.0, memory_limit=256)
    evaluation = evaluate(fs_test_case, result)
    assert evaluation["passed"], (
        f"{fs_test_case['relative']}: {evaluation['reason']}"
    )
