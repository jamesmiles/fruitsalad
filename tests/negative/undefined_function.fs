-- @test neg-004: Undefined function call
-- @expect_error "undefined"

blend main() {
    foo(42)
}
