-- @test neg-003: Non-exhaustive sort
-- @expect_runtime_error "non-exhaustive"

blend main() {
    sort 42 {
        0 => "zero"
        1 => "one"
    }
}
