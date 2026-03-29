-- @test ds-001: Stack with Basket
-- @expect 3
-- @expect 2
-- @expect 1

blend main() {
    fresh stack = []

    -- Push 1, 2, 3
    stack.push(1)
    stack.push(2)
    stack.push(3)

    -- Pop and display three times
    display(stack.pop())
    display(stack.pop())
    display(stack.pop())
}
