-- @test neg-rt-003: Pop from empty basket
-- @expect_runtime_error "empty"

blend main() {
    fresh arr = []
    arr.pop()
}
