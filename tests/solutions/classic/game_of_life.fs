-- @test classic-004: Conway's Game of Life (1 generation)
-- @expect 5

blend count_neighbors(grid, r, c, rows, cols) {
    fresh count = 0
    fresh dr = [-1, -1, -1, 0, 0, 1, 1, 1]
    fresh dc = [-1, 0, 1, -1, 1, -1, 0, 1]

    each d in 0..8 {
        preserve nr = r + dr[d]
        preserve nc = c + dc[d]
        if nr >= 0 && nr < rows && nc >= 0 && nc < cols {
            count = count + grid[nr][nc]
        }
    }
    yield count
}

blend main() {
    preserve rows = 6
    preserve cols = 6

    -- Initialize grid
    fresh grid = []
    each i in 0..rows {
        fresh row = []
        each j in 0..cols {
            row.push(0)
        }
        grid.push(row)
    }

    -- Place glider: (0,1),(1,2),(2,0),(2,1),(2,2)
    grid[0][1] = 1
    grid[1][2] = 1
    grid[2][0] = 1
    grid[2][1] = 1
    grid[2][2] = 1

    -- Compute next generation
    fresh next = []
    each i in 0..rows {
        fresh row = []
        each j in 0..cols {
            preserve neighbors = count_neighbors(grid, i, j, rows, cols)
            if grid[i][j] == 1 {
                if neighbors == 2 || neighbors == 3 {
                    row.push(1)
                } else {
                    row.push(0)
                }
            } else {
                if neighbors == 3 {
                    row.push(1)
                } else {
                    row.push(0)
                }
            }
        }
        next.push(row)
    }

    -- Count live cells
    fresh live = 0
    each i in 0..rows {
        each j in 0..cols {
            live = live + next[i][j]
        }
    }

    display(live)
}
