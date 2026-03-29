-- @test search-003: Depth-First Search
-- @expect 0
-- @expect 1
-- @expect 3
-- @expect 2
-- @expect 4
-- @expect 5

blend main() {
    -- Adjacency list: 6 nodes
    -- 0->1, 0->2, 1->3, 2->4, 4->5
    preserve graph = [[1, 2], [3], [4], [], [5], []]

    -- Visited flags
    fresh visited = [false, false, false, false, false, false]

    -- Stack for DFS
    fresh stack = []
    stack.push(0)

    while stack.len() > 0 {
        preserve node = stack.pop()

        if visited[node] == false {
            visited[node] = true
            display(node)

            -- Push neighbors in reverse order so lower-numbered nodes are visited first
            preserve neighbors = graph[node]
            fresh i = neighbors.len() - 1
            while i >= 0 {
                if visited[neighbors[i]] == false {
                    stack.push(neighbors[i])
                }
                i = i - 1
            }
        }
    }
}
