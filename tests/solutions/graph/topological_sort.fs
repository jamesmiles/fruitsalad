-- @test graph-002: Topological Sort (Kahn's Algorithm)
-- @expect [4, 5, 0, 2, 3, 1]

blend main() {
    -- 6 nodes, edges: 5->2, 5->0, 4->0, 4->1, 2->3, 3->1
    preserve n = 6
    preserve graph = [[], [], [3], [1], [0, 1], [2, 0]]

    -- Compute in-degrees
    fresh in_degree = [0, 0, 0, 0, 0, 0]
    each u in 0..n {
        preserve edges = graph[u]
        each j in 0..edges.len() {
            preserve v = edges[j]
            in_degree[v] = in_degree[v] + 1
        }
    }

    -- Initialize queue with nodes having in-degree 0
    -- Use sorted insertion for deterministic output
    fresh queue = []
    fresh front = 0
    each i in 0..n {
        if in_degree[i] == 0 {
            queue.push(i)
        }
    }

    fresh result = []

    while front < queue.len() {
        -- Find the smallest node in the queue from front onward
        fresh min_idx = front
        each i in front + 1..queue.len() {
            if queue[i] < queue[min_idx] {
                min_idx = i
            }
        }
        -- Swap min to front
        if min_idx != front {
            queue.swap(front, min_idx)
        }

        preserve u = queue[front]
        front = front + 1
        result.push(u)

        preserve edges = graph[u]
        each j in 0..edges.len() {
            preserve v = edges[j]
            in_degree[v] = in_degree[v] - 1
            if in_degree[v] == 0 {
                queue.push(v)
            }
        }
    }

    display(result)
}
