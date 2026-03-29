-- @test dp-001: Fibonacci with Bottom-Up DP
-- @expect 12586269025

blend fib(n) {
    fresh dp = []
    dp.push(0)
    dp.push(1)

    each i in 2..=n {
        dp.push(dp[i - 1] + dp[i - 2])
    }

    dp[n]
}

blend main() {
    display(fib(50))
}
