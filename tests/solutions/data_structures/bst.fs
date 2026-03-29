-- @test ds-003: Binary Search Tree
-- @expect 1
-- @expect 3
-- @expect 4
-- @expect 5
-- @expect 7

medley Tree {
    Node(value: Apple, left, right),
    Empty,
}

blend insert(tree, val) {
    sort tree {
        Tree.Empty => {
            yield Tree.Node(val, Tree.Empty, Tree.Empty)
        }
        Tree.Node(v, left, right) => {
            if val < v {
                yield Tree.Node(v, insert(left, val), right)
            } else {
                yield Tree.Node(v, left, insert(right, val))
            }
        }
    }
}

blend inorder(tree) {
    sort tree {
        Tree.Empty => {}
        Tree.Node(v, left, right) => {
            inorder(left)
            display(v)
            inorder(right)
        }
    }
}

blend main() {
    fresh tree = Tree.Empty
    tree = insert(tree, 5)
    tree = insert(tree, 3)
    tree = insert(tree, 7)
    tree = insert(tree, 1)
    tree = insert(tree, 4)
    inorder(tree)
}
