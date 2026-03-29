-- @test type-005: Smoothie Pipeline Operator (Phase 3)
-- @expect 11

blend double(x) {
    x * 2
}

blend add_one(x) {
    x + 1
}

blend main() {
    -- 5 ~> double = double(5) = 10, then ~> add_one = add_one(10) = 11
    preserve result = 5 ~> double ~> add_one
    display(result)
}
