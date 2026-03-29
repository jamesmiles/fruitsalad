-- @test neg-rt-001: Division by zero
-- @expect_runtime_error "division by zero"

blend main() {
    display(10 / 0)
}
