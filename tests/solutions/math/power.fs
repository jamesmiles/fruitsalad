-- @test math-005: Integer Exponentiation
-- @expect 1024

blend power(base: Apple, exp: Apple) -> Apple {
    if exp == 0 {
        1
    } else {
        base * power(base, exp - 1)
    }
}

blend main() {
    display(power(2, 10))
}
