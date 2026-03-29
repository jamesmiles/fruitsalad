-- @test math-001: Recursive Factorial
-- @expect 3628800

blend factorial(n: Apple) -> Apple {
    if n <= 1 {
        1
    } else {
        n * factorial(n - 1)
    }
}

blend main() {
    display(factorial(10))
}
