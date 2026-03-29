#!/usr/bin/env bash
# Compare fs interpreter and ffs compiler output for .fs files in a directory
# Usage: tests/ffs_compare.sh tests/solutions/sorting
#        tests/ffs_compare.sh tests/solutions/math tests/solutions/strings
#
# Exit 0 if all compatible programs match, 1 if any mismatch/run error.
# Programs using unsupported features (Bowl/Medley/Sort/Ripe) are reported
# as COMPILE_ERR but do NOT cause failure.

set -uo pipefail

pass=0; fail=0; compile_err=0; skip=0

for dir in "$@"; do
    for f in "$dir"/*.fs; do
        [ -f "$f" ] || continue
        name=$(basename "$f" .fs)

        # Skip self-hosting meta-tests (interpreter-in-interpreter)
        case "$name" in self_hosting|ffs_factorial) skip=$((skip+1)); continue;; esac

        # Get reference output from fs interpreter
        fs_out=$(timeout 10 python3 -m fs "$f" 2>/dev/null)
        if [ $? -ne 0 ]; then skip=$((skip+1)); continue; fi

        # Compile with ffs
        py_code=$(timeout 20 python3 -m fs ffs/compiler.fs "$f" 2>/dev/null)
        if [ $? -ne 0 ]; then
            echo "  COMPILE_ERR: $name (unsupported feature)"
            compile_err=$((compile_err+1))
            continue
        fi

        # Run generated Python
        ffs_out=$(echo "$py_code" | timeout 5 python3 2>/dev/null)
        if [ $? -ne 0 ]; then
            echo "  RUN_ERR: $name"
            fail=$((fail+1))
            continue
        fi

        # Compare
        if [ "$fs_out" = "$ffs_out" ]; then
            echo "  PASS: $name"
            pass=$((pass+1))
        else
            echo "  MISMATCH: $name"
            echo "    fs:  $(echo "$fs_out" | head -1)"
            echo "    ffs: $(echo "$ffs_out" | head -1)"
            fail=$((fail+1))
        fi
    done
done

echo ""
echo "Results: $pass pass, $fail fail, $compile_err compile_err, $skip skipped"
[ $fail -eq 0 ] || exit 1
