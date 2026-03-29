-- @test neg-007: Type error in arithmetic
-- @expect_runtime_error "Cannot"

blend main() {
    fresh arr = [1, 2]
    display(arr + 5)
}
