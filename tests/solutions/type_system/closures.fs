-- @test type-004: Closures and Higher-Order Functions
-- @expect 10
-- @expect 20
-- @expect 15

blend apply(f, x) {
    f(x)
}

blend make_multiplier(factor) {
    |x| { x * factor }
}

blend compose(f, g) {
    |x| { f(g(x)) }
}

blend main() {
    preserve double = make_multiplier(2)
    preserve triple = make_multiplier(3)

    -- apply double to 5
    display(apply(double, 5))

    -- apply double to 10
    display(apply(double, 10))

    -- apply triple to 5
    display(apply(triple, 5))
}
