-- @test ds-002: Queue Using Two Stacks
-- @expect 1
-- @expect 2
-- @expect 3

-- Queue implemented with two stacks (baskets)
-- stack_in: for enqueue (push)
-- stack_out: for dequeue (pop from reversed order)

blend enqueue(stack_in, val) {
    stack_in.push(val)
}

blend transfer(stack_in, stack_out) {
    while stack_in.len() > 0 {
        stack_out.push(stack_in.pop())
    }
}

blend dequeue(stack_in, stack_out) {
    if stack_out.len() == 0 {
        transfer(stack_in, stack_out)
    }
    stack_out.pop()
}

blend main() {
    fresh stack_in = []
    fresh stack_out = []

    enqueue(stack_in, 1)
    enqueue(stack_in, 2)
    enqueue(stack_in, 3)

    display(dequeue(stack_in, stack_out))
    display(dequeue(stack_in, stack_out))
    display(dequeue(stack_in, stack_out))
}
