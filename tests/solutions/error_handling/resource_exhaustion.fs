-- @test err-003: Resource Pool with Exhaustion Handling
-- @expect ok, ok, ok, error("resource exhausted")
-- @expect ok, ok, ok, ok, available_count=0
-- @expect ok, ok, error("no resource to release")
-- @expect error("resource exhausted")

-- Pool represented as basket: [total, in_use]
blend make_pool(capacity) {
    [capacity, 0]
}

blend acquire(pool) {
    if pool[1] >= pool[0] {
        yield rot("resource exhausted")
    }
    pool[1] = pool[1] + 1
    ripe("ok")
}

blend release(pool) {
    if pool[1] <= 0 {
        yield rot("no resource to release")
    }
    pool[1] = pool[1] - 1
    ripe("ok")
}

blend available_count(pool) {
    pool[0] - pool[1]
}

blend format_result(result) {
    sort result {
        ripe(v) => v
        rot(e) => "error(\"{e}\")"
    }
}

blend main() {
    -- Test 1: capacity=3, acquire x4
    fresh p1 = make_pool(3)
    preserve r1a = format_result(acquire(p1))
    preserve r1b = format_result(acquire(p1))
    preserve r1c = format_result(acquire(p1))
    preserve r1d = format_result(acquire(p1))
    display("{r1a}, {r1b}, {r1c}, {r1d}")

    -- Test 2: capacity=2, acquire, release, acquire, acquire, available_count
    fresh p2 = make_pool(2)
    preserve r2a = format_result(acquire(p2))
    preserve r2b = format_result(release(p2))
    preserve r2c = format_result(acquire(p2))
    preserve r2d = format_result(acquire(p2))
    preserve r2e = available_count(p2)
    display("{r2a}, {r2b}, {r2c}, {r2d}, available_count={r2e}")

    -- Test 3: capacity=1, acquire, release, release
    fresh p3 = make_pool(1)
    preserve r3a = format_result(acquire(p3))
    preserve r3b = format_result(release(p3))
    preserve r3c = format_result(release(p3))
    display("{r3a}, {r3b}, {r3c}")

    -- Test 4: capacity=0, acquire
    fresh p4 = make_pool(0)
    preserve r4a = format_result(acquire(p4))
    display("{r4a}")
}
