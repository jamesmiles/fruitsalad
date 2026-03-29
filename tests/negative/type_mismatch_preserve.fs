-- @test neg-001: Immutable variable reassignment
-- @expect_error "Cannot reassign immutable"

blend main() {
    preserve x = 42
    x = 10
}
