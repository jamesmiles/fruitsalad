-- @test type-006: Ripe and Harvest Types (Phase 3)
-- @expect 5
-- @expect Error: division by zero

blend safe_div(a, b) {
    if b == 0 {
        yield rot("division by zero")
    }
    ripe(a / b)
}

blend main() {
    sort safe_div(10, 2) {
        ripe(v) => { display(v) }
        rot(e) => { display("Error: {e}") }
    }
    sort safe_div(10, 0) {
        ripe(v) => { display(v) }
        rot(e) => { display("Error: {e}") }
    }
}
