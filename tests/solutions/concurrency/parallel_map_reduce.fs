-- @test conc-003: Parallel Map-Reduce (sequential simulation)
-- @expect 55
-- @expect 1400
-- @expect 4
-- @expect 0

blend map_reduce(data, map_fn, reduce_fn, init) {
    -- Sequential simulation of parallel map-reduce
    -- Map phase: apply map_fn to each element
    fresh mapped = []
    each i in 0..data.len() {
        mapped.push(map_fn(data[i]))
    }
    -- Reduce phase: combine all mapped results
    fresh acc = init
    each i in 0..mapped.len() {
        acc = reduce_fn(acc, mapped[i])
    }
    acc
}

blend square(x) { x * x }
blend sum(a, b) { a + b }

blend main() {
    -- Test 1: [1,2,3,4,5] -> square -> sum = 1+4+9+16+25 = 55
    display(map_reduce([1, 2, 3, 4, 5], square, sum, 0))

    -- Test 2: [10,20,30] -> square -> sum = 100+400+900 = 1400
    display(map_reduce([10, 20, 30], square, sum, 0))

    -- Test 3: [1,1,1,1] -> square -> sum = 1+1+1+1 = 4
    display(map_reduce([1, 1, 1, 1], square, sum, 0))

    -- Test 4: [] -> square -> sum = 0
    display(map_reduce([], square, sum, 0))
}
