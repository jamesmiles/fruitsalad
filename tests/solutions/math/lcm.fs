-- @test math-004: LCM via GCD
-- @expect 36

blend gcd(a: Apple, b: Apple) -> Apple {
    if b == 0 {
        a
    } else {
        gcd(b, a % b)
    }
}

blend lcm(a: Apple, b: Apple) -> Apple {
    a / gcd(a, b) * b
}

blend main() {
    display(lcm(12, 18))
}
