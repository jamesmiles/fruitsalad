-- @test math-007: Modular Exponentiation
-- @expect 24
-- @expect 1594323

blend power_mod(base, exp, mod) {
    fresh result = 1
    fresh b = base % mod

    fresh e = exp
    while e > 0 {
        if e % 2 == 1 {
            result = (result * b) % mod
        }
        e = e / 2
        b = (b * b) % mod
    }

    result
}

blend main() {
    display(power_mod(2, 10, 1000))
    display(power_mod(3, 13, 1000000007))
}
