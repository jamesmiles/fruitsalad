-- @test ds-002: Linked List with Medley
-- @expect 1
-- @expect 2
-- @expect 3

medley Node {
    Cons(value: Apple, next),
    Nil,
}

blend traverse(node) {
    sort node {
        Node.Cons(val, next) => {
            display(val)
            traverse(next)
        }
        Node.Nil => {}
    }
}

blend main() {
    preserve list = Node.Cons(1, Node.Cons(2, Node.Cons(3, Node.Nil)))
    traverse(list)
}
