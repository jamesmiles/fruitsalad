-- @test neg-rt-002: Index out of range
-- @expect_runtime_error "out of range"

blend main() {
    preserve arr = [1, 2, 3]
    display(arr[5])
}
