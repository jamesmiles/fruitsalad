-- @test graph-001: Dijkstra's Shortest Path
-- @expect [0, 3, 1, 4]

blend main() {
    -- Weighted adjacency list: graph[node] = [[to, weight], ...]
    -- 0->1(4), 0->2(1), 2->1(2), 1->3(1), 2->3(5)
    preserve graph = [
        [[1, 4], [2, 1]],
        [[3, 1]],
        [[1, 2], [3, 5]],
        []
    ]
    preserve n = 4
    preserve INF = 999999

    -- Distance array
    fresh dist = [INF, INF, INF, INF]
    dist[0] = 0

    -- Visited array
    fresh visited = [false, false, false, false]

    each step in 0..n {
        -- Find unvisited node with minimum distance
        fresh u = -1
        fresh min_dist = INF
        each i in 0..n {
            if visited[i] == false && dist[i] < min_dist {
                min_dist = dist[i]
                u = i
            }
        }

        if u == -1 {
            snap
        }

        visited[u] = true

        -- Relax edges from u
        preserve edges = graph[u]
        each i in 0..edges.len() {
            preserve edge = edges[i]
            preserve v = edge[0]
            preserve w = edge[1]
            if dist[u] + w < dist[v] {
                dist[v] = dist[u] + w
            }
        }
    }

    display(dist)
}
