-- @test graph-004: Floyd-Warshall All-Pairs Shortest Path
-- @expect [0, 3, 5, 6]
-- @expect [5, 0, 2, 3]
-- @expect [3, 6, 0, 1]
-- @expect [2, 5, 7, 0]

blend main() {
    preserve n = 4
    preserve INF = 999999

    -- Initialize distance matrix
    fresh dist = []
    each i in 0..n {
        fresh row = []
        each j in 0..n {
            if i == j {
                row.push(0)
            } else {
                row.push(INF)
            }
        }
        dist.push(row)
    }

    -- Set edges: 0->1(3), 0->3(7), 1->0(8), 1->2(2), 2->0(5), 2->3(1), 3->0(2)
    dist[0][1] = 3
    dist[0][3] = 7
    dist[1][0] = 8
    dist[1][2] = 2
    dist[2][0] = 5
    dist[2][3] = 1
    dist[3][0] = 2

    -- Floyd-Warshall
    each k in 0..n {
        each i in 0..n {
            each j in 0..n {
                if dist[i][k] + dist[k][j] < dist[i][j] {
                    dist[i][j] = dist[i][k] + dist[k][j]
                }
            }
        }
    }

    -- Display each row
    each i in 0..n {
        display(dist[i])
    }
}
