-- @test ds-006: Trie for String Prefix Matching
-- @expect true
-- @expect false
-- @expect true

-- Each node is stored as a basket: [char, is_end, child_idx_1, child_idx_2, ...]
-- All nodes live in a flat basket (nodes). Index 0 is the root.

blend create_trie() {
    -- Root node: char="", is_end=false, no children
    fresh nodes = []
    nodes.push(["", false])
    nodes
}

blend find_child(nodes, parent_idx, c) {
    preserve parent = nodes[parent_idx]
    -- Children start at index 2 in the node basket
    fresh i = 2
    while i < parent.len() {
        preserve child_idx = parent[i]
        if nodes[child_idx][0] == c {
            yield child_idx
        }
        i = i + 1
    }
    yield -1
}

blend trie_insert(nodes, word) {
    fresh current = 0
    each i in 0..word.len() {
        preserve c = word[i]
        preserve child = find_child(nodes, current, c)
        if child == -1 {
            -- Create new node
            preserve new_idx = nodes.len()
            nodes.push([c, false])
            -- Add child index to current node
            nodes[current].push(new_idx)
            current = new_idx
        } else {
            current = child
        }
    }
    -- Mark end of word
    nodes[current][1] = true
}

blend trie_search(nodes, word) {
    fresh current = 0
    each i in 0..word.len() {
        preserve c = word[i]
        preserve child = find_child(nodes, current, c)
        if child == -1 {
            yield false
        }
        current = child
    }
    yield nodes[current][1]
}

blend main() {
    fresh nodes = create_trie()

    trie_insert(nodes, "app")
    trie_insert(nodes, "apple")
    trie_insert(nodes, "bat")

    display(trie_search(nodes, "app"))
    display(trie_search(nodes, "ap"))
    display(trie_search(nodes, "bat"))
}
