-- @test neg-005: Toss rot without handler
-- @expect_runtime_error "division by zero"

blend fail() {
    toss rot("division by zero")
}

blend main() {
    fail()
}
