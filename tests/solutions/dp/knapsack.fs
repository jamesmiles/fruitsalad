-- @test dp-004: 0/1 Knapsack
-- @expect 9

blend knapsack(weights, values, capacity) {
    preserve n = weights.len()

    -- Build 2D DP table: dp[i][w] = max value using first i items with capacity w
    fresh dp = []
    each i in 0..=n {
        fresh row = []
        each w in 0..=capacity {
            row.push(0)
        }
        dp.push(row)
    }

    each i in 1..=n {
        each w in 0..=capacity {
            -- Don't take item i-1
            dp[i][w] = dp[i - 1][w]

            -- Take item i-1 if it fits
            if weights[i - 1] <= w {
                preserve with_item = dp[i - 1][w - weights[i - 1]] + values[i - 1]
                if with_item > dp[i][w] {
                    dp[i][w] = with_item
                }
            }
        }
    }

    dp[n][capacity]
}

blend main() {
    preserve weights = [1, 3, 4, 5]
    preserve values = [1, 4, 5, 7]
    display(knapsack(weights, values, 7))
}
