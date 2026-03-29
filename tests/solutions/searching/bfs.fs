-- @test search-004: Breadth-First Search
-- @expect 0
-- @expect 1
-- @expect 2
-- @expect 3
-- @expect 4
-- @expect 5

blend main() {
    -- Adjacency list: 6 nodes
    -- 0->1, 0->2, 1->3, 2->4, 4->5
    preserve graph = [[1, 2], [3], [4], [], [5], []]

    -- Visited flags
    fresh visited = [false, false, false, false, false, false]

    -- Queue for BFS (use basket with front index)
    fresh queue = []
    fresh front = 0
    queue.push(0)
    visited[0] = true

    while front < queue.len() {
        preserve node = queue[front]
        front = front + 1
        display(node)

        preserve neighbors = graph[node]
        each i in 0..neighbors.len() {
            preserve neighbor = neighbors[i]
            if visited[neighbor] == false {
                visited[neighbor] = true
                queue.push(neighbor)
            }
        }
    }
}
