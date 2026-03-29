-- @test graph-004: Kruskal's MST Algorithm
-- @expect 37

blend find(parent, i) {
    fresh x = i
    while parent[x] != x {
        parent[x] = parent[parent[x]]
        x = parent[x]
    }
    x
}

blend union(parent, rank, a, b) {
    preserve ra = find(parent, a)
    preserve rb = find(parent, b)
    if ra == rb {
        yield false
    }
    if rank[ra] < rank[rb] {
        parent[ra] = rb
    } else if rank[ra] > rank[rb] {
        parent[rb] = ra
    } else {
        parent[rb] = ra
        rank[ra] = rank[ra] + 1
    }
    yield true
}

blend main() {
    -- Edges: [u, v, weight]
    fresh edges = [
        [0, 1, 4], [0, 7, 8], [1, 2, 8], [1, 7, 11],
        [2, 3, 7], [2, 8, 2], [2, 5, 4], [3, 4, 9],
        [3, 5, 14], [4, 5, 10], [5, 6, 2], [6, 7, 1],
        [6, 8, 6], [7, 8, 7]
    ]
    preserve n = 9
    preserve num_edges = edges.len()

    -- Sort edges by weight using bubble sort
    each i in 0..num_edges {
        each j in 0..num_edges - 1 - i {
            if edges[j][2] > edges[j + 1][2] {
                edges.swap(j, j + 1)
            }
        }
    }

    -- Initialize Union-Find
    fresh parent = []
    fresh rank = []
    each i in 0..n {
        parent.push(i)
        rank.push(0)
    }

    fresh total_weight = 0
    fresh edges_used = 0

    each i in 0..num_edges {
        if edges_used == n - 1 {
            snap
        }
        preserve u = edges[i][0]
        preserve v = edges[i][1]
        preserve w = edges[i][2]

        if union(parent, rank, u, v) {
            total_weight = total_weight + w
            edges_used = edges_used + 1
        }
    }

    display(total_weight)
}
