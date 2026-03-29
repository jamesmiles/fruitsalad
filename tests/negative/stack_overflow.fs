-- @test neg-rt-004: Stack overflow from infinite recursion
-- @expect_runtime_error "recursion"

blend infinite(n) {
    infinite(n + 1)
}

blend main() {
    infinite(0)
}
