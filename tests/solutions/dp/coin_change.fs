-- @test dp-002: Coin Change (Minimum Coins)
-- @expect 3

blend coin_change(coins, amount) {
    -- dp[i] = minimum coins needed for amount i
    -- Use amount + 1 as "infinity" (impossible)
    preserve inf = amount + 1
    fresh dp = []
    each i in 0..=amount {
        dp.push(inf)
    }
    dp[0] = 0

    each i in 1..=amount {
        each j in 0..coins.len() {
            if coins[j] <= i {
                if dp[i - coins[j]] + 1 < dp[i] {
                    dp[i] = dp[i - coins[j]] + 1
                }
            }
        }
    }

    if dp[amount] > amount {
        yield -1
    }
    dp[amount]
}

blend main() {
    preserve coins = [1, 5, 10, 25]
    display(coin_change(coins, 36))
}
