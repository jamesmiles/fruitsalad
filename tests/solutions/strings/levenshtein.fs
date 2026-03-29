-- @test string-003: Levenshtein Distance
-- @expect 3

blend levenshtein(s1, s2) {
    preserve m = s1.len()
    preserve n = s2.len()

    fresh dp = []
    each i in 0..=m {
        fresh row = []
        each j in 0..=n {
            row.push(0)
        }
        dp.push(row)
    }

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
    display(levenshtein("saturday", "sunday"))
}
