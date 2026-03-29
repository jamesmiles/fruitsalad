-- @test classic-003: N-Queens (N=4)
-- @expect 2

blend is_safe(cols, row, col) {
    each i in 0..row {
        if cols[i] == col {
            yield false
        }
        -- Check diagonals: |cols[i] - col| == row - i
        preserve diff = row - i
        if cols[i] - col == diff || col - cols[i] == diff {
            yield false
        }
    }
    yield true
}

blend solve(cols, row, n) {
    if row == n {
        yield 1
    }

    fresh count = 0
    each col in 0..n {
        if is_safe(cols, row, col) {
            cols[row] = col
            count = count + solve(cols, row + 1, n)
        }
    }
    yield count
}

blend main() {
    preserve n = 4
    fresh cols = [-1, -1, -1, -1]
    display(solve(cols, 0, n))
}
