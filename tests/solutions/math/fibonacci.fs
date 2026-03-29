-- @test math-002: Recursive Fibonacci
-- @expect 55

blend fib(n: Apple) -> Apple {
    if n <= 1 {
        n
    } else {
        fib(n - 1) + fib(n - 2)
    }
}

blend main() {
    display(fib(10))
}
