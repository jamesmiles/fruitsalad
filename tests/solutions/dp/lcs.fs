-- @test dp-003: Longest Common Subsequence
-- @expect 4

blend lcs(s1, s2) {
    preserve m = s1.len()
    preserve n = s2.len()

    -- Build 2D DP table as basket-of-baskets
    fresh dp = []
    each i in 0..=m {
        fresh row = []
        each j in 0..=n {
            row.push(0)
        }
        dp.push(row)
    }

    each i in 1..=m {
        each j in 1..=n {
            if s1[i - 1] == s2[j - 1] {
                dp[i][j] = dp[i - 1][j - 1] + 1
            } else {
                if dp[i - 1][j] > dp[i][j - 1] {
                    dp[i][j] = dp[i - 1][j]
                } else {
                    dp[i][j] = dp[i][j - 1]
                }
            }
        }
    }

    dp[m][n]
}

blend main() {
    display(lcs("ABCBDAB", "BDCAB"))
}
