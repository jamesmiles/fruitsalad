-- @test math-003: Euclidean GCD
-- @expect 6

blend gcd(a: Apple, b: Apple) -> Apple {
    if b == 0 {
        a
    } else {
        gcd(b, a % b)
    }
}

blend main() {
    display(gcd(48, 18))
}
