-- @test neg-006: Wrong argument count
-- @expect_error "expects 2 argument"

blend add(a, b) {
    a + b
}

blend main() {
    display(add(1))
}
