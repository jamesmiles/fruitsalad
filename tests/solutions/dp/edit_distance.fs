-- @test dp-005: Edit Distance (Levenshtein)
-- @expect 3

blend edit_distance(s1, s2) {
    preserve m = s1.len()
    preserve n = s2.len()

    -- Build 2D DP table
    fresh dp = []
    each i in 0..=m {
        fresh row = []
        each j in 0..=n {
            row.push(0)
        }
        dp.push(row)
    }

    -- Base cases
    each i in 0..=m {
        dp[i][0] = i
    }
    each j in 0..=n {
        dp[0][j] = j
    }

    each i in 1..=m {
        each j in 1..=n {
            if s1[i - 1] == s2[j - 1] {
                dp[i][j] = dp[i - 1][j - 1]
            } else {
                fresh min_val = dp[i - 1][j] + 1
                if dp[i][j - 1] + 1 < min_val {
                    min_val = dp[i][j - 1] + 1
                }
                if dp[i - 1][j - 1] + 1 < min_val {
                    min_val = dp[i - 1][j - 1] + 1
                }
                dp[i][j] = min_val
            }
        }
    }

    dp[m][n]
}

blend main() {
    display(edit_distance("kitten", "sitting"))
}
