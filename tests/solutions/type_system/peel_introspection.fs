-- @test type-008: Peel type introspection
-- @expect Apple
-- @expect Banana
-- @expect Cherry
-- @expect Date
-- @expect Basket

blend main() {
    display(peel(42))
    display(peel("hello"))
    display(peel(true))
    display(peel(3.14))
    display(peel([1, 2, 3]))
}
