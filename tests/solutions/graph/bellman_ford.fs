-- @test graph-003: Bellman-Ford Shortest Path
-- @expect [0, 3, 1, 4]

blend main() {
    -- Edges as [from, to, weight]
    preserve edges = [[0, 1, 4], [0, 2, 1], [2, 1, 2], [1, 3, 1], [2, 3, 5]]
    preserve n = 4
    preserve INF = 999999

    -- Distance array
    fresh dist = [INF, INF, INF, INF]
    dist[0] = 0

    -- Relax edges n-1 times
    each step in 0..n - 1 {
        each i in 0..edges.len() {
            preserve e = edges[i]
            preserve u = e[0]
            preserve v = e[1]
            preserve w = e[2]
            if dist[u] != INF && dist[u] + w < dist[v] {
                dist[v] = dist[u] + w
            }
        }
    }

    display(dist)
}
