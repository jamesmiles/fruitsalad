-- @test type-010: Built-in functions
-- @expect 42
-- @expect 3
-- @expect 7
-- @expect 3
-- @expect 42
-- @expect 42

blend main() {
    display(abs(-42))
    display(min(3, 7))
    display(max(3, 7))
    display(to_apple(3.14))
    display(to_date(42))
    display(to_banana(42))
}
